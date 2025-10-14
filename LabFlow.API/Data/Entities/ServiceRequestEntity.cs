namespace LabFlow.API.Data.Entities;

/// <summary>
/// Database entity for storing FHIR ServiceRequest resources
/// REQ-FHIR-005: Store service requests (laboratory orders) with searchable fields for FHIR search parameters
/// </summary>
public class ServiceRequestEntity
{
    /// <summary>
    /// FHIR Resource ID (UUID format)
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Complete FHIR ServiceRequest resource stored as JSON
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
    /// LOINC code for the requested service/test - extracted for search
    /// FHIR search parameter: code
    /// Example: "2339-0" for Glucose test, "58410-2" for CBC panel
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Display text for the code - for debugging/logging
    /// Example: "Glucose [Mass/volume] in Blood"
    /// </summary>
    public string? CodeDisplay { get; set; }

    /// <summary>
    /// Request status - extracted for search
    /// FHIR search parameter: status (REQUIRED)
    /// Values: draft, active, on-hold, revoked, completed, entered-in-error, unknown
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Request intent - extracted for search
    /// FHIR search parameter: intent (REQUIRED)
    /// Values: proposal, plan, directive, order, original-order, reflex-order, filler-order, instance-order, option
    /// Common for lab orders: "order" (most common), "reflex-order" (automated follow-up)
    /// </summary>
    public string? Intent { get; set; }

    /// <summary>
    /// Category of service - extracted for search
    /// FHIR search parameter: category
    /// Common values: "Laboratory procedure" (SNOMED 108252007), "Radiology", "Pathology", etc.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Priority of the request - extracted for search
    /// FHIR search parameter: priority
    /// Values: routine, urgent, asap, stat
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Date/time when the request was authored/created - extracted for search
    /// FHIR search parameter: authored
    /// </summary>
    public DateTime? AuthoredOn { get; set; }

    /// <summary>
    /// Reference to the requester (Practitioner, Organization, etc.) - extracted for search
    /// FHIR search parameter: requester
    /// Format: "Practitioner/[id]" or "Organization/[id]"
    /// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Reference to the performer (who will fulfill the request) - extracted for search
    /// FHIR search parameter: performer
    /// Format: "Practitioner/[id]", "Organization/[id]", etc.
    /// Example: Laboratory organization that will process the test
    /// </summary>
    public string? PerformerId { get; set; }

    /// <summary>
    /// Date/time when the service should occur - extracted for search
    /// FHIR search parameter: occurrence
    /// Used for scheduling: "Please collect blood sample on 2025-10-15"
    /// </summary>
    public DateTime? OccurrenceDateTime { get; set; }

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
