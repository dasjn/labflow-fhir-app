using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using LabFlow.API.Data;
using LabFlow.API.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabFlow.API.Controllers;

/// <summary>
/// FHIR Patient Resource endpoint
/// REQ-FHIR-001: Implement Patient CRUD operations following FHIR R4 specification
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/fhir+json", "application/json")]
public class PatientController : ControllerBase
{
    private readonly FhirDbContext _context;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;
    private readonly ILogger<PatientController> _logger;

    public PatientController(
        FhirDbContext context,
        FhirJsonSerializer serializer,
        FhirJsonParser parser,
        ILogger<PatientController> logger)
    {
        _context = context;
        _serializer = serializer;
        _parser = parser;
        _logger = logger;
    }

    /// <summary>
    /// Read a Patient resource by ID
    /// FHIR operation: GET [base]/Patient/[id]
    /// </summary>
    /// <param name="id">FHIR Resource ID</param>
    /// <returns>Patient resource or OperationOutcome if not found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Patient), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatient(string id)
    {
        _logger.LogInformation("GET Patient/{Id}", id);

        // Find patient in database
        var patientEntity = await _context.Patients
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (patientEntity == null)
        {
            _logger.LogWarning("Patient {Id} not found", id);
            return NotFound(CreateOperationOutcome(
                $"Patient with ID '{id}' not found",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.NotFound));
        }

        // Deserialize FHIR Patient from stored JSON using Firely SDK
        var patient = _parser.Parse<Patient>(patientEntity.FhirJson);

        // Update meta information
        patient.Meta = new Meta
        {
            VersionId = patientEntity.VersionId.ToString(),
            LastUpdated = patientEntity.LastUpdated
        };

        return Ok(patient);
    }

