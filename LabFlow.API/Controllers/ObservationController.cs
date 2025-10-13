using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using LabFlow.API.Data;
using LabFlow.API.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabFlow.API.Controllers;

/// <summary>
/// FHIR Observation Resource endpoint (laboratory results)
/// REQ-FHIR-002: Implement Observation CRUD operations following FHIR R4 specification
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/fhir+json", "application/json")]
public class ObservationController : ControllerBase
{
    private readonly FhirDbContext _context;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;
    private readonly ILogger<ObservationController> _logger;

    public ObservationController(
        FhirDbContext context,
        FhirJsonSerializer serializer,
        FhirJsonParser parser,
        ILogger<ObservationController> logger)
    {
        _context = context;
        _serializer = serializer;
        _parser = parser;
        _logger = logger;
    }

    /// <summary>
    /// Read an Observation resource by ID
    /// FHIR operation: GET [base]/Observation/[id]
    /// </summary>
    /// <param name="id">FHIR Resource ID</param>
    /// <returns>Observation resource or OperationOutcome if not found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Observation), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetObservation(string id)
    {
        _logger.LogInformation("GET Observation/{Id}", id);

        var observationEntity = await _context.Observations
            .Where(o => o.Id == id && !o.IsDeleted)
            .FirstOrDefaultAsync();

        if (observationEntity == null)
        {
            _logger.LogWarning("Observation {Id} not found", id);
            return NotFound(CreateOperationOutcome(
                $"Observation with ID '{id}' not found",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.NotFound));
        }

        var observation = _parser.Parse<Observation>(observationEntity.FhirJson);

        observation.Meta = new Meta
        {
            VersionId = observationEntity.VersionId.ToString(),
            LastUpdated = observationEntity.LastUpdated
        };

        return Ok(observation);
    }

