namespace LabFlow.API.Data.Entities;

/// <summary>
/// Database entity for storing FHIR Patient resources
/// REQ-FHIR-001: Store patients with searchable fields for FHIR search parameters
/// </summary>
public class PatientEntity
{
    /// <summary>
    /// FHIR Resource ID (UUID format)
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Complete FHIR Patient resource stored as JSON
    /// We store the full FHIR resource to maintain all data fidelity
    /// </summary>
    public string FhirJson { get; set; } = string.Empty;

    // Searchable fields extracted from FHIR resource for query performance
    // These enable FHIR search parameters without parsing JSON every time

    /// <summary>
    /// Patient's family name (last name) - extracted for search
    /// FHIR search parameter: name
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// Patient's given name (first name) - extracted for search
    /// FHIR search parameter: name
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    /// Patient identifier (e.g., medical record number) - extracted for search
    /// FHIR search parameter: identifier
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Patient's birth date - extracted for search
    /// FHIR search parameter: birthdate
    /// </summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// Patient's gender - extracted for search
    /// FHIR search parameter: gender
    /// Values: male, female, other, unknown
    /// </summary>
    public string? Gender { get; set; }

    // Metadata fields for tracking and audit trail

    /// <summary>
    /// When this resource was created in the database
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this resource was last updated
    /// FHIR search parameter: _lastUpdated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// FHIR version ID for optimistic concurrency control
    /// Increments with each update
    /// </summary>
    public int VersionId { get; set; } = 1;

    /// <summary>
    /// Soft delete flag - we don't actually delete FHIR resources
    /// Supports audit trail and regulatory compliance
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}
