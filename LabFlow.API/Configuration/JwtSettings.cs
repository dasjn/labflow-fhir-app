namespace LabFlow.API.Configuration;

/// <summary>
/// JWT authentication configuration settings
/// Supports both HS256 (symmetric) and RS256 (asymmetric) algorithms
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for HS256 signature (minimum 256 bits / 32 characters)
    /// NEVER commit this to source control - use environment variables or Azure Key Vault in production
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (quien emite el token)
    /// Example: "LabFlowAPI" or "https://labflow.com"
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience (para quien es el token)
    /// Example: "LabFlowClients" or "https://labflow.com/api"
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes
    /// FHIR security best practice: short-lived tokens (30-60 minutes)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration in days (optional - for implementing refresh flow)
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Authentication provider type
    /// Allows easy migration: "JWT" (current) → "SMART" (future)
    /// </summary>
    public string AuthProvider { get; set; } = "JWT";

    /// <summary>
    /// Validate settings on startup
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException("JWT SecretKey is required");

        if (SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters (256 bits)");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("JWT Issuer is required");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("JWT Audience is required");

        if (ExpirationMinutes < 1)
            throw new InvalidOperationException("JWT ExpirationMinutes must be positive");
    }
}
