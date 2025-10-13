using Hl7.Fhir.Serialization;
using LabFlow.API.Data;
using Microsoft.EntityFrameworkCore;
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
    builder.Services.AddControllers()
        .AddNewtonsoftJson(options =>
        {
            // Configure Newtonsoft.Json to work with FHIR resources
            // Firely SDK requires specific JSON settings
            options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
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

    // Add Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "LabFlow FHIR API",
            Version = "v1",
            Description = "FHIR R4 compliant API for laboratory results interoperability",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "David",
                Email = "contact@labflow.com"
            }
        });
    });

    // Add CORS (for development)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

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

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

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
