namespace LabFlow.API.Services;

/// <summary>
/// Authentication service interface
/// Handles user registration, login, and JWT token generation
/// Designed for easy migration to SMART on FHIR (change implementation, keep interface)
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="email">User email (unique)</param>
    /// <param name="password">Plain text password (will be hashed with BCrypt)</param>
    /// <param name="role">User role (Doctor, LabTechnician, Admin)</param>
    /// <param name="fullName">Optional full name</param>
    /// <returns>User ID if successful, null if email already exists</returns>
    Task<(bool Success, string? UserId, string? ErrorMessage)> RegisterAsync(
        string email,
        string password,
        string role,
        string? fullName = null);

    /// <summary>
    /// Authenticate user and generate JWT token
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">Plain text password</param>
    /// <returns>JWT token if successful, error message if failed</returns>
    Task<(bool Success, string? Token, int? ExpiresIn, string? ErrorMessage)> LoginAsync(
        string email,
        string password);

    /// <summary>
    /// Validate JWT token (for refresh token scenarios)
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if valid, null if invalid/expired</returns>
    Task<string?> ValidateTokenAsync(string token);
}
