namespace LabFlow.API.Data.Entities;

/// <summary>
/// Database entity for storing FHIR DiagnosticReport resources
/// REQ-FHIR-004: Store diagnostic reports that group multiple laboratory observations
/// </summary>
public class DiagnosticReportEntity
{
    /// <summary>
    /// FHIR Resource ID (UUID format)
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Complete FHIR DiagnosticReport resource stored as JSON
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
    /// LOINC code for the report type - extracted for search
    /// FHIR search parameter: code
    /// Example: "58410-2" for Complete blood count (CBC) panel
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Display text for the code - for debugging/logging
    /// Example: "Complete blood count (CBC) panel - Blood by Automated count"
    /// </summary>
    public string? CodeDisplay { get; set; }

    /// <summary>
    /// Report status - extracted for search
    /// FHIR search parameter: status
    /// Values: registered, partial, preliminary, final, amended, corrected, cancelled, etc.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Service category - extracted for search
    /// FHIR search parameter: category
    /// Common values: "LAB" (laboratory), "RAD" (radiology), "PATH" (pathology), etc.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Date/time the study was performed - extracted for search
    /// FHIR search parameter: date
    /// </summary>
    public DateTime? EffectiveDateTime { get; set; }

    /// <summary>
    /// Date/time the report was issued/published - extracted for search
    /// FHIR search parameter: issued
    /// </summary>
    public DateTime? Issued { get; set; }

    /// <summary>
    /// Comma-separated list of Observation IDs that are part of this report
    /// FHIR search parameter: result
    /// Format: "obs1-id,obs2-id,obs3-id"
    /// Used to search for reports containing specific observations
    /// </summary>
    public string? ResultIds { get; set; }

    /// <summary>
    /// Clinical conclusion/interpretation of the report - extracted for search
    /// FHIR element: conclusion
    /// Example: "All values within normal range"
    /// </summary>
    public string? Conclusion { get; set; }

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
