using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using LabFlow.API.Controllers;
using LabFlow.API.Data;
using LabFlow.API.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace LabFlow.Tests;

public class ObservationControllerTests : IDisposable
{
    private readonly FhirDbContext _context;
    private readonly ObservationController _controller;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;

    public ObservationControllerTests()
    {
        // Setup in-memory database with unique name per test
        var options = new DbContextOptionsBuilder<FhirDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FhirDbContext(options);

        // Setup Firely SDK
        _serializer = new FhirJsonSerializer();
        _parser = new FhirJsonParser();

        // Setup logger
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<ObservationController>();

        // Create controller
        _controller = new ObservationController(_context, _serializer, _parser, logger);

        // Setup HttpContext for URL generation
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.HttpContext.Request.Scheme = "http";
        _controller.HttpContext.Request.Host = new HostString("localhost:5000");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Helper Methods

    /// <summary>
    /// Create a test patient to reference in observations
    /// </summary>
    private async Task<string> SeedTestPatient(string familyName = "TestPatient")
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid().ToString(),
            Name = new List<HumanName>
            {
                new HumanName { Family = familyName, Given = new[] { "Test" } }
            }
        };

        var now = DateTime.UtcNow;
        var fhirJson = _serializer.SerializeToString(patient);

        var entity = new PatientEntity
        {
            Id = patient.Id,
            FhirJson = fhirJson,
            FamilyName = familyName,
            GivenName = "Test",
            CreatedAt = now,
            LastUpdated = now,
            VersionId = 1
        };

        _context.Patients.Add(entity);
        await _context.SaveChangesAsync();

