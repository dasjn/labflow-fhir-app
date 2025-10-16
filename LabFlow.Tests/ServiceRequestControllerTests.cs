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

public class ServiceRequestControllerTests : IDisposable
{
    private readonly FhirDbContext _context;
    private readonly ServiceRequestController _controller;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;

    public ServiceRequestControllerTests()
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
            .CreateLogger<ServiceRequestController>();

        // Create controller
        _controller = new ServiceRequestController(_context, _serializer, _parser, logger);

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
    /// Create a test patient to reference in service requests
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
    /// Create a test service request with common laboratory order values
    /// </summary>
    private ServiceRequest CreateTestServiceRequest(
        string? patientId = null,
        string? loincCode = null,
        string? codeDisplay = null,
        string? status = null,
        string? intent = null,
        string? category = null,
        string? priority = null,
        DateTime? authoredOn = null)
    {
        var serviceRequest = new ServiceRequest
        {
            Status = status != null
                ? Enum.Parse<RequestStatus>(status, ignoreCase: true)
                : RequestStatus.Active,

            Intent = intent != null
                ? Enum.Parse<RequestIntent>(intent, ignoreCase: true)
                : RequestIntent.Order,

            Code = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new Coding
                    {
                        System = "http://loinc.org",
                        Code = loincCode ?? "2339-0", // Default: Glucose test
                        Display = codeDisplay ?? "Glucose [Mass/volume] in Blood"
                    }
                }
            },

            Subject = patientId != null
                ? new ResourceReference($"Patient/{patientId}")
                : null,

