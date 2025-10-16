using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using LabFlow.API.Data;
using LabFlow.API.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabFlow.API.Controllers;

/// <summary>
/// FHIR ServiceRequest Resource endpoint (laboratory orders)
/// REQ-FHIR-005: Implement ServiceRequest CRUD operations following FHIR R4 specification
/// REQ-FHIR-006: Secure with JWT authentication - Doctors and Admins only
/// </summary>
[Authorize(Roles = "Doctor,Admin")] // Only doctors and admins can order tests
[ApiController]
[Route("[controller]")]
[Produces("application/fhir+json", "application/json")]
public class ServiceRequestController : ControllerBase
{
    private readonly FhirDbContext _context;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;
    private readonly ILogger<ServiceRequestController> _logger;

    public ServiceRequestController(
        FhirDbContext context,
        FhirJsonSerializer serializer,
        FhirJsonParser parser,
        ILogger<ServiceRequestController> logger)
    {
        _context = context;
        _serializer = serializer;
        _parser = parser;
        _logger = logger;
    }

    /// <summary>
    /// Read a ServiceRequest resource by ID
    /// FHIR operation: GET [base]/ServiceRequest/[id]
    /// </summary>
    /// <param name="id">FHIR Resource ID</param>
    /// <returns>ServiceRequest resource or OperationOutcome if not found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceRequest(string id)
    {
        _logger.LogInformation("GET ServiceRequest/{Id}", id);

        var serviceRequestEntity = await _context.ServiceRequests
            .Where(sr => sr.Id == id && !sr.IsDeleted)
            .FirstOrDefaultAsync();

        if (serviceRequestEntity == null)
        {
            _logger.LogWarning("ServiceRequest {Id} not found", id);
            return NotFound(CreateOperationOutcome(
                $"ServiceRequest with ID '{id}' not found",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.NotFound));
        }

        var serviceRequest = _parser.Parse<ServiceRequest>(serviceRequestEntity.FhirJson);

        serviceRequest.Meta = new Meta
        {
            VersionId = serviceRequestEntity.VersionId.ToString(),
            LastUpdated = serviceRequestEntity.LastUpdated
        };

        return Ok(serviceRequest);
    }

