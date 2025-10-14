namespace LabFlow.API.Data.Entities;

/// <summary>
/// User entity for JWT authentication
/// Stores hashed passwords using BCrypt (NEVER plain text)
/// </summary>
public class UserEntity
{
    /// <summary>
    /// Unique user identifier (GUID)
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User email (unique, used for login)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt password hash (60 characters)
    /// Format: $2a$11$... (algorithm + cost + salt + hash)
    /// NEVER store plain text passwords
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User role for authorization
    /// Values: "Doctor", "LabTechnician", "Admin"
    /// Maps to FHIR compartments and scopes (future SMART on FHIR compatibility)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Optional: User's full name
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last login timestamp (for audit trail)
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Account active status
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Soft delete flag (audit trail requirement)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// User roles for role-based authorization
/// Compatible with future SMART on FHIR scopes mapping:
/// - Doctor → user/*.read, user/*.write
/// - LabTechnician → user/Observation.*, user/DiagnosticReport.*
/// - Admin → Full access
/// </summary>
public static class UserRoles
{
    public const string Doctor = "Doctor";
    public const string LabTechnician = "LabTechnician";
    public const string Admin = "Admin";

    public static readonly string[] All = { Doctor, LabTechnician, Admin };

    public static bool IsValid(string role)
    {
        return All.Contains(role);
    }
}
