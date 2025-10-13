namespace LabFlow.API.Data.Entities;

/// <summary>
/// Database entity for storing FHIR Observation resources (laboratory results)
/// REQ-FHIR-002: Store lab observations with searchable fields for FHIR search parameters
/// </summary>
public class ObservationEntity
{
    /// <summary>
    /// FHIR Resource ID (UUID format)
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Complete FHIR Observation resource stored as JSON
    /// Maintains full data fidelity including all FHIR elements
    /// </summary>
    public string FhirJson { get; set; } = string.Empty;

    // Searchable fields extracted from FHIR resource for query performance
    // These enable FHIR search parameters without parsing JSON every time

    /// <summary>
    /// Reference to Patient (subject) - extracted for search
    /// FHIR search parameter: patient or subject
    /// Format: "Patient/[id]"
    /// </summary>
    public string? PatientId { get; set; }

    /// <summary>
    /// LOINC code for the observation type - extracted for search
    /// FHIR search parameter: code
    /// Example: "2339-0" for Glucose
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Display text for the code - for debugging/logging
    /// Example: "Glucose [Mass/volume] in Blood"
    /// </summary>
    public string? CodeDisplay { get; set; }

    /// <summary>
    /// Observation status - extracted for search
    /// FHIR search parameter: status
    /// Values: registered, preliminary, final, amended, corrected, cancelled, etc.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Category of observation - extracted for search
    /// FHIR search parameter: category
    /// Common values: "laboratory", "vital-signs", "imaging", etc.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Date/time the observation was made - extracted for search
    /// FHIR search parameter: date
    /// </summary>
    public DateTime? EffectiveDateTime { get; set; }

    /// <summary>
    /// Numeric value of the observation - extracted for search
    /// FHIR search parameter: value-quantity
    /// Example: 95.0 for glucose level
    /// </summary>
    public decimal? ValueQuantity { get; set; }

    /// <summary>
    /// Unit of measurement for the value - extracted for search
    /// Example: "mg/dL", "mmol/L"
    /// </summary>
    public string? ValueUnit { get; set; }

    /// <summary>
    /// Coded value (for non-numeric observations) - extracted for search
    /// FHIR search parameter: value-concept
    /// Example: "Positive", "Negative"
    /// </summary>
    public string? ValueCodeableConcept { get; set; }

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
    /// Supports audit trail and regulatory compliance (IEC 62304, FDA 21 CFR Part 11)
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}
