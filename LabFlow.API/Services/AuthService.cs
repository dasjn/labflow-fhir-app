using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LabFlow.API.Configuration;
using LabFlow.API.Data;
using LabFlow.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;

namespace LabFlow.API.Services;

/// <summary>
/// JWT authentication service implementation
/// Uses BCrypt for password hashing and HS256 for JWT signing
/// Includes comprehensive audit logging (FHIR AuditEvent compatible)
/// </summary>
public class AuthService : IAuthService
{
    private readonly FhirDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        FhirDbContext context,
        JwtSettings jwtSettings,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user with BCrypt password hashing
    /// </summary>
    public async Task<(bool Success, string? UserId, string? ErrorMessage)> RegisterAsync(
        string email,
        string password,
        string role,
        string? fullName = null)
    {
        try
        {
            // Audit log: Registration attempt
            _logger.LogInformation("User registration attempt for email: {Email}, role: {Role}", email, role);

            // Validate email format
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                _logger.LogWarning("Invalid email format: {Email}", email);
                return (false, null, "Invalid email format");
            }

            // Validate password strength
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                _logger.LogWarning("Password too weak for email: {Email}", email);
                return (false, null, "Password must be at least 8 characters");
            }

            // Validate role
            if (!UserRoles.IsValid(role))
            {
                _logger.LogWarning("Invalid role: {Role} for email: {Email}", role, email);
                return (false, null, $"Invalid role. Must be one of: {string.Join(", ", UserRoles.All)}");
            }

            // Check if email already exists
            var existingUser = await _context.Users
                .Where(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email already exists: {Email}", email);
                return (false, null, "Email already exists");
            }

            // Hash password with BCrypt (work factor: 11 = 2^11 iterations)
            // BCrypt automatically generates salt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

            // Create user entity
            var user = new UserEntity
            {
                Id = Guid.NewGuid().ToString(),
                Email = email.ToLower(), // Normalize email
                PasswordHash = passwordHash,
                Role = role,
                FullName = fullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Audit log: Successful registration
            _logger.LogInformation(
                "User registered successfully: UserId={UserId}, Email={Email}, Role={Role}",
                user.Id, email, role);

            return (true, user.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for email: {Email}", email);
            return (false, null, "An error occurred during registration");
        }
    }

    /// <summary>
    /// Authenticate user and generate JWT token
    /// </summary>
    public async Task<(bool Success, string? Token, int? ExpiresIn, string? ErrorMessage)> LoginAsync(
        string email,
        string password)
    {
        try
        {
            // Audit log: Login attempt (WHO is trying to access)
            _logger.LogInformation("Login attempt for email: {Email} from IP: (implement IP tracking)", email);

            // Find user by email
            var user = await _context.Users
                .Where(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found: {Email}", email);
                // Don't reveal if user exists (security best practice)
                return (false, null, null, "Invalid email or password");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: Account disabled: {Email}", email);
                return (false, null, null, "Account is disabled");
            }

            // Verify password with BCrypt
            bool passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!passwordValid)
            {
                _logger.LogWarning("Login failed: Invalid password for email: {Email}", email);
                return (false, null, null, "Invalid email or password");
            }

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            user.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user);

            // Audit log: Successful login (WHO accessed WHEN)
            _logger.LogInformation(
                "User logged in successfully: UserId={UserId}, Email={Email}, Role={Role}, Timestamp={Timestamp}",
                user.Id, email, user.Role, DateTime.UtcNow);

            return (true, token, _jwtSettings.ExpirationMinutes * 60, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", email);
            return (false, null, null, "An error occurred during login");
        }
    }

    /// <summary>
    /// Generate JWT token with claims
    /// </summary>
    private string GenerateJwtToken(UserEntity user)
    {
        // JWT claims (información en el token)
        var claims = new List<Claim>
        {
            // Standard JWT claims
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),           // Subject (user ID)
            new Claim(JwtRegisteredClaimNames.Email, user.Email),      // Email
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID (unique per token)
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()), // Issued at

            // Custom claims for authorization
            new Claim(ClaimTypes.Role, user.Role),                     // Role for [Authorize(Roles = "Doctor")]
            new Claim("role", user.Role),                              // Lowercase for compatibility

            // FHIR/SMART compatible claims (preparando para futura migración)
            new Claim("fhirUser", $"Practitioner/{user.Id}"),         // FHIR resource reference
            new Claim("scope", MapRoleToScope(user.Role))              // SMART scope equivalent
        };

        // Add full name if available
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            claims.Add(new Claim(ClaimTypes.Name, user.FullName));
        }

        // Generate signing key from secret
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create JWT token
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: now, // Token is valid immediately
            expires: now.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Map user role to SMART on FHIR scope (future compatibility)
    /// </summary>
    private string MapRoleToScope(string role)
    {
        return role switch
        {
            UserRoles.Admin => "user/*.* system/*.read system/*.write",
            UserRoles.Doctor => "user/*.read user/Patient.write user/Observation.read user/DiagnosticReport.read",
            UserRoles.LabTechnician => "user/Observation.* user/DiagnosticReport.* user/ServiceRequest.read",
            _ => "user/*.read"
        };
    }

    /// <summary>
    /// Validate JWT token (for refresh scenarios)
    /// </summary>
    public async Task<string?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromSeconds(5) // 5 seconds tolerance for clock skew (industry standard)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // After ValidateToken, JWT "sub" claim is mapped to ClaimTypes.NameIdentifier
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation("Token validated successfully for UserId: {UserId}", userId);

            return userId;
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed with unexpected error: {Message}", ex.Message);
            return null;
        }
    }
}
