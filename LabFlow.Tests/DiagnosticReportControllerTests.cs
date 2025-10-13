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

public class DiagnosticReportControllerTests : IDisposable
{
    private readonly FhirDbContext _context;
    private readonly DiagnosticReportController _controller;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;

    public DiagnosticReportControllerTests()
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
            .CreateLogger<DiagnosticReportController>();

        // Create controller
        _controller = new DiagnosticReportController(_context, _serializer, _parser, logger);

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
    /// Create a test patient to reference in reports
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
    /// Create a test observation
    /// </summary>
    private async Task<string> SeedTestObservation(string patientId, string loincCode = "2339-0")
    {
        var observation = new Observation
        {
            Id = Guid.NewGuid().ToString(),
            Status = ObservationStatus.Final,
            Code = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new Coding
                    {
                        System = "http://loinc.org",
                        Code = loincCode,
                        Display = "Test Observation"
                    }
                }
            },
            Subject = new ResourceReference($"Patient/{patientId}"),
            Effective = new FhirDateTime(DateTime.UtcNow)
        };

        var now = DateTime.UtcNow;
        var fhirJson = _serializer.SerializeToString(observation);

        var entity = new ObservationEntity
        {
            Id = observation.Id,
            FhirJson = fhirJson,
            PatientId = patientId,
            Code = loincCode,
            Status = "final",
            Category = "laboratory",
            EffectiveDateTime = DateTime.UtcNow,
            CreatedAt = now,
            LastUpdated = now,
            VersionId = 1
        };

        _context.Observations.Add(entity);
        await _context.SaveChangesAsync();

        return observation.Id;
    }

    /// <summary>
    /// Create a test diagnostic report
    /// </summary>
    private DiagnosticReport CreateTestDiagnosticReport(
        string? patientId = null,
        string? loincCode = null,
        string? codeDisplay = null,
        string? status = null,
        string? category = null,
        DateTime? effectiveDateTime = null,
        DateTimeOffset? issued = null,
        List<string>? observationIds = null,
        string? conclusion = null)
    {
        var report = new DiagnosticReport
        {
            Status = status != null
                ? Enum.Parse<DiagnosticReport.DiagnosticReportStatus>(status, ignoreCase: true)
                : DiagnosticReport.DiagnosticReportStatus.Final,

            Code = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new Coding
                    {
                        System = "http://loinc.org",
                        Code = loincCode ?? "58410-2", // Default: CBC panel
                        Display = codeDisplay ?? "Complete blood count (CBC) panel"
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
                            System = "http://terminology.hl7.org/CodeSystem/v2-0074",
                            Code = category ?? "LAB"
                        }
                    }
                }
            },

            Effective = new FhirDateTime(effectiveDateTime ?? DateTime.UtcNow),
            IssuedElement = issued.HasValue ? new Instant(issued.Value) : new Instant(DateTimeOffset.UtcNow),
            Conclusion = conclusion
        };

        // Add result references if provided
        if (observationIds != null && observationIds.Any())
        {
            report.Result = observationIds.Select(id => new ResourceReference($"Observation/{id}")).ToList();
        }

        return report;
    }

    /// <summary>
    /// Seed a diagnostic report to the database
    /// </summary>
    private async Task<string> SeedDiagnosticReport(DiagnosticReport report)
    {
        report.Id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var fhirJson = _serializer.SerializeToString(report);

        var patientRef = report.Subject?.Reference;
        var extractedPatientId = !string.IsNullOrEmpty(patientRef) && patientRef.StartsWith("Patient/")
            ? patientRef.Substring("Patient/".Length)
            : patientRef;

        var resultIds = report.Result?.Select(r => r.Reference?.Replace("Observation/", ""))
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        var entity = new DiagnosticReportEntity
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
                : null,
            Issued = report.IssuedElement?.Value?.UtcDateTime,
            ResultIds = resultIds != null && resultIds.Any() ? string.Join(",", resultIds) : null,
            Conclusion = report.Conclusion,
            CreatedAt = now,
            LastUpdated = now,
            VersionId = 1
        };

        _context.DiagnosticReports.Add(entity);
        await _context.SaveChangesAsync();

        return report.Id;
    }

    #endregion

    #region GetDiagnosticReport Tests

    [Fact]
    public async Task GetDiagnosticReport_ValidId_ReturnsDiagnosticReport()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var observationId = await SeedTestObservation(patientId);
        var report = CreateTestDiagnosticReport(
            patientId: patientId,
            loincCode: "58410-2",
            observationIds: new List<string> { observationId },
            conclusion: "All values within normal range"
        );
        var reportId = await SeedDiagnosticReport(report);

        // Act
        var result = await _controller.GetDiagnosticReport(reportId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedReport = okResult!.Value as DiagnosticReport;

        returnedReport.Should().NotBeNull();
        returnedReport!.Id.Should().Be(reportId);
        returnedReport.Status.Should().Be(DiagnosticReport.DiagnosticReportStatus.Final);
        returnedReport.Code.Coding.Should().HaveCount(1);
        returnedReport.Code.Coding[0].Code.Should().Be("58410-2");
        returnedReport.Subject.Reference.Should().Contain(patientId);
        returnedReport.Result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDiagnosticReport_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _controller.GetDiagnosticReport(nonExistentId);

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

    #region CreateDiagnosticReport Tests

    [Fact]
    public async Task CreateDiagnosticReport_ValidReport_ReturnsCreated()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var obs1Id = await SeedTestObservation(patientId, "718-7"); // Hemoglobin
        var obs2Id = await SeedTestObservation(patientId, "6690-2"); // WBC

        var report = CreateTestDiagnosticReport(
            patientId: patientId,
            loincCode: "58410-2",
            codeDisplay: "Complete blood count (CBC) panel",
            observationIds: new List<string> { obs1Id, obs2Id },
            conclusion: "Normal CBC results"
        );
        var reportJson = _serializer.SerializeToString(report);

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = reportJson.Length;

        // Act
        var result = await _controller.CreateDiagnosticReport();

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var returnedReport = createdResult!.Value as DiagnosticReport;

        returnedReport.Should().NotBeNull();
        returnedReport!.Id.Should().NotBeNullOrEmpty();
        returnedReport.Code.Coding[0].Code.Should().Be("58410-2");
        returnedReport.Result.Should().HaveCount(2);
        returnedReport.Conclusion.Should().Be("Normal CBC results");
        returnedReport.Meta.Should().NotBeNull();
        returnedReport.Meta!.VersionId.Should().Be("1");

        // Verify it was saved to database
        var savedEntity = await _context.DiagnosticReports.FindAsync(returnedReport.Id);
        savedEntity.Should().NotBeNull();
        savedEntity!.Code.Should().Be("58410-2");
        savedEntity.PatientId.Should().Be(patientId);
        savedEntity.ResultIds.Should().Contain(obs1Id);
        savedEntity.ResultIds.Should().Contain(obs2Id);
    }

    [Fact]
    public async Task CreateDiagnosticReport_InvalidContentType_ReturnsBadRequest()
    {
        // Arrange
        var reportJson = "{\"test\": \"data\"}";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "text/plain";

        // Act
        var result = await _controller.CreateDiagnosticReport();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome.Should().NotBeNull();
        outcome!.Issue[0].Diagnostics.Should().Contain("Content-Type");
    }

    [Fact]
    public async Task CreateDiagnosticReport_MissingStatus_ReturnsBadRequest()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var report = CreateTestDiagnosticReport(patientId: patientId);
        report.Status = null; // Invalid: missing required status

        var reportJson = _serializer.SerializeToString(report);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateDiagnosticReport();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("status");
    }

    [Fact]
    public async Task CreateDiagnosticReport_MissingCode_ReturnsBadRequest()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var report = CreateTestDiagnosticReport(patientId: patientId);
        report.Code = null; // Invalid: missing required code

        var reportJson = _serializer.SerializeToString(report);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateDiagnosticReport();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("code");
    }

    [Fact]
    public async Task CreateDiagnosticReport_NonExistentPatient_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentPatientId = Guid.NewGuid().ToString();
        var report = CreateTestDiagnosticReport(patientId: nonExistentPatientId);
        var reportJson = _serializer.SerializeToString(report);

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateDiagnosticReport();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("does not exist");
        outcome.Issue[0].Diagnostics.Should().Contain("patient");
    }

    [Fact]
    public async Task CreateDiagnosticReport_NonExistentObservation_ReturnsBadRequest()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var nonExistentObsId = Guid.NewGuid().ToString();
        var report = CreateTestDiagnosticReport(
            patientId: patientId,
            observationIds: new List<string> { nonExistentObsId }
        );
        var reportJson = _serializer.SerializeToString(report);

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateDiagnosticReport();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("does not exist");
        outcome.Issue[0].Diagnostics.Should().Contain("observation");
    }

    [Fact]
    public async Task CreateDiagnosticReport_InvalidReferenceType_ReturnsBadRequest()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var report = CreateTestDiagnosticReport(patientId: patientId);
        // Add invalid reference (not an Observation)
        report.Result = new List<ResourceReference>
        {
            new ResourceReference("Patient/123")
        };
        var reportJson = _serializer.SerializeToString(report);

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateDiagnosticReport();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("must be an Observation");
    }

    #endregion

    #region SearchDiagnosticReports Tests

    [Fact]
    public async Task SearchDiagnosticReports_ByPatient_ReturnsMatchingReports()
    {
        // Arrange
        var patient1Id = await SeedTestPatient("Patient1");
        var patient2Id = await SeedTestPatient("Patient2");

        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patient1Id, loincCode: "58410-2"));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patient1Id, loincCode: "24331-1"));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patient2Id, loincCode: "58410-2"));

        // Act
        var result = await _controller.SearchDiagnosticReports(patient: patient1Id, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var bundle = okResult!.Value as Bundle;

        bundle.Should().NotBeNull();
        bundle!.Type.Should().Be(Bundle.BundleType.Searchset);
        bundle.Total.Should().Be(2);
        bundle.Entry.Should().HaveCount(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((DiagnosticReport)e.Resource).Subject.Reference.Contains(patient1Id));
    }

    [Fact]
    public async Task SearchDiagnosticReports_ByCode_ReturnsMatchingReports()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, loincCode: "58410-2")); // CBC
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, loincCode: "24331-1")); // Lipid panel
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, loincCode: "58410-2")); // CBC

        // Act
        var result = await _controller.SearchDiagnosticReports(null, code: "58410-2", null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((DiagnosticReport)e.Resource).Code.Coding[0].Code == "58410-2");
    }

    [Fact]
    public async Task SearchDiagnosticReports_ByCategory_ReturnsMatchingReports()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, category: "LAB"));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, category: "RAD"));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, category: "LAB"));

        // Act
        var result = await _controller.SearchDiagnosticReports(null, null, category: "LAB", null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
    }

    [Fact]
    public async Task SearchDiagnosticReports_ByDate_ReturnsMatchingReports()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var date1 = new DateTime(2025, 10, 13);
        var date2 = new DateTime(2025, 10, 14);

        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, effectiveDateTime: date1));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, effectiveDateTime: date2));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, effectiveDateTime: date1));

        // Act
        var result = await _controller.SearchDiagnosticReports(null, null, null, date: "2025-10-13", null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
    }

    [Fact]
    public async Task SearchDiagnosticReports_ByIssued_ReturnsMatchingReports()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var issued1 = new DateTimeOffset(new DateTime(2025, 10, 13, 0, 0, 0, DateTimeKind.Utc));
        var issued2 = new DateTimeOffset(new DateTime(2025, 10, 14, 0, 0, 0, DateTimeKind.Utc));

        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, issued: issued1));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, issued: issued2));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, issued: issued1));

        // Act
        var result = await _controller.SearchDiagnosticReports(null, null, null, null, issued: "2025-10-13", null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
    }

    [Fact]
    public async Task SearchDiagnosticReports_ByStatus_ReturnsMatchingReports()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, status: "final"));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, status: "preliminary"));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, status: "final"));

        // Act
        var result = await _controller.SearchDiagnosticReports(null, null, null, null, null, status: "final");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
    }

    [Fact]
    public async Task SearchDiagnosticReports_CombinedFilters_ReturnsMatchingReports()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, loincCode: "58410-2", status: "final"));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, loincCode: "24331-1", status: "final"));
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, loincCode: "58410-2", status: "preliminary"));

        // Act
        var result = await _controller.SearchDiagnosticReports(
            patient: patientId,
            code: "58410-2",
            null,
            null,
            null,
            status: "final");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(1);
        var report = (DiagnosticReport)bundle.Entry[0].Resource;
        report.Code.Coding[0].Code.Should().Be("58410-2");
        report.Status.Should().Be(DiagnosticReport.DiagnosticReportStatus.Final);
    }

    [Fact]
    public async Task SearchDiagnosticReports_NoMatches_ReturnsEmptyBundle()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedDiagnosticReport(CreateTestDiagnosticReport(patientId: patientId, loincCode: "58410-2"));

        // Act
        var result = await _controller.SearchDiagnosticReports(null, code: "9999-9", null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(0);
        bundle.Entry.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchDiagnosticReports_InvalidDate_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchDiagnosticReports(null, null, null, date: "invalid-date", null, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("Invalid date format");
    }

    [Fact]
    public async Task SearchDiagnosticReports_InvalidIssued_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchDiagnosticReports(null, null, null, null, issued: "invalid-date", null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("Invalid issued date format");
    }

    #endregion
}
