using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using LabFlow.API.Data;
using LabFlow.API.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabFlow.API.Controllers;

/// <summary>
/// FHIR DiagnosticReport Resource endpoint (grouped laboratory reports)
/// REQ-FHIR-004: Implement DiagnosticReport CRUD operations following FHIR R4 specification
/// REQ-FHIR-006: Secure with JWT authentication - All authenticated users
/// </summary>
[Authorize] // All authenticated users can access diagnostic reports
[ApiController]
[Route("[controller]")]
[Produces("application/fhir+json", "application/json")]
public class DiagnosticReportController : ControllerBase
{
    private readonly FhirDbContext _context;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;
    private readonly ILogger<DiagnosticReportController> _logger;

    public DiagnosticReportController(
        FhirDbContext context,
        FhirJsonSerializer serializer,
        FhirJsonParser parser,
        ILogger<DiagnosticReportController> logger)
    {
        _context = context;
        _serializer = serializer;
        _parser = parser;
        _logger = logger;
    }

    /// <summary>
    /// Read a DiagnosticReport resource by ID
    /// FHIR operation: GET [base]/DiagnosticReport/[id]
    /// </summary>
    /// <param name="id">FHIR Resource ID</param>
    /// <returns>DiagnosticReport resource or OperationOutcome if not found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DiagnosticReport), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiagnosticReport(string id)
    {
        _logger.LogInformation("GET DiagnosticReport/{Id}", id);

        var reportEntity = await _context.DiagnosticReports
            .Where(d => d.Id == id && !d.IsDeleted)
            .FirstOrDefaultAsync();

        if (reportEntity == null)
        {
            _logger.LogWarning("DiagnosticReport {Id} not found", id);
            return NotFound(CreateOperationOutcome(
                $"DiagnosticReport with ID '{id}' not found",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.NotFound));
        }

        var report = _parser.Parse<DiagnosticReport>(reportEntity.FhirJson);

        report.Meta = new Meta
        {
            VersionId = reportEntity.VersionId.ToString(),
            LastUpdated = reportEntity.LastUpdated
        };

        return Ok(report);
    }