            Category = category != null
                ? new List<CodeableConcept>
                {
                    new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new Coding
                            {
                                System = "http://snomed.info/sct",
                                Code = category
                            }
                        }
                    }
                }
                : null,

            Priority = priority != null
                ? Enum.Parse<RequestPriority>(priority, ignoreCase: true)
                : null,

            AuthoredOn = (authoredOn ?? DateTime.UtcNow).ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return serviceRequest;
    }

    /// <summary>
    /// Seed a service request to the database
    /// </summary>
    private async Task<string> SeedServiceRequest(ServiceRequest serviceRequest)
    {
        serviceRequest.Id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var fhirJson = _serializer.SerializeToString(serviceRequest);

        var patientRef = serviceRequest.Subject?.Reference;
        var extractedPatientId = !string.IsNullOrEmpty(patientRef) && patientRef.StartsWith("Patient/")
            ? patientRef.Substring("Patient/".Length)
            : patientRef;

        var entity = new ServiceRequestEntity
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
            CreatedAt = now,
            LastUpdated = now,
            VersionId = 1
        };

        _context.ServiceRequests.Add(entity);
        await _context.SaveChangesAsync();

        return serviceRequest.Id;
    }

    #endregion

    #region GetServiceRequest Tests

    [Fact]
    public async Task GetServiceRequest_ValidId_ReturnsServiceRequest()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var serviceRequest = CreateTestServiceRequest(
            patientId: patientId,
            loincCode: "2339-0",
            status: "active",
            intent: "order"
        );
        var serviceRequestId = await SeedServiceRequest(serviceRequest);

        // Act
        var result = await _controller.GetServiceRequest(serviceRequestId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedServiceRequest = okResult!.Value as ServiceRequest;

        returnedServiceRequest.Should().NotBeNull();
        returnedServiceRequest!.Id.Should().Be(serviceRequestId);
        returnedServiceRequest.Status.Should().Be(RequestStatus.Active);
        returnedServiceRequest.Intent.Should().Be(RequestIntent.Order);
        returnedServiceRequest.Code.Coding.Should().HaveCount(1);
        returnedServiceRequest.Code.Coding[0].Code.Should().Be("2339-0");
        returnedServiceRequest.Subject.Reference.Should().Contain(patientId);
    }

    [Fact]
    public async Task GetServiceRequest_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _controller.GetServiceRequest(nonExistentId);

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

    #region CreateServiceRequest Tests

    [Fact]
    public async Task CreateServiceRequest_ValidServiceRequest_ReturnsCreated()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var serviceRequest = CreateTestServiceRequest(
            patientId: patientId,
            loincCode: "58410-2",
            codeDisplay: "Complete blood count (CBC) panel",
            status: "active",
            intent: "order",
            priority: "routine"
        );
        var serviceRequestJson = _serializer.SerializeToString(serviceRequest);

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serviceRequestJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = serviceRequestJson.Length;

        // Act
        var result = await _controller.CreateServiceRequest();

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var returnedServiceRequest = createdResult!.Value as ServiceRequest;

        returnedServiceRequest.Should().NotBeNull();
        returnedServiceRequest!.Id.Should().NotBeNullOrEmpty();
        returnedServiceRequest.Code.Coding[0].Code.Should().Be("58410-2");
        returnedServiceRequest.Meta.Should().NotBeNull();
        returnedServiceRequest.Meta!.VersionId.Should().Be("1");

        // Verify it was saved to database
        var savedEntity = await _context.ServiceRequests.FindAsync(returnedServiceRequest.Id);
        savedEntity.Should().NotBeNull();
        savedEntity!.Code.Should().Be("58410-2");
        savedEntity.PatientId.Should().Be(patientId);
    }

    [Fact]
    public async Task CreateServiceRequest_InvalidContentType_ReturnsBadRequest()
    {
        // Arrange
        var serviceRequestJson = "{\"test\": \"data\"}";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serviceRequestJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "text/plain";

        // Act
        var result = await _controller.CreateServiceRequest();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome.Should().NotBeNull();
        outcome!.Issue[0].Diagnostics.Should().Contain("Content-Type");
    }

    [Fact]
    public async Task CreateServiceRequest_MissingStatus_ReturnsBadRequest()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var serviceRequest = CreateTestServiceRequest(patientId: patientId);
        serviceRequest.Status = null; // Invalid: missing required status

        var serviceRequestJson = _serializer.SerializeToString(serviceRequest);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serviceRequestJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateServiceRequest();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("status");
    }

    [Fact]
    public async Task CreateServiceRequest_MissingIntent_ReturnsBadRequest()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var serviceRequest = CreateTestServiceRequest(patientId: patientId);
        serviceRequest.Intent = null; // Invalid: missing required intent

        var serviceRequestJson = _serializer.SerializeToString(serviceRequest);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serviceRequestJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateServiceRequest();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("intent");
    }

    [Fact]
    public async Task CreateServiceRequest_NonExistentPatient_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentPatientId = Guid.NewGuid().ToString();
        var serviceRequest = CreateTestServiceRequest(patientId: nonExistentPatientId);
        var serviceRequestJson = _serializer.SerializeToString(serviceRequest);

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serviceRequestJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreateServiceRequest();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("does not exist");
    }

    #endregion

    #region SearchServiceRequests Tests

    [Fact]
    public async Task SearchServiceRequests_ByPatient_ReturnsMatchingServiceRequests()
    {
        // Arrange
        var patient1Id = await SeedTestPatient("Patient1");
        var patient2Id = await SeedTestPatient("Patient2");

        await SeedServiceRequest(CreateTestServiceRequest(patientId: patient1Id, loincCode: "2339-0"));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patient1Id, loincCode: "58410-2"));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patient2Id, loincCode: "2339-0"));

        // Act
        var result = await _controller.SearchServiceRequests(
            patient: patient1Id, null, null, null, null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var bundle = okResult!.Value as Bundle;

        bundle.Should().NotBeNull();
        bundle!.Type.Should().Be(Bundle.BundleType.Searchset);
        bundle.Total.Should().Be(2);
        bundle.Entry.Should().HaveCount(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((ServiceRequest)e.Resource).Subject.Reference.Contains(patient1Id));
    }

    [Fact]
    public async Task SearchServiceRequests_ByPatientWithPrefix_ReturnsMatchingServiceRequests()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId));

        // Act - Test with "Patient/123" format
        var result = await _controller.SearchServiceRequests(
            patient: $"Patient/{patientId}", null, null, null, null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(1);
        ((ServiceRequest)bundle.Entry[0].Resource).Subject.Reference.Should().Contain(patientId);
    }

    [Fact]
    public async Task SearchServiceRequests_ByCode_ReturnsMatchingServiceRequests()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, loincCode: "2339-0")); // Glucose test
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, loincCode: "58410-2")); // CBC panel
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, loincCode: "2339-0")); // Glucose test

        // Act
        var result = await _controller.SearchServiceRequests(
            null, code: "2339-0", null, null, null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((ServiceRequest)e.Resource).Code.Coding[0].Code == "2339-0");
    }

    [Fact]
    public async Task SearchServiceRequests_ByStatus_ReturnsMatchingServiceRequests()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, status: "active"));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, status: "completed"));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, status: "active"));

        // Act
        var result = await _controller.SearchServiceRequests(
            null, null, status: "active", null, null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((ServiceRequest)e.Resource).Status == RequestStatus.Active);
    }

    [Fact]
    public async Task SearchServiceRequests_ByIntent_ReturnsMatchingServiceRequests()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, intent: "order"));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, intent: "plan"));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, intent: "order"));

        // Act
        var result = await _controller.SearchServiceRequests(
            null, null, null, intent: "order", null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((ServiceRequest)e.Resource).Intent == RequestIntent.Order);
    }

    [Fact]
    public async Task SearchServiceRequests_ByAuthored_ReturnsMatchingServiceRequests()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var date1 = new DateTime(2025, 10, 13, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2025, 10, 14, 0, 0, 0, DateTimeKind.Utc);

        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, authoredOn: date1));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, authoredOn: date2));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, authoredOn: date1));

        // Act
        var result = await _controller.SearchServiceRequests(
            null, null, null, null, null, authored: "2025-10-13", null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
    }

    [Fact]
    public async Task SearchServiceRequests_CombinedFilters_ReturnsMatchingServiceRequests()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, loincCode: "2339-0", status: "active", intent: "order"));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, loincCode: "58410-2", status: "active", intent: "order"));
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, loincCode: "2339-0", status: "completed", intent: "order"));

        // Act
        var result = await _controller.SearchServiceRequests(
            patient: patientId,
            code: "2339-0",
            status: "active",
            intent: "order",
            null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(1);
        var serviceRequest = (ServiceRequest)bundle.Entry[0].Resource;
        serviceRequest.Code.Coding[0].Code.Should().Be("2339-0");
        serviceRequest.Status.Should().Be(RequestStatus.Active);
        serviceRequest.Intent.Should().Be(RequestIntent.Order);
    }

    [Fact]
    public async Task SearchServiceRequests_NoMatches_ReturnsEmptyBundle()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId, loincCode: "2339-0"));

        // Act
        var result = await _controller.SearchServiceRequests(
            null, code: "9999-9", null, null, null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(0);
        bundle.Entry.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchServiceRequests_InvalidAuthoredDate_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchServiceRequests(
            null, null, null, null, null, authored: "invalid-date", null, null, null, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("Invalid date format");
    }

    #endregion

    #region UpdateServiceRequest Tests

    [Fact]
    public async Task UpdateServiceRequest_ValidServiceRequest_ReturnsOk()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var serviceRequest = CreateTestServiceRequest(
            patientId: patientId,
            loincCode: "2339-0",
            status: "active",
            intent: "order",
            priority: "routine"
        );
        var serviceRequestId = await SeedServiceRequest(serviceRequest);

        // Update service request data
        serviceRequest.Id = serviceRequestId;
        serviceRequest.Status = RequestStatus.Completed;
        serviceRequest.Priority = RequestPriority.Urgent;

        var serviceRequestJson = _serializer.SerializeToString(serviceRequest);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serviceRequestJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = serviceRequestJson.Length;

        // Act
        var result = await _controller.UpdateServiceRequest(serviceRequestId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedServiceRequest = okResult!.Value as ServiceRequest;

        returnedServiceRequest.Should().NotBeNull();
        returnedServiceRequest!.Id.Should().Be(serviceRequestId);
        returnedServiceRequest.Status.Should().Be(RequestStatus.Completed);
        returnedServiceRequest.Priority.Should().Be(RequestPriority.Urgent);
        returnedServiceRequest.Meta!.VersionId.Should().Be("2");

        // Verify database was updated
        var updatedEntity = await _context.ServiceRequests.FindAsync(serviceRequestId);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.Status.Should().Be("completed");
        updatedEntity.Priority.Should().Be("urgent");
        updatedEntity.VersionId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateServiceRequest_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var nonExistentId = Guid.NewGuid().ToString();
        var serviceRequest = CreateTestServiceRequest(patientId: patientId);
        serviceRequest.Id = nonExistentId;

        var serviceRequestJson = _serializer.SerializeToString(serviceRequest);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serviceRequestJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = serviceRequestJson.Length;

        // Act
        var result = await _controller.UpdateServiceRequest(nonExistentId);

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

    #region DeleteServiceRequest Tests

    [Fact]
    public async Task DeleteServiceRequest_ValidId_ReturnsNoContent()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        var serviceRequest = CreateTestServiceRequest(patientId: patientId);
        var serviceRequestId = await SeedServiceRequest(serviceRequest);

        // Act
        var result = await _controller.DeleteServiceRequest(serviceRequestId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify soft delete
        var deletedEntity = await _context.ServiceRequests.FindAsync(serviceRequestId);
        deletedEntity.Should().NotBeNull();
        deletedEntity!.IsDeleted.Should().BeTrue();
        deletedEntity.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteServiceRequest_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _controller.DeleteServiceRequest(nonExistentId);

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
    public async Task SearchServiceRequests_WithDefaultPagination_Returns20Results()
    {
        // Arrange - Seed 30 service requests
        var patientId = await SeedTestPatient();
        for (int i = 1; i <= 30; i++)
        {
            await SeedServiceRequest(CreateTestServiceRequest(
                patientId: patientId,
                loincCode: $"TEST-{i:D3}"
            ));
            await Task.Delay(10);
        }

        // Act - No pagination parameters
        var result = await _controller.SearchServiceRequests(null, null, null, null, null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(30);
        bundle.Entry.Should().HaveCount(20); // Default page size
        bundle.Link.Should().Contain(l => l.Relation == "self");
        bundle.Link.Should().Contain(l => l.Relation == "next");
        bundle.Link.Should().NotContain(l => l.Relation == "previous");
    }

    [Fact]
    public async Task SearchServiceRequests_WithCustomCount_ReturnsCorrectPageSize()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        for (int i = 1; i <= 15; i++)
        {
            await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId));
        }

        // Act - Request 5 results per page
        var result = await _controller.SearchServiceRequests(null, null, null, null, null, null, null, null, _count: 5, _offset: null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(15);
        bundle.Entry.Should().HaveCount(5);
        bundle.Link.Should().Contain(l => l.Relation == "next");
    }

    [Fact]
    public async Task SearchServiceRequests_WithOffset_ReturnsCorrectPage()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        for (int i = 1; i <= 10; i++)
        {
            await SeedServiceRequest(CreateTestServiceRequest(patientId: patientId));
            await Task.Delay(10);
        }

        // Act - Request second page
        var result = await _controller.SearchServiceRequests(null, null, null, null, null, null, null, null, _count: 5, _offset: 5);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(10);
        bundle.Entry.Should().HaveCount(5);
        bundle.Link.Should().Contain(l => l.Relation == "previous");
        bundle.Link.Should().NotContain(l => l.Relation == "next");
    }

    [Fact]
    public async Task SearchServiceRequests_CountTooLarge_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchServiceRequests(null, null, null, null, null, null, null, null, _count: 101, _offset: null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;
        outcome!.Issue[0].Diagnostics.Should().Contain("_count parameter must be between 1 and 100");
    }

    [Fact]
    public async Task SearchServiceRequests_NegativeOffset_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchServiceRequests(null, null, null, null, null, null, null, null, _count: null, _offset: -1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;
        outcome!.Issue[0].Diagnostics.Should().Contain("_offset parameter must be non-negative");
    }

    [Fact]
    public async Task SearchServiceRequests_PaginationWithFilters_PreservesQueryParams()
    {
        // Arrange
        var patientId = await SeedTestPatient();
        for (int i = 1; i <= 25; i++)
        {
            await SeedServiceRequest(CreateTestServiceRequest(
                patientId: patientId,
                loincCode: "2339-0", // Glucose test
                status: "active"
            ));
        }

        // Act
        var result = await _controller.SearchServiceRequests(
            patient: patientId,
            code: "2339-0",
            status: "active",
            intent: null,
            category: null,
            authored: null,
            requester: null,
            performer: null,
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
        nextLink.Url.Should().Contain("status=active");
        nextLink.Url.Should().Contain("_count=10");
        nextLink.Url.Should().Contain("_offset=10");
    }

    #endregion
}