    /// <summary>
    /// Search for ServiceRequest resources
    /// FHIR operation: GET [base]/ServiceRequest?[parameters]
    /// </summary>
    /// <param name="patient">Search by patient reference (format: Patient/[id] or just [id])</param>
    /// <param name="code">Search by LOINC code</param>
    /// <param name="status">Search by status (draft, active, completed, etc.)</param>
    /// <param name="intent">Search by intent (proposal, plan, order, etc.)</param>
    /// <param name="category">Search by category</param>
    /// <param name="authored">Search by authored date (format: YYYY-MM-DD)</param>
    /// <param name="requester">Search by requester reference</param>
    /// <param name="performer">Search by performer reference</param>
    /// <returns>Bundle with search results</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Bundle), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchServiceRequests(
        [FromQuery] string? patient,
        [FromQuery] string? code,
        [FromQuery] string? status,
        [FromQuery] string? intent,
        [FromQuery] string? category,
        [FromQuery] string? authored,
        [FromQuery] string? requester,
        [FromQuery] string? performer)
    {
        _logger.LogInformation(
            "GET ServiceRequest - Search with patient={Patient}, code={Code}, status={Status}, intent={Intent}, category={Category}, authored={Authored}, requester={Requester}, performer={Performer}",
            patient, code, status, intent, category, authored, requester, performer);

        var query = _context.ServiceRequests.Where(sr => !sr.IsDeleted).AsQueryable();

        // Filter by patient reference
        if (!string.IsNullOrEmpty(patient))
        {
            // Handle both "Patient/123" and "123" formats
            var patientId = patient.StartsWith("Patient/")
                ? patient.Substring("Patient/".Length)
                : patient;

            query = query.Where(sr => sr.PatientId == patientId);
        }

        // Filter by LOINC code
        if (!string.IsNullOrEmpty(code))
        {
            query = query.Where(sr => sr.Code == code);
        }

        // Filter by status
        if (!string.IsNullOrEmpty(status))
        {
            var statusLower = status.ToLower();
            query = query.Where(sr => sr.Status == statusLower);
        }

        // Filter by intent
        if (!string.IsNullOrEmpty(intent))
        {
            var intentLower = intent.ToLower();
            query = query.Where(sr => sr.Intent == intentLower);
        }

        // Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(sr => sr.Category == category);
        }

        // Filter by authored date
        if (!string.IsNullOrEmpty(authored))
        {
            if (!DateTime.TryParse(authored, out var parsedDate))
            {
                return BadRequest(CreateOperationOutcome(
                    $"Invalid date format: '{authored}'. Expected format: YYYY-MM-DD",
                    OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Invalid));
            }

            query = query.Where(sr => sr.AuthoredOn.HasValue &&
                sr.AuthoredOn.Value.Date == parsedDate.Date);
        }

        // Filter by requester
        if (!string.IsNullOrEmpty(requester))
        {
            query = query.Where(sr => sr.RequesterId == requester);
        }

        // Filter by performer
        if (!string.IsNullOrEmpty(performer))
        {
            query = query.Where(sr => sr.PerformerId == performer);
        }

        var serviceRequestEntities = await query.ToListAsync();

        _logger.LogInformation("Found {Count} service requests matching search criteria", serviceRequestEntities.Count);

        // Build FHIR Bundle
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Searchset,
            Total = serviceRequestEntities.Count,
            Entry = new List<Bundle.EntryComponent>()
        };

        foreach (var entity in serviceRequestEntities)
        {
            var serviceRequest = _parser.Parse<ServiceRequest>(entity.FhirJson);

            serviceRequest.Meta = new Meta
            {
                VersionId = entity.VersionId.ToString(),
                LastUpdated = entity.LastUpdated
            };

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{Request.Scheme}://{Request.Host}/ServiceRequest/{serviceRequest.Id}",
                Resource = serviceRequest,
                Search = new Bundle.SearchComponent
                {
                    Mode = Bundle.SearchEntryMode.Match
                }
            });
        }

        return Ok(bundle);
    }

    /// <summary>
    /// Update an existing ServiceRequest resource
    /// FHIR operation: PUT [base]/ServiceRequest/[id]
    /// </summary>
    /// <param name="id">FHIR Resource ID</param>
    /// <returns>Updated service request resource</returns>
    [HttpPut("{id}")]
    [Consumes("application/json", "application/fhir+json")]
    [ProducesResponseType(typeof(ServiceRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateServiceRequest(string id)
    {
        _logger.LogInformation("PUT ServiceRequest/{Id} - Updating service request", id);

        var contentType = Request.ContentType?.ToLower();
        if (contentType == null ||
            (!contentType.Contains("application/json") &&
             !contentType.Contains("application/fhir+json")))
        {
            return BadRequest(CreateOperationOutcome(
                "Content-Type must be application/json or application/fhir+json",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Structure));
        }

        var existingEntity = await _context.ServiceRequests
            .Where(sr => sr.Id == id && !sr.IsDeleted)
            .FirstOrDefaultAsync();

        if (existingEntity == null)
        {
            _logger.LogWarning("ServiceRequest {Id} not found for update", id);
            return NotFound(CreateOperationOutcome(
                $"ServiceRequest with ID '{id}' not found",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.NotFound));
        }

        Request.EnableBuffering();
        string serviceRequestJson;
        using (var reader = new StreamReader(
            Request.Body,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true))
        {
            serviceRequestJson = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(serviceRequestJson))
        {
            return BadRequest(CreateOperationOutcome(
                "Request body is empty",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Required));
        }

        ServiceRequest serviceRequest;
        try
        {
            serviceRequest = _parser.Parse<ServiceRequest>(serviceRequestJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse ServiceRequest JSON: {Error}", ex.Message);
            return BadRequest(CreateOperationOutcome(
                $"Invalid FHIR ServiceRequest JSON: {ex.Message}",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Structure));
        }

        var validationResult = ValidateServiceRequest(serviceRequest);
        if (!validationResult.Success)
        {
            _logger.LogWarning("ServiceRequest validation failed: {Error}", validationResult.Error);
            return BadRequest(CreateOperationOutcome(
                validationResult.Error,
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid));
        }

        // Validate patient reference
        if (!string.IsNullOrEmpty(serviceRequest.Subject?.Reference))
        {
            var patientId = serviceRequest.Subject.Reference.StartsWith("Patient/")
                ? serviceRequest.Subject.Reference.Substring("Patient/".Length)
                : serviceRequest.Subject.Reference;

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == patientId && !p.IsDeleted);
            if (!patientExists)
            {
                return BadRequest(CreateOperationOutcome(
                    $"Referenced patient '{serviceRequest.Subject.Reference}' does not exist",
                    OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Invalid));
            }
        }

        if (!string.IsNullOrEmpty(serviceRequest.Id) && serviceRequest.Id != id)
        {
            return BadRequest(CreateOperationOutcome(
                $"Resource ID '{serviceRequest.Id}' does not match URL ID '{id}'",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid));
        }

        serviceRequest.Id = id;

        var newVersion = existingEntity.VersionId + 1;
        var now = DateTime.UtcNow;

        serviceRequest.Meta = new Meta
        {
            VersionId = newVersion.ToString(),
            LastUpdated = now
        };

        var fhirJson = _serializer.SerializeToString(serviceRequest);

        var patientRef = serviceRequest.Subject?.Reference;
        var extractedPatientId = !string.IsNullOrEmpty(patientRef) && patientRef.StartsWith("Patient/")
            ? patientRef.Substring("Patient/".Length)
            : patientRef;

        var requesterRef = serviceRequest.Requester?.Reference;
        var performerRef = serviceRequest.Performer?.FirstOrDefault()?.Reference;

        existingEntity.FhirJson = fhirJson;
        existingEntity.PatientId = extractedPatientId;
        existingEntity.Code = serviceRequest.Code?.Coding?.FirstOrDefault()?.Code;
        existingEntity.CodeDisplay = serviceRequest.Code?.Coding?.FirstOrDefault()?.Display;
        existingEntity.Status = serviceRequest.Status?.ToString()?.ToLower();
        existingEntity.Intent = serviceRequest.Intent?.ToString()?.ToLower();
        existingEntity.Category = serviceRequest.Category?.FirstOrDefault()?.Coding?.FirstOrDefault()?.Code;
        existingEntity.Priority = serviceRequest.Priority?.ToString()?.ToLower();
        existingEntity.AuthoredOn = serviceRequest.AuthoredOn != null
            ? DateTime.Parse(serviceRequest.AuthoredOn)
            : null;
        existingEntity.RequesterId = requesterRef;
        existingEntity.PerformerId = performerRef;
        existingEntity.OccurrenceDateTime = serviceRequest.Occurrence is FhirDateTime occurrenceDateTime
            ? DateTime.Parse(occurrenceDateTime.Value)
            : null;
        existingEntity.LastUpdated = now;
        existingEntity.VersionId = newVersion;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated ServiceRequest {Id} to version {Version}", id, newVersion);

        return Ok(serviceRequest);
    }

    /// <summary>
    /// Delete a ServiceRequest resource (soft delete)
    /// FHIR operation: DELETE [base]/ServiceRequest/[id]
    /// </summary>
    /// <param name="id">FHIR Resource ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteServiceRequest(string id)
    {
        _logger.LogInformation("DELETE ServiceRequest/{Id} - Soft deleting service request", id);

        var serviceRequestEntity = await _context.ServiceRequests
            .Where(sr => sr.Id == id && !sr.IsDeleted)
            .FirstOrDefaultAsync();

        if (serviceRequestEntity == null)
        {
            _logger.LogWarning("ServiceRequest {Id} not found for deletion", id);
            return NotFound(CreateOperationOutcome(
                $"ServiceRequest with ID '{id}' not found",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.NotFound));
        }

        serviceRequestEntity.IsDeleted = true;
        serviceRequestEntity.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft deleted ServiceRequest {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Create a new ServiceRequest resource
    /// FHIR operation: POST [base]/ServiceRequest
    /// </summary>
    /// <returns>Created service request with Location header</returns>
    [HttpPost]
    [Consumes("application/json", "application/fhir+json")]
    [ProducesResponseType(typeof(ServiceRequest), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateServiceRequest()
    {
        _logger.LogInformation("POST ServiceRequest - Creating new service request");

        // Validate Content-Type
        var contentType = Request.ContentType?.ToLower();
        if (contentType == null ||
            (!contentType.Contains("application/json") &&
             !contentType.Contains("application/fhir+json")))
        {
            return BadRequest(CreateOperationOutcome(
                "Content-Type must be application/json or application/fhir+json",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Structure));
        }

        Request.EnableBuffering();

        string serviceRequestJson;
        using (var reader = new StreamReader(
            Request.Body,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true))
        {
            serviceRequestJson = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(serviceRequestJson))
        {
            return BadRequest(CreateOperationOutcome(
                "Request body is empty",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Required));
        }

        // Parse FHIR ServiceRequest
        ServiceRequest serviceRequest;
        try
        {
            serviceRequest = _parser.Parse<ServiceRequest>(serviceRequestJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse ServiceRequest JSON: {Error}", ex.Message);
            return BadRequest(CreateOperationOutcome(
                $"Invalid FHIR ServiceRequest JSON: {ex.Message}",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Structure));
        }

        // Validate FHIR resource
        var validationResult = ValidateServiceRequest(serviceRequest);
        if (!validationResult.Success)
        {
            _logger.LogWarning("ServiceRequest validation failed: {Error}", validationResult.Error);
            return BadRequest(CreateOperationOutcome(
                validationResult.Error,
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid));
        }

        // Validate patient reference exists
        if (!string.IsNullOrEmpty(serviceRequest.Subject?.Reference))
        {
            var patientId = serviceRequest.Subject.Reference.StartsWith("Patient/")
                ? serviceRequest.Subject.Reference.Substring("Patient/".Length)
                : serviceRequest.Subject.Reference;

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == patientId && !p.IsDeleted);
            if (!patientExists)
            {
                return BadRequest(CreateOperationOutcome(
                    $"Referenced patient '{serviceRequest.Subject.Reference}' does not exist",
                    OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Invalid));
            }
        }

        // Generate new ID if not provided
        if (string.IsNullOrEmpty(serviceRequest.Id))
        {
            serviceRequest.Id = Guid.NewGuid().ToString();
        }

        // Check if ID already exists
        var exists = await _context.ServiceRequests.AnyAsync(sr => sr.Id == serviceRequest.Id);
        if (exists)
        {
            return BadRequest(CreateOperationOutcome(
                $"ServiceRequest with ID '{serviceRequest.Id}' already exists",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Duplicate));
        }

        // Set metadata
        var now = DateTime.UtcNow;
        serviceRequest.Meta = new Meta
        {
            VersionId = "1",
            LastUpdated = now
        };

        // Serialize to JSON
        var fhirJson = _serializer.SerializeToString(serviceRequest);

        // Extract searchable fields
        var patientRef = serviceRequest.Subject?.Reference;
        var extractedPatientId = !string.IsNullOrEmpty(patientRef) && patientRef.StartsWith("Patient/")
            ? patientRef.Substring("Patient/".Length)
            : patientRef;

        var requesterRef = serviceRequest.Requester?.Reference;
        var performerRef = serviceRequest.Performer?.FirstOrDefault()?.Reference;

        var serviceRequestEntity = new ServiceRequestEntity
        {
            Id = serviceRequest.Id,
            FhirJson = fhirJson,
            PatientId = extractedPatientId,
            Code = serviceRequest.Code?.Coding?.FirstOrDefault()?.Code,
            CodeDisplay = serviceRequest.Code?.Coding?.FirstOrDefault()?.Display,
            Status = serviceRequest.Status?.ToString()?.ToLower(),
            Intent = serviceRequest.Intent?.ToString()?.ToLower(),
            Category = serviceRequest.Category?.FirstOrDefault()?.Coding?.FirstOrDefault()?.Code,
            Priority = serviceRequest.Priority?.ToString()?.ToLower(),
            AuthoredOn = serviceRequest.AuthoredOn != null
                ? DateTime.Parse(serviceRequest.AuthoredOn)
                : null,
            RequesterId = requesterRef,
            PerformerId = performerRef,
            OccurrenceDateTime = serviceRequest.Occurrence is FhirDateTime occurrenceDateTime
                ? DateTime.Parse(occurrenceDateTime.Value)
                : null,
            CreatedAt = now,
            LastUpdated = now,
            VersionId = 1
        };

        _context.ServiceRequests.Add(serviceRequestEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created ServiceRequest {Id}", serviceRequest.Id);

        return CreatedAtAction(
            nameof(GetServiceRequest),
            new { id = serviceRequest.Id },
            serviceRequest);
    }

    #region Helper Methods

    /// <summary>
    /// Validate ServiceRequest resource
    /// </summary>
    private (bool Success, string Error) ValidateServiceRequest(ServiceRequest serviceRequest)
    {
        if (serviceRequest == null)
        {
            return (false, "ServiceRequest resource is required");
        }

        // FHIR requires status
        if (serviceRequest.Status == null)
        {
            return (false, "ServiceRequest must have a status (draft, active, on-hold, revoked, completed, entered-in-error, unknown)");
        }

        // FHIR requires intent
        if (serviceRequest.Intent == null)
        {
            return (false, "ServiceRequest must have an intent (proposal, plan, directive, order, original-order, reflex-order, filler-order, instance-order, option)");
        }

        // FHIR requires subject
        if (serviceRequest.Subject == null || string.IsNullOrEmpty(serviceRequest.Subject.Reference))
        {
            return (false, "ServiceRequest must have a subject reference (patient)");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Create FHIR OperationOutcome for errors
    /// </summary>
    private OperationOutcome CreateOperationOutcome(
        string message,
        OperationOutcome.IssueSeverity severity,
        OperationOutcome.IssueType code)
    {
        return new OperationOutcome
        {
            Issue = new List<OperationOutcome.IssueComponent>
            {
                new OperationOutcome.IssueComponent
                {
                    Severity = severity,
                    Code = code,
                    Diagnostics = message
                }
            }
        };
    }

    #endregion
}