    /// <summary>
    /// Search for DiagnosticReport resources
    /// FHIR operation: GET [base]/DiagnosticReport?[parameters]
    /// </summary>
    /// <param name="patient">Search by patient reference (format: Patient/[id] or just [id])</param>
    /// <param name="code">Search by report code (LOINC)</param>
    /// <param name="category">Search by category (e.g., LAB, RAD)</param>
    /// <param name="date">Search by effective date (format: YYYY-MM-DD)</param>
    /// <param name="issued">Search by issued date (format: YYYY-MM-DD)</param>
    /// <param name="status">Search by status (registered, partial, preliminary, final, etc.)</param>
    /// <returns>Bundle with search results</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Bundle), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchDiagnosticReports(
        [FromQuery] string? patient,
        [FromQuery] string? code,
        [FromQuery] string? category,
        [FromQuery] string? date,
        [FromQuery] string? issued,
        [FromQuery] string? status)
    {
        _logger.LogInformation(
            "GET DiagnosticReport - Search with patient={Patient}, code={Code}, category={Category}, date={Date}, issued={Issued}, status={Status}",
            patient, code, category, date, issued, status);

        var query = _context.DiagnosticReports.Where(d => !d.IsDeleted).AsQueryable();

        // Filter by patient reference
        if (!string.IsNullOrEmpty(patient))
        {
            // Handle both "Patient/123" and "123" formats
            var patientId = patient.StartsWith("Patient/")
                ? patient.Substring("Patient/".Length)
                : patient;

            query = query.Where(d => d.PatientId == patientId);
        }

        // Filter by LOINC code
        if (!string.IsNullOrEmpty(code))
        {
            query = query.Where(d => d.Code == code);
        }

        // Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            var categoryUpper = category.ToUpper();
            query = query.Where(d => d.Category == categoryUpper);
        }

        // Filter by effective date
        if (!string.IsNullOrEmpty(date))
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest(CreateOperationOutcome(
                    $"Invalid date format: '{date}'. Expected format: YYYY-MM-DD",
                    OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Invalid));
            }

            query = query.Where(d => d.EffectiveDateTime.HasValue &&
                d.EffectiveDateTime.Value.Date == parsedDate.Date);
        }

        // Filter by issued date
        if (!string.IsNullOrEmpty(issued))
        {
            if (!DateTime.TryParse(issued, out var parsedIssued))
            {
                return BadRequest(CreateOperationOutcome(
                    $"Invalid issued date format: '{issued}'. Expected format: YYYY-MM-DD",
                    OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Invalid));
            }

            query = query.Where(d => d.Issued.HasValue &&
                d.Issued.Value.Date == parsedIssued.Date);
        }

        // Filter by status
        if (!string.IsNullOrEmpty(status))
        {
            var statusLower = status.ToLower();
            query = query.Where(d => d.Status == statusLower);
        }

        var reportEntities = await query.ToListAsync();

        _logger.LogInformation("Found {Count} diagnostic reports matching search criteria", reportEntities.Count);

        // Build FHIR Bundle
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Searchset,
            Total = reportEntities.Count,
            Entry = new List<Bundle.EntryComponent>()
        };

        foreach (var entity in reportEntities)
        {
            var report = _parser.Parse<DiagnosticReport>(entity.FhirJson);

            report.Meta = new Meta
            {
                VersionId = entity.VersionId.ToString(),
                LastUpdated = entity.LastUpdated
            };

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{Request.Scheme}://{Request.Host}/DiagnosticReport/{report.Id}",
                Resource = report,
                Search = new Bundle.SearchComponent
                {
                    Mode = Bundle.SearchEntryMode.Match
                }
            });
        }

        return Ok(bundle);
    }

    /// <summary>
    /// Update an existing DiagnosticReport resource
    /// FHIR operation: PUT [base]/DiagnosticReport/[id]
    /// </summary>
    /// <param name="id">FHIR Resource ID</param>
    /// <returns>Updated diagnostic report resource</returns>
    [HttpPut("{id}")]
    [Consumes("application/json", "application/fhir+json")]
    [ProducesResponseType(typeof(DiagnosticReport), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDiagnosticReport(string id)
    {
        _logger.LogInformation("PUT DiagnosticReport/{Id} - Updating diagnostic report", id);

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

        // Check if diagnostic report exists
        var existingEntity = await _context.DiagnosticReports
            .Where(d => d.Id == id && !d.IsDeleted)
            .FirstOrDefaultAsync();

        if (existingEntity == null)
        {
            _logger.LogWarning("DiagnosticReport {Id} not found for update", id);
            return NotFound(CreateOperationOutcome(
                $"DiagnosticReport with ID '{id}' not found",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.NotFound));
        }

        // Read and parse request body
        Request.EnableBuffering();
        string reportJson;
        using (var reader = new StreamReader(
            Request.Body,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true))
        {
            reportJson = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(reportJson))
        {
            return BadRequest(CreateOperationOutcome(
                "Request body is empty",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Required));
        }

        DiagnosticReport report;
        try
        {
            report = _parser.Parse<DiagnosticReport>(reportJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse DiagnosticReport JSON: {Error}", ex.Message);
            return BadRequest(CreateOperationOutcome(
                $"Invalid FHIR DiagnosticReport JSON: {ex.Message}",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Structure));
        }

        // Validate resource
        var validationResult = await ValidateDiagnosticReport(report);
        if (!validationResult.Success)
        {
            _logger.LogWarning("DiagnosticReport validation failed: {Error}", validationResult.Error);
            return BadRequest(CreateOperationOutcome(
                validationResult.Error,
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid));
        }

        // Ensure ID matches
        if (!string.IsNullOrEmpty(report.Id) && report.Id != id)
        {
            return BadRequest(CreateOperationOutcome(
                $"Resource ID '{report.Id}' does not match URL ID '{id}'",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid));
        }

        report.Id = id;

        // Increment version
        var newVersion = existingEntity.VersionId + 1;
        var now = DateTime.UtcNow;

        report.Meta = new Meta
        {
            VersionId = newVersion.ToString(),
            LastUpdated = now
        };

        // Serialize to JSON
        var fhirJson = _serializer.SerializeToString(report);

        // Extract searchable fields
        var patientRef = report.Subject?.Reference;
        var extractedPatientId = !string.IsNullOrEmpty(patientRef) && patientRef.StartsWith("Patient/")
            ? patientRef.Substring("Patient/".Length)
            : patientRef;

        // Extract result observation IDs
        var resultIds = report.Result?.Select(r => r.Reference?.Replace("Observation/", ""))
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        // Update entity
        existingEntity.FhirJson = fhirJson;
        existingEntity.PatientId = extractedPatientId;
        existingEntity.Code = report.Code?.Coding?.FirstOrDefault()?.Code;
        existingEntity.CodeDisplay = report.Code?.Coding?.FirstOrDefault()?.Display;
        existingEntity.Status = report.Status?.ToString()?.ToLower();
        existingEntity.Category = report.Category?.FirstOrDefault()?.Coding?.FirstOrDefault()?.Code?.ToUpper();
        existingEntity.EffectiveDateTime = report.Effective is FhirDateTime effectiveDateTime
            ? DateTime.Parse(effectiveDateTime.Value)
            : report.Effective is Period period && period.StartElement != null
                ? DateTime.Parse(period.StartElement.Value)
                : null;
        existingEntity.Issued = report.IssuedElement?.Value?.UtcDateTime;
        existingEntity.ResultIds = resultIds != null && resultIds.Any() ? string.Join(",", resultIds) : null;
        existingEntity.Conclusion = report.Conclusion;
        existingEntity.LastUpdated = now;
        existingEntity.VersionId = newVersion;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated DiagnosticReport {Id} to version {Version}", id, newVersion);

        return Ok(report);
    }

    /// <summary>
    /// Delete a DiagnosticReport resource (soft delete)
    /// FHIR operation: DELETE [base]/DiagnosticReport/[id]
    /// </summary>
    /// <param name="id">FHIR Resource ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDiagnosticReport(string id)
    {
        _logger.LogInformation("DELETE DiagnosticReport/{Id} - Soft deleting diagnostic report", id);

        var reportEntity = await _context.DiagnosticReports
            .Where(d => d.Id == id && !d.IsDeleted)
            .FirstOrDefaultAsync();

        if (reportEntity == null)
        {
            _logger.LogWarning("DiagnosticReport {Id} not found for deletion", id);
            return NotFound(CreateOperationOutcome(
                $"DiagnosticReport with ID '{id}' not found",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.NotFound));
        }

        // Soft delete (preserves audit trail)
        reportEntity.IsDeleted = true;
        reportEntity.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft deleted DiagnosticReport {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Create a new DiagnosticReport resource
    /// FHIR operation: POST [base]/DiagnosticReport
    /// </summary>
    /// <returns>Created diagnostic report with Location header</returns>
    [HttpPost]
    [Consumes("application/json", "application/fhir+json")]
    [ProducesResponseType(typeof(DiagnosticReport), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDiagnosticReport()
    {
        _logger.LogInformation("POST DiagnosticReport - Creating new diagnostic report");

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

        string reportJson;
        using (var reader = new StreamReader(
            Request.Body,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true))
        {
            reportJson = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(reportJson))
        {
            return BadRequest(CreateOperationOutcome(
                "Request body is empty",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Required));
        }

        // Parse FHIR DiagnosticReport
        DiagnosticReport report;
        try
        {
            report = _parser.Parse<DiagnosticReport>(reportJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse DiagnosticReport JSON: {Error}", ex.Message);
            return BadRequest(CreateOperationOutcome(
                $"Invalid FHIR DiagnosticReport JSON: {ex.Message}",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Structure));
        }

        // Validate FHIR resource
        var validationResult = await ValidateDiagnosticReport(report);
        if (!validationResult.Success)
        {
            _logger.LogWarning("DiagnosticReport validation failed: {Error}", validationResult.Error);
            return BadRequest(CreateOperationOutcome(
                validationResult.Error,
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid));
        }

        // Generate new ID if not provided
        if (string.IsNullOrEmpty(report.Id))
        {
            report.Id = Guid.NewGuid().ToString();
        }

        // Check if ID already exists
        var exists = await _context.DiagnosticReports.AnyAsync(d => d.Id == report.Id);
        if (exists)
        {
            return BadRequest(CreateOperationOutcome(
                $"DiagnosticReport with ID '{report.Id}' already exists",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Duplicate));
        }

        // Set metadata
        var now = DateTime.UtcNow;
        report.Meta = new Meta
        {
            VersionId = "1",
            LastUpdated = now
        };

        // Serialize to JSON
        var fhirJson = _serializer.SerializeToString(report);

        // Extract searchable fields
        var patientRef = report.Subject?.Reference;
        var extractedPatientId = !string.IsNullOrEmpty(patientRef) && patientRef.StartsWith("Patient/")
            ? patientRef.Substring("Patient/".Length)
            : patientRef;

        // Extract result observation IDs
        var resultIds = report.Result?.Select(r => r.Reference?.Replace("Observation/", ""))
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        var reportEntity = new DiagnosticReportEntity
        {
            Id = report.Id,
            FhirJson = fhirJson,
            PatientId = extractedPatientId,
            Code = report.Code?.Coding?.FirstOrDefault()?.Code,
            CodeDisplay = report.Code?.Coding?.FirstOrDefault()?.Display,
            Status = report.Status?.ToString()?.ToLower(),
            Category = report.Category?.FirstOrDefault()?.Coding?.FirstOrDefault()?.Code?.ToUpper(),
            EffectiveDateTime = report.Effective is FhirDateTime effectiveDateTime
                ? DateTime.Parse(effectiveDateTime.Value)
                : report.Effective is Period period && period.StartElement != null
                    ? DateTime.Parse(period.StartElement.Value)
                    : null,
            Issued = report.IssuedElement?.Value?.UtcDateTime,
            ResultIds = resultIds != null && resultIds.Any() ? string.Join(",", resultIds) : null,
            Conclusion = report.Conclusion,
            CreatedAt = now,
            LastUpdated = now,
            VersionId = 1
        };

        _context.DiagnosticReports.Add(reportEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created DiagnosticReport {Id}", report.Id);

        return CreatedAtAction(
            nameof(GetDiagnosticReport),
            new { id = report.Id },
            report);
    }

    #region Helper Methods

    /// <summary>
    /// Validate DiagnosticReport resource
    /// </summary>
    private async Task<(bool Success, string Error)> ValidateDiagnosticReport(DiagnosticReport report)
    {
        if (report == null)
        {
            return (false, "DiagnosticReport resource is required");
        }

        // FHIR requires status
        if (report.Status == null)
        {
            return (false, "DiagnosticReport must have a status (registered, partial, preliminary, final, etc.)");
        }

        // FHIR requires code
        if (report.Code == null || report.Code.Coding == null || !report.Code.Coding.Any())
        {
            return (false, "DiagnosticReport must have a code (e.g., LOINC code for panel type)");
        }

        // FHIR requires subject (patient)
        if (report.Subject == null || string.IsNullOrEmpty(report.Subject.Reference))
        {
            return (false, "DiagnosticReport must have a subject reference (patient)");
        }

        // Validate patient reference exists
        var patientId = report.Subject.Reference.StartsWith("Patient/")
            ? report.Subject.Reference.Substring("Patient/".Length)
            : report.Subject.Reference;

        var patientExists = await _context.Patients.AnyAsync(p => p.Id == patientId && !p.IsDeleted);
        if (!patientExists)
        {
            return (false, $"Referenced patient '{report.Subject.Reference}' does not exist");
        }

        // Validate observation references exist (if provided)
        if (report.Result != null && report.Result.Any())
        {
            foreach (var resultRef in report.Result)
            {
                if (string.IsNullOrEmpty(resultRef.Reference))
                {
                    continue;
                }

                // Only validate Observation references
                if (!resultRef.Reference.StartsWith("Observation/"))
                {
                    return (false, $"Result reference '{resultRef.Reference}' must be an Observation resource");
                }

                var observationId = resultRef.Reference.Substring("Observation/".Length);
                var observationExists = await _context.Observations.AnyAsync(o => o.Id == observationId && !o.IsDeleted);
                if (!observationExists)
                {
                    return (false, $"Referenced observation '{resultRef.Reference}' does not exist");
                }
            }
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
