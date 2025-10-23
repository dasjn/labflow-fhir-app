using System.Text;
using Hl7.Fhir.Serialization;
using LabFlow.API;
using LabFlow.API.Configuration;
using LabFlow.API.Data;
using LabFlow.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/labflow-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting LabFlow FHIR API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add Controllers for FHIR endpoints
    // Note: We register a custom FhirJsonOutputFormatter to handle FHIR resources correctly
    builder.Services.AddControllers(options =>
    {
        // CRITICAL: Add FhirJsonOutputFormatter FIRST so it takes precedence for FHIR resources
        var fhirSerializer = new FhirJsonSerializer();
        options.OutputFormatters.Insert(0, new FhirJsonOutputFormatter(fhirSerializer));
    });

    // Configure Database with Entity Framework Core
    // Using SQLite for development (easy setup, no server needed)
    // Switch to PostgreSQL for production by changing to options.UseNpgsql()
    builder.Services.AddDbContext<FhirDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("FhirDatabase")));

    // Register Firely FHIR serializers with strict validation
    builder.Services.AddSingleton(new FhirJsonSerializer());

    // Configure parser with strict validation for data quality
    var parserSettings = new ParserSettings
    {
        AcceptUnknownMembers = false,      // Reject non-FHIR fields
        AllowUnrecognizedEnums = false     // Reject invalid enum values
    };
    builder.Services.AddSingleton(new FhirJsonParser(parserSettings));

    // ⚠️ TESTING MODE: Disable authentication if DISABLE_AUTH_FOR_TESTING is set
    // CRITICAL: This should NEVER be used in production!
    var disableAuthForTesting = builder.Configuration.GetValue<bool>("DISABLE_AUTH_FOR_TESTING", false);

    if (disableAuthForTesting)
    {
        Log.Warning("⚠️ AUTHENTICATION DISABLED FOR TESTING - DO NOT USE IN PRODUCTION!");

        // Add fake authentication scheme that always succeeds
        builder.Services.AddAuthentication("TestScheme")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

        builder.Services.AddAuthorization(options =>
        {
            // Override ALL authorization requirements to allow anonymous access
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("TestScheme")
                .RequireAssertion(_ => true) // Always succeed
                .Build();

            options.FallbackPolicy = options.DefaultPolicy;
        });
    }
    else
    {
        // PRODUCTION MODE: Full JWT authentication
        Log.Information("JWT Authentication enabled (production mode)");

        // Configure JWT Settings from appsettings.json
        var jwtSettings = new JwtSettings();
        builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);
        jwtSettings.Validate(); // Validate on startup
        builder.Services.AddSingleton(jwtSettings);

        // Register Authentication Service
        builder.Services.AddScoped<IAuthService, AuthService>();

        // Configure JWT Authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = true; // ✅ HTTPS enforcement
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromSeconds(5), // 5 seconds tolerance for clock skew (industry standard)
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };

            // Audit logging for authentication events
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var userId = context.Principal?.FindFirst("sub")?.Value;
                    Log.Information("JWT token validated successfully for UserId: {UserId}", userId);
                    return System.Threading.Tasks.Task.CompletedTask;
                }
            };
        });

        builder.Services.AddAuthorization();
    }

    // Add Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "LabFlow FHIR API",
            Version = "v1",
            Description = "FHIR R4 compliant API for laboratory results interoperability with JWT authentication",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "David",
                Email = "contact@labflow.com"
            }
        });

        // Add JWT Bearer authentication to Swagger UI
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below. Example: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Configure CORS with restricted origins (FHIR security best practice)
    var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
                         ?? new[] { "http://localhost:3000", "http://localhost:5173" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FhirCors", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required for JWT tokens
        });
    });

    var app = builder.Build();

    // Apply database migrations automatically on startup
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<FhirDbContext>();
        Log.Information("Applying database migrations...");
        dbContext.Database.Migrate();
        Log.Information("Database migrations applied successfully");
    }

    // Configure HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "LabFlow FHIR API v1");
            c.RoutePrefix = string.Empty; // Swagger at root
        });
    }

    // HTTPS redirection (security best practice)
    app.UseHttpsRedirection();

    // HSTS (HTTP Strict Transport Security) for production
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts(); // Forces HTTPS for future requests
    }

    // CORS with restricted origins
    app.UseCors("FhirCors");

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

    // Authentication & Authorization (ORDER MATTERS)
    app.UseAuthentication(); // ← Must be before UseAuthorization
    app.UseAuthorization();

    app.MapControllers();

    // Simple health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithName("HealthCheck")
        .WithOpenApi();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class visible to integration tests
public partial class Program { }