    /// <summary>
    /// Search for Patient resources
    /// FHIR operation: GET [base]/Patient?[parameters]
    /// </summary>
    /// <param name="name">Search by family or given name (partial match)</param>
    /// <param name="identifier">Search by identifier (exact match)</param>
    /// <param name="birthdate">Search by birth date (exact match, format: YYYY-MM-DD)</param>
    /// <param name="gender">Search by gender (male, female, other, unknown)</param>
    /// <returns>Bundle with search results</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Bundle), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPatients(
        [FromQuery] string? name,
        [FromQuery] string? identifier,
        [FromQuery] string? birthdate,
        [FromQuery] string? gender)
    {
        _logger.LogInformation("GET Patient - Search with name={Name}, identifier={Identifier}, birthdate={Birthdate}, gender={Gender}",
            name, identifier, birthdate, gender);

        // Start with base query (exclude soft-deleted)
        var query = _context.Patients.Where(p => !p.IsDeleted).AsQueryable();

        // Apply search filters
        if (!string.IsNullOrEmpty(name))
        {
            // Search in both FamilyName and GivenName (case-insensitive partial match)
            var nameLower = name.ToLower();
            query = query.Where(p =>
                (p.FamilyName != null && p.FamilyName.ToLower().Contains(nameLower)) ||
                (p.GivenName != null && p.GivenName.ToLower().Contains(nameLower)));
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            // Exact match on identifier
            query = query.Where(p => p.Identifier == identifier);
        }

        if (!string.IsNullOrEmpty(birthdate))
        {
            // Validate and parse birthdate
            if (!DateTime.TryParse(birthdate, out var parsedDate))
            {
                return BadRequest(CreateOperationOutcome(
                    $"Invalid birthdate format: '{birthdate}'. Expected format: YYYY-MM-DD",
                    OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Invalid));
            }

            // Exact date match (comparing only date part)
            query = query.Where(p => p.BirthDate.HasValue &&
                p.BirthDate.Value.Date == parsedDate.Date);
        }

        if (!string.IsNullOrEmpty(gender))
        {
            // Case-insensitive exact match on gender
            var genderLower = gender.ToLower();

            // Validate gender value (FHIR allows: male, female, other, unknown)
            if (genderLower != "male" && genderLower != "female" &&
                genderLower != "other" && genderLower != "unknown")
            {
                return BadRequest(CreateOperationOutcome(
                    $"Invalid gender value: '{gender}'. Valid values: male, female, other, unknown",
                    OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Invalid));
            }

            query = query.Where(p => p.Gender == genderLower);
        }

        // Execute query
        var patientEntities = await query.ToListAsync();

        _logger.LogInformation("Found {Count} patients matching search criteria", patientEntities.Count);

        // Build FHIR Bundle with search results
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Searchset,
            Total = patientEntities.Count,
            Entry = new List<Bundle.EntryComponent>()
        };

        foreach (var entity in patientEntities)
        {
            // Deserialize Patient from stored JSON
            var patient = _parser.Parse<Patient>(entity.FhirJson);

            // Update metadata
            patient.Meta = new Meta
            {
                VersionId = entity.VersionId.ToString(),
                LastUpdated = entity.LastUpdated
            };

            // Add to bundle
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{Request.Scheme}://{Request.Host}/Patient/{patient.Id}",
                Resource = patient,
                Search = new Bundle.SearchComponent
                {
                    Mode = Bundle.SearchEntryMode.Match
                }
            });
        }

        return Ok(bundle);
    }

    /// <summary>
    /// Create a new Patient resource
    /// FHIR operation: POST [base]/Patient
    /// </summary>
    /// <returns>Created patient with Location header</returns>
    [HttpPost]
    [Consumes("application/json", "application/fhir+json")]
    [ProducesResponseType(typeof(Patient), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePatient()
    {
        _logger.LogInformation("POST Patient - Creating new patient");

        // Validate Content-Type (FHIR spec requirement)
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

        // Enable buffering for potential multiple reads (logging, diagnostics)
        Request.EnableBuffering();

        // Read raw JSON from request body
        string patientJson;
        using (var reader = new StreamReader(
            Request.Body,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true))
        {
            patientJson = await reader.ReadToEndAsync();
            Request.Body.Position = 0;  // Reset for potential middleware usage
        }

        if (string.IsNullOrWhiteSpace(patientJson))
        {
            return BadRequest(CreateOperationOutcome(
                "Request body is empty",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Required));
        }

        // Parse JSON to FHIR Patient using Firely SDK
        Patient patient;
        try
        {
            patient = _parser.Parse<Patient>(patientJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse Patient JSON: {Error}", ex.Message);
            return BadRequest(CreateOperationOutcome(
                $"Invalid FHIR Patient JSON: {ex.Message}",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Structure));
        }

        // Validate FHIR resource
        var validationResult = ValidatePatient(patient);
        if (!validationResult.Success)
        {
            _logger.LogWarning("Patient validation failed: {Error}", validationResult.Error);
            return BadRequest(CreateOperationOutcome(
                validationResult.Error,
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Invalid));
        }

        // Generate new ID if not provided
        if (string.IsNullOrEmpty(patient.Id))
        {
            patient.Id = Guid.NewGuid().ToString();
        }

        // Check if ID already exists
        var exists = await _context.Patients.AnyAsync(p => p.Id == patient.Id);
        if (exists)
        {
            return BadRequest(CreateOperationOutcome(
                $"Patient with ID '{patient.Id}' already exists",
                OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Duplicate));
        }

        // Set metadata
        var now = DateTime.UtcNow;
        patient.Meta = new Meta
        {
            VersionId = "1",
            LastUpdated = now
        };

        // Serialize FHIR Patient to JSON using Firely SDK
        var fhirJson = _serializer.SerializeToString(patient);

        // Create entity with extracted searchable fields
        // These fields enable fast FHIR search queries
        var patientEntity = new PatientEntity
        {
            Id = patient.Id,
            FhirJson = fhirJson,
            FamilyName = patient.Name?.FirstOrDefault()?.Family,
            GivenName = patient.Name?.FirstOrDefault()?.Given?.FirstOrDefault(),
            Identifier = patient.Identifier?.FirstOrDefault()?.Value,
            BirthDate = patient.BirthDate != null ? DateTime.Parse(patient.BirthDate) : null,
            Gender = patient.Gender?.ToString()?.ToLower(),
            CreatedAt = now,
            LastUpdated = now,
            VersionId = 1
        };

        // Save to database
        _context.Patients.Add(patientEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created Patient {Id}", patient.Id);

        // Return 201 Created with Location header
        return CreatedAtAction(
            nameof(GetPatient),
            new { id = patient.Id },
            patient);
    }

    #region Helper Methods

    /// <summary>
    /// Validate Patient resource
    /// Basic validation - ensures FHIR resource has minimum required data
    /// </summary>
    private (bool Success, string Error) ValidatePatient(Patient patient)
    {
        if (patient == null)
        {
            return (false, "Patient resource is required");
        }

        // FHIR requires at least one name or identifier
        if ((patient.Name == null || !patient.Name.Any()) &&
            (patient.Identifier == null || !patient.Identifier.Any()))
        {
            return (false, "Patient must have at least one name or identifier");
        }

        // Validate birthdate format if provided
        if (!string.IsNullOrEmpty(patient.BirthDate))
        {
            if (!DateTime.TryParse(patient.BirthDate, out _))
            {
                return (false, "Invalid birthDate format");
            }
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Create FHIR OperationOutcome for errors
    /// This follows FHIR specification for standardized error reporting
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