        return patient.Id;
    }

    /// <summary>
    /// Create a test observation with common laboratory values
    /// </summary>
    private Observation CreateTestObservation(
        string? patientId = null,
        string? loincCode = null,
        string? codeDisplay = null,
        decimal? valueQuantity = null,
        string? valueUnit = null,
        string? status = null,
        string? category = null,
        DateTime? effectiveDateTime = null)
    {
        var observation = new Observation
        {
            Status = status != null
                ? Enum.Parse<ObservationStatus>(status, ignoreCase: true)
                : ObservationStatus.Final,

            Code = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new Coding
                    {
                        System = "http://loinc.org",
                        Code = loincCode ?? "2339-0", // Default: Glucose
                        Display = codeDisplay ?? "Glucose [Mass/volume] in Blood"
                    }
                }
            },

            Subject = patientId != null
                ? new ResourceReference($"Patient/{patientId}")
                : null,

            Category = new List<CodeableConcept>
            {
                new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = "http://terminology.hl7.org/CodeSystem/observation-category",
                            Code = category ?? "laboratory"
                        }
                    }
                }
            },

            Effective = new FhirDateTime(effectiveDateTime ?? DateTime.UtcNow)
        };

        // Add value if provided
        if (valueQuantity.HasValue)
        {
            observation.Value = new Quantity
            {
                Value = valueQuantity.Value,
                Unit = valueUnit ?? "mg/dL",
                System = "http://unitsofmeasure.org"
            };
        }

        return observation;
    }

    /// <summary>
    /// Seed an observation to the database
    /// </summary>
    private async Task<string> SeedObservation(Observation observation)
    {
        observation.Id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var fhirJson = _serializer.SerializeToString(observation);

        var patientRef = observation.Subject?.Reference;
        var extractedPatientId = !string.IsNullOrEmpty(patientRef) && patientRef.StartsWith("Patient/")
            ? patientRef.Substring("Patient/".Length)
            : patientRef;

        var entity = new ObservationEntity
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
            CreatedAt = now,
            LastUpdated = now,
            VersionId = 1
        };

        _context.Observations.Add(entity);
        await _context.SaveChangesAsync();

        return observation.Id;
    }

    #endregion

    #region GetObservation Tests

    [Fact]
    public async Task GetObservation_ValidId_ReturnsObservation()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var observation = CreateTestObservation(
            patientId: patientId,
            loincCode: "2339-0",
            valueQuantity: 95,
            valueUnit: "mg/dL"
        );
        var observationId = await SeedObservation(observation);

        // Act
        var result = await _controller.GetObservation(observationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedObservation = okResult!.Value as Observation;

        returnedObservation.Should().NotBeNull();
        returnedObservation!.Id.Should().Be(observationId);
        returnedObservation.Status.Should().Be(ObservationStatus.Final);
        returnedObservation.Code.Coding.Should().HaveCount(1);
        returnedObservation.Code.Coding[0].Code.Should().Be("2339-0");
        returnedObservation.Subject.Reference.Should().Contain(patientId);
    }

    [Fact]
    public async Task GetObservation_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _controller.GetObservation(nonExistentId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var outcome = notFoundResult!.Value as OperationOutcome;

        outcome.Should().NotBeNull();
        outcome!.Issue.Should().HaveCount(1);
        outcome.Issue[0].Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
        outcome.Issue[0].Code.Should().Be(OperationOutcome.IssueType.NotFound);
    }

    #endregion

    #region CreateObservation Tests

    [Fact]
    public async Task CreateObservation_ValidObservation_ReturnsCreated()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var observation = CreateTestObservation(
            patientId: patientId,
            loincCode: "2085-9",
            codeDisplay: "HDL Cholesterol",
            valueQuantity: 55,
            valueUnit: "mg/dL"
        );
        var observationJson = _serializer.SerializeToString(observation);

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(observationJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = observationJson.Length;

        // Act
        var result = await _controller.CreateObservation();

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var returnedObservation = createdResult!.Value as Observation;

        returnedObservation.Should().NotBeNull();
        returnedObservation!.Id.Should().NotBeNullOrEmpty();
        returnedObservation.Code.Coding[0].Code.Should().Be("2085-9");
        returnedObservation.Meta.Should().NotBeNull();
        returnedObservation.Meta!.VersionId.Should().Be("1");

        // Verify it was saved to database
        var savedEntity = await _context.Observations.FindAsync(returnedObservation.Id);
        savedEntity.Should().NotBeNull();
        savedEntity!.Code.Should().Be("2085-9");
        savedEntity.PatientId.Should().Be(patientId);
    }

    [Fact]
    public async Task CreateObservation_InvalidContentType_ReturnsBadRequest()
    {
        // Arrange
        var observationJson = "{\"test\": \"data\"}";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(observationJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "text/plain";

        // Act
        var result = await _controller.CreateObservation();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome.Should().NotBeNull();
        outcome!.Issue[0].Diagnostics.Should().Contain("Content-Type");
    }

    [Fact]
    public async Task CreateObservation_MissingStatus_ReturnsBadRequest()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var observation = CreateTestObservation(patientId: patientId);
        observation.Status = null; // Invalid: missing required status

        var observationJson = _serializer.SerializeToString(observation);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(observationJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateObservation();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("status");
    }

    [Fact]
    public async Task CreateObservation_MissingCode_ReturnsBadRequest()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var observation = CreateTestObservation(patientId: patientId);
        observation.Code = null; // Invalid: missing required code

        var observationJson = _serializer.SerializeToString(observation);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(observationJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateObservation();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("code");
    }

    [Fact]
    public async Task CreateObservation_NonExistentPatient_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentPatientId = Guid.NewGuid().ToString();
        var observation = CreateTestObservation(patientId: nonExistentPatientId);
        var observationJson = _serializer.SerializeToString(observation);

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(observationJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateObservation();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("does not exist");
    }

    #endregion

    #region SearchObservations Tests

    [Fact]
    public async Task SearchObservations_ByPatient_ReturnsMatchingObservations()
    {
        // Arrange
        var patient1Id = await SeedTestPatient("Patient1");
        var patient2Id = await SeedTestPatient("Patient2");

        await SeedObservation(CreateTestObservation(patientId: patient1Id, loincCode: "2339-0"));
        await SeedObservation(CreateTestObservation(patientId: patient1Id, loincCode: "2085-9"));
        await SeedObservation(CreateTestObservation(patientId: patient2Id, loincCode: "2339-0"));

        // Act
        var result = await _controller.SearchObservations(patient: patient1Id, null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var bundle = okResult!.Value as Bundle;

        bundle.Should().NotBeNull();
        bundle!.Type.Should().Be(Bundle.BundleType.Searchset);
        bundle.Total.Should().Be(2);
        bundle.Entry.Should().HaveCount(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((Observation)e.Resource).Subject.Reference.Contains(patient1Id));
    }

    [Fact]
    public async Task SearchObservations_ByPatientWithPrefix_ReturnsMatchingObservations()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedObservation(CreateTestObservation(patientId: patientId));

        // Act - Test with "Patient/123" format
        var result = await _controller.SearchObservations(patient: $"Patient/{patientId}", null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(1);
        ((Observation)bundle.Entry[0].Resource).Subject.Reference.Should().Contain(patientId);
    }

    [Fact]
    public async Task SearchObservations_ByCode_ReturnsMatchingObservations()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedObservation(CreateTestObservation(patientId: patientId, loincCode: "2339-0")); // Glucose
        await SeedObservation(CreateTestObservation(patientId: patientId, loincCode: "2085-9")); // HDL
        await SeedObservation(CreateTestObservation(patientId: patientId, loincCode: "2339-0")); // Glucose

        // Act
        var result = await _controller.SearchObservations(null, code: "2339-0", null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((Observation)e.Resource).Code.Coding[0].Code == "2339-0");
    }

    [Fact]
    public async Task SearchObservations_ByCategory_ReturnsMatchingObservations()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedObservation(CreateTestObservation(patientId: patientId, category: "laboratory"));
        await SeedObservation(CreateTestObservation(patientId: patientId, category: "vital-signs"));
        await SeedObservation(CreateTestObservation(patientId: patientId, category: "laboratory"));

        // Act
        var result = await _controller.SearchObservations(null, null, category: "laboratory", null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
    }

    [Fact]
    public async Task SearchObservations_ByDate_ReturnsMatchingObservations()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var date1 = new DateTime(2025, 10, 13);
        var date2 = new DateTime(2025, 10, 14);

        await SeedObservation(CreateTestObservation(patientId: patientId, effectiveDateTime: date1));
        await SeedObservation(CreateTestObservation(patientId: patientId, effectiveDateTime: date2));
        await SeedObservation(CreateTestObservation(patientId: patientId, effectiveDateTime: date1));

        // Act
        var result = await _controller.SearchObservations(null, null, null, date: "2025-10-13", null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
    }

    [Fact]
    public async Task SearchObservations_ByStatus_ReturnsMatchingObservations()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedObservation(CreateTestObservation(patientId: patientId, status: "final"));
        await SeedObservation(CreateTestObservation(patientId: patientId, status: "preliminary"));
        await SeedObservation(CreateTestObservation(patientId: patientId, status: "final"));

        // Act
        var result = await _controller.SearchObservations(null, null, null, null, status: "final", null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
    }

    [Fact]
    public async Task SearchObservations_CombinedFilters_ReturnsMatchingObservations()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedObservation(CreateTestObservation(patientId: patientId, loincCode: "2339-0", status: "final"));
        await SeedObservation(CreateTestObservation(patientId: patientId, loincCode: "2085-9", status: "final"));
        await SeedObservation(CreateTestObservation(patientId: patientId, loincCode: "2339-0", status: "preliminary"));

        // Act
        var result = await _controller.SearchObservations(
            patient: patientId,
            code: "2339-0",
            null,
            null,
            status: "final",
            null,
            null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(1);
        var observation = (Observation)bundle.Entry[0].Resource;
        observation.Code.Coding[0].Code.Should().Be("2339-0");
        observation.Status.Should().Be(ObservationStatus.Final);
    }

    [Fact]
    public async Task SearchObservations_NoMatches_ReturnsEmptyBundle()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedObservation(CreateTestObservation(patientId: patientId, loincCode: "2339-0"));

        // Act
        var result = await _controller.SearchObservations(null, code: "9999-9", null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(0);
        bundle.Entry.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchObservations_InvalidDate_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchObservations(null, null, null, date: "invalid-date", null, null, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("Invalid date format");
    }

    #endregion

    #region UpdateObservation Tests

    [Fact]
    public async Task UpdateObservation_ValidObservation_ReturnsOk()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var observation = CreateTestObservation(
            patientId: patientId,
            loincCode: "2339-0",
            valueQuantity: 95,
            valueUnit: "mg/dL"
        );
        var observationId = await SeedObservation(observation);

        // Update observation data
        observation.Id = observationId;
        observation.Status = ObservationStatus.Amended;
        observation.Value = new Quantity
        {
            Value = 110,
            Unit = "mg/dL",
            System = "http://unitsofmeasure.org"
        };

        var observationJson = _serializer.SerializeToString(observation);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(observationJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = observationJson.Length;

        // Act
        var result = await _controller.UpdateObservation(observationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedObservation = okResult!.Value as Observation;

        returnedObservation.Should().NotBeNull();
        returnedObservation!.Id.Should().Be(observationId);
        returnedObservation.Status.Should().Be(ObservationStatus.Amended);
        ((Quantity)returnedObservation.Value).Value.Should().Be(110);
        returnedObservation.Meta!.VersionId.Should().Be("2");

        // Verify database was updated
        var updatedEntity = await _context.Observations.FindAsync(observationId);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.Status.Should().Be("amended");
        updatedEntity.ValueQuantity.Should().Be(110);
        updatedEntity.VersionId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateObservation_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var nonExistentId = Guid.NewGuid().ToString();
        var observation = CreateTestObservation(patientId: patientId);
        observation.Id = nonExistentId;

        var observationJson = _serializer.SerializeToString(observation);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(observationJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = observationJson.Length;

        // Act
        var result = await _controller.UpdateObservation(nonExistentId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var outcome = notFoundResult!.Value as OperationOutcome;

        outcome.Should().NotBeNull();
        outcome!.Issue.Should().HaveCount(1);
        outcome.Issue[0].Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
        outcome.Issue[0].Code.Should().Be(OperationOutcome.IssueType.NotFound);
    }

    #endregion

    #region DeleteObservation Tests

    [Fact]
    public async Task DeleteObservation_ValidId_ReturnsNoContent()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var observation = CreateTestObservation(patientId: patientId);
        var observationId = await SeedObservation(observation);

        // Act
        var result = await _controller.DeleteObservation(observationId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify soft delete
        var deletedEntity = await _context.Observations.FindAsync(observationId);
        deletedEntity.Should().NotBeNull();
        deletedEntity!.IsDeleted.Should().BeTrue();
        deletedEntity.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteObservation_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _controller.DeleteObservation(nonExistentId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var outcome = notFoundResult!.Value as OperationOutcome;

        outcome.Should().NotBeNull();
        outcome!.Issue.Should().HaveCount(1);
        outcome.Issue[0].Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
        outcome.Issue[0].Code.Should().Be(OperationOutcome.IssueType.NotFound);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task SearchObservations_WithDefaultPagination_Returns20Results()
    {
        // Arrange - Seed 30 observations
        var patientId = await SeedTestPatient();
        for (int i = 1; i <= 30; i++)
        {
            await SeedObservation(CreateTestObservation(
                patientId: patientId,
                loincCode: $"CODE-{i:D3}",
                valueQuantity: 90 + i
            ));
            await Task.Delay(10); // Ensure different LastUpdated timestamps
        }

        // Act - No pagination parameters
        var result = await _controller.SearchObservations(null, null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(30);
        bundle.Entry.Should().HaveCount(20); // Default page size

        // Verify links
        bundle.Link.Should().Contain(l => l.Relation == "self");
        bundle.Link.Should().Contain(l => l.Relation == "next");
        bundle.Link.Should().NotContain(l => l.Relation == "previous");
    }

    [Fact]
    public async Task SearchObservations_WithCustomCount_ReturnsCorrectPageSize()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        for (int i = 1; i <= 15; i++)
        {
            await SeedObservation(CreateTestObservation(patientId: patientId));
        }

        // Act - Request 5 results per page
        var result = await _controller.SearchObservations(null, null, null, null, null, _count: 5, _offset: null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(15);
        bundle.Entry.Should().HaveCount(5);
        bundle.Link.Should().Contain(l => l.Relation == "next");
    }

    [Fact]
    public async Task SearchObservations_WithOffset_ReturnsCorrectPage()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        for (int i = 1; i <= 10; i++)
        {
            await SeedObservation(CreateTestObservation(patientId: patientId));
            await Task.Delay(10);
        }

        // Act - Request second page
        var result = await _controller.SearchObservations(null, null, null, null, null, _count: 5, _offset: 5);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(10);
        bundle.Entry.Should().HaveCount(5);

        // Verify links
        bundle.Link.Should().Contain(l => l.Relation == "previous");
        bundle.Link.Should().NotContain(l => l.Relation == "next");
    }

    [Fact]
    public async Task SearchObservations_CountTooLarge_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchObservations(null, null, null, null, null, _count: 101, _offset: null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;
        outcome!.Issue[0].Diagnostics.Should().Contain("Parameter _count must be between 1 and 100");
    }

    [Fact]
    public async Task SearchObservations_NegativeOffset_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchObservations(null, null, null, null, null, _count: null, _offset: -1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;
        outcome!.Issue[0].Diagnostics.Should().Contain("Parameter _offset must be non-negative");
    }

    [Fact]
    public async Task SearchObservations_PaginationWithFilters_PreservesQueryParams()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        for (int i = 1; i <= 25; i++)
        {
            await SeedObservation(CreateTestObservation(patientId: patientId, loincCode: "2339-0"));
        }

        // Act - Search by patient and code with pagination
        var result = await _controller.SearchObservations(
            patient: patientId,
            code: "2339-0",
            category: null,
            date: null,
            status: null,
            _count: 10,
            _offset: 0);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(25);
        bundle.Entry.Should().HaveCount(10);

        // Verify pagination links preserve query parameters
        var nextLink = bundle.Link.FirstOrDefault(l => l.Relation == "next");
        nextLink.Should().NotBeNull();
        nextLink!.Url.Should().Contain($"patient={patientId}");
        nextLink.Url.Should().Contain("code=2339-0");
        nextLink.Url.Should().Contain("_count=10");
        nextLink.Url.Should().Contain("_offset=10");
    }

    #endregion
}