    /// <summary>
    /// Search for Observation resources
    /// FHIR operation: GET [base]/Observation?[parameters]
    /// </summary>
    /// <param name="patient">Search by patient reference (format: Patient/[id] or just [id])</param>
    /// <param name="code">Search by LOINC code</param>
    /// <param name="category">Search by category (e.g., laboratory, vital-signs)</param>
    /// <param name="date">Search by observation date (format: YYYY-MM-DD)</param>
    /// <param name="status">Search by status (final, preliminary, etc.)</param>
    /// <returns>Bundle with search results</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Bundle), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchObservations(
        [FromQuery] string? patient,
        [FromQuery] string? code,
        [FromQuery] string? category,
        [FromQuery] string? date,
        [FromQuery] string? status)
    {
        _logger.LogInformation(
            "GET Observation - Search with patient={Patient}, code={Code}, category={Category}, date={Date}, status={Status}",
            patient, code, category, date, status);

        var query = _context.Observations.Where(o => !o.IsDeleted).AsQueryable();

        // Filter by patient reference
        if (!string.IsNullOrEmpty(patient))
        {
            // Handle both "Patient/123" and "123" formats
            var patientId = patient.StartsWith("Patient/")
                ? patient.Substring("Patient/".Length)
                : patient;

            query = query.Where(o => o.PatientId == patientId);
        }

        // Filter by LOINC code
        if (!string.IsNullOrEmpty(code))
        {
            query = query.Where(o => o.Code == code);
        }

        // Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            var categoryLower = category.ToLower();
            query = query.Where(o => o.Category == categoryLower);
        }

        // Filter by date
        if (!string.IsNullOrEmpty(date))
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest(CreateOperationOutcome(
                    $"Invalid date format: '{date}'. Expected format: YYYY-MM-DD",
                    OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Invalid));
            }

            query = query.Where(o => o.EffectiveDateTime.HasValue &&
                o.EffectiveDateTime.Value.Date == parsedDate.Date);
        }

        // Filter by status
        if (!string.IsNullOrEmpty(status))
        {
            var statusLower = status.ToLower();
            query = query.Where(o => o.Status == statusLower);
        }

        var observationEntities = await query.ToListAsync();

        _logger.LogInformation("Found {Count} observations matching search criteria", observationEntities.Count);

        // Build FHIR Bundle
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Searchset,
            Total = observationEntities.Count,
            Entry = new List<Bundle.EntryComponent>()
        };

        foreach (var entity in observationEntities)
        {
            var observation = _parser.Parse<Observation>(entity.FhirJson);

            observation.Meta = new Meta
            {
                VersionId = entity.VersionId.ToString(),
                LastUpdated = entity.LastUpdated
            };

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{Request.Scheme}://{Request.Host}/Observation/{observation.Id}",
                Resource = observation,
                Search = new Bundle.SearchComponent
                {
                    Mode = Bundle.SearchEntryMode.Match
                }
            });
        }

        return Ok(bundle);
    }

    /// <summary>
    /// Create a new Observation resource
    /// FHIR operation: POST [base]/Observation
    /// </summary>
    /// <returns>Created observation with Location header</returns>
    [HttpPost]
    [Consumes("application/json", "application/fhir+json")]
    [ProducesResponseType(typeof(Observation), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateObservation()
    {
        _logger.LogInformation("POST Observation - Creating new observation");

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

        string observationJson;
        using (var reader = new StreamReader(
            Request.Body,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true))
        {
            observationJson = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(observationJson))
        {
            return BadRequest(CreateOperationOutcome(
                "Request body is empty",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Required));
        }

        // Parse FHIR Observation
        Observation observation;
        try
        {
            observation = _parser.Parse<Observation>(observationJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse Observation JSON: {Error}", ex.Message);
            return BadRequest(CreateOperationOutcome(
                $"Invalid FHIR Observation JSON: {ex.Message}",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Structure));
        }

        // Validate FHIR resource
        var validationResult = ValidateObservation(observation);
        if (!validationResult.Success)
        {
            _logger.LogWarning("Observation validation failed: {Error}", validationResult.Error);
            return BadRequest(CreateOperationOutcome(
                validationResult.Error,
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid));
        }

        // Validate patient reference exists
        if (!string.IsNullOrEmpty(observation.Subject?.Reference))
        {
            var patientId = observation.Subject.Reference.StartsWith("Patient/")
                ? observation.Subject.Reference.Substring("Patient/".Length)
                : observation.Subject.Reference;

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == patientId && !p.IsDeleted);
            if (!patientExists)
            {
                return BadRequest(CreateOperationOutcome(
                    $"Referenced patient '{observation.Subject.Reference}' does not exist",
                    OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Invalid));
            }
        }

        // Generate new ID if not provided
        if (string.IsNullOrEmpty(observation.Id))
        {
            observation.Id = Guid.NewGuid().ToString();
        }

        // Check if ID already exists
        var exists = await _context.Observations.AnyAsync(o => o.Id == observation.Id);
        if (exists)
        {
            return BadRequest(CreateOperationOutcome(
                $"Observation with ID '{observation.Id}' already exists",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Duplicate));
        }

        // Set metadata
        var now = DateTime.UtcNow;
        observation.Meta = new Meta
        {
            VersionId = "1",
            LastUpdated = now
        };

        // Serialize to JSON
        var fhirJson = _serializer.SerializeToString(observation);

        // Extract searchable fields
        var patientRef = observation.Subject?.Reference;
        var extractedPatientId = !string.IsNullOrEmpty(patientRef) && patientRef.StartsWith("Patient/")
            ? patientRef.Substring("Patient/".Length)
            : patientRef;

        var observationEntity = new ObservationEntity
        {
            Id = observation.Id,
            FhirJson = fhirJson,
            PatientId = extractedPatientId,
            Code = observation.Code?.Coding?.FirstOrDefault()?.Code,
            CodeDisplay = observation.Code?.Coding?.FirstOrDefault()?.Display,
            Status = observation.Status?.ToString()?.ToLower(),
            Category = observation.Category?.FirstOrDefault()?.Coding?.FirstOrDefault()?.Code?.ToLower(),
            EffectiveDateTime = observation.Effective is FhirDateTime effectiveDateTime
                ? DateTime.Parse(effectiveDateTime.Value)
                : null,
            ValueQuantity = observation.Value is Quantity quantity ? quantity.Value : null,
            ValueUnit = observation.Value is Quantity quantityUnit ? quantityUnit.Unit : null,
            ValueCodeableConcept = observation.Value is CodeableConcept codeableConcept
                ? codeableConcept.Coding?.FirstOrDefault()?.Code
                : null,
            CreatedAt = now,
            LastUpdated = now,
            VersionId = 1
        };

        _context.Observations.Add(observationEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created Observation {Id}", observation.Id);

        return CreatedAtAction(
            nameof(GetObservation),
            new { id = observation.Id },
            observation);
    }

    #region Helper Methods

    /// <summary>
    /// Validate Observation resource
    /// </summary>
    private (bool Success, string Error) ValidateObservation(Observation observation)
    {
        if (observation == null)
        {
            return (false, "Observation resource is required");
        }

        // FHIR requires status
        if (observation.Status == null)
        {
            return (false, "Observation must have a status (registered, preliminary, final, etc.)");
        }

        // FHIR requires code
        if (observation.Code == null || observation.Code.Coding == null || !observation.Code.Coding.Any())
        {
            return (false, "Observation must have a code (e.g., LOINC code)");
        }

        // Must have either subject or focus (we focus on subject/patient)
        if (observation.Subject == null || string.IsNullOrEmpty(observation.Subject.Reference))
        {
            return (false, "Observation must have a subject reference (patient)");
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
