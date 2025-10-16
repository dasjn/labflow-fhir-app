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

public class PatientControllerTests : IDisposable
{
    private readonly FhirDbContext _context;
    private readonly PatientController _controller;
    private readonly FhirJsonSerializer _serializer;
    private readonly FhirJsonParser _parser;

    public PatientControllerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<FhirDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        _context = new FhirDbContext(options);

        // Setup Firely SDK
        _serializer = new FhirJsonSerializer();
        _parser = new FhirJsonParser();

        // Setup logger
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<PatientController>();

        // Create controller
        _controller = new PatientController(_context, _serializer, _parser, logger);

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

    private Patient CreateTestPatient(string familyName, string givenName, string? identifier = null,
        string? birthDate = null, AdministrativeGender? gender = null)
    {
        var patient = new Patient
        {
            Name = new List<HumanName>
            {
                new HumanName
                {
                    Use = HumanName.NameUse.Official,
                    Family = familyName,
                    Given = new[] { givenName }
                }
            }
        };

        if (identifier != null)
        {
            patient.Identifier = new List<Identifier>
            {
                new Identifier
                {
                    System = "urn:oid:2.16.840.1.113883.2.4.6.3",
                    Value = identifier
                }
            };
        }

        if (birthDate != null)
        {
            patient.BirthDate = birthDate;
        }

        if (gender.HasValue)
        {
            patient.Gender = gender.Value;
        }

        return patient;
    }

    private async Task<string> SeedPatient(Patient patient)
    {
        patient.Id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var fhirJson = _serializer.SerializeToString(patient);

        var entity = new PatientEntity
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

        _context.Patients.Add(entity);
        await _context.SaveChangesAsync();

        return patient.Id;
    }

    #endregion

    #region GetPatient Tests

    [Fact]
    public async Task GetPatient_ValidId_ReturnsPatient()
    {
        // Arrange
        var patient = CreateTestPatient("García", "Juan", "12345678");
        var patientId = await SeedPatient(patient);

        // Act
        var result = await _controller.GetPatient(patientId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedPatient = okResult!.Value as Patient;

        returnedPatient.Should().NotBeNull();
        returnedPatient!.Id.Should().Be(patientId);
        returnedPatient.Name.Should().HaveCount(1);
        returnedPatient.Name[0].Family.Should().Be("García");
        returnedPatient.Name[0].Given.Should().Contain("Juan");
    }

    [Fact]
    public async Task GetPatient_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _controller.GetPatient(nonExistentId);

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

    #region CreatePatient Tests

    [Fact]
    public async Task CreatePatient_ValidPatient_ReturnsCreated()
    {
        // Arrange
        var patient = CreateTestPatient("Smith", "John", "87654321", "1990-05-20", AdministrativeGender.Male);
        var patientJson = _serializer.SerializeToString(patient);

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(patientJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = patientJson.Length;

        // Act
        var result = await _controller.CreatePatient();

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var returnedPatient = createdResult!.Value as Patient;

        returnedPatient.Should().NotBeNull();
        returnedPatient!.Id.Should().NotBeNullOrEmpty();
        returnedPatient.Name[0].Family.Should().Be("Smith");
        returnedPatient.Meta.Should().NotBeNull();
        returnedPatient.Meta!.VersionId.Should().Be("1");

        // Verify it was saved to database
        var savedEntity = await _context.Patients.FindAsync(returnedPatient.Id);
        savedEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePatient_InvalidContentType_ReturnsBadRequest()
    {
        // Arrange
        var patientJson = "{\"test\": \"data\"}";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(patientJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "text/plain";

        // Act
        var result = await _controller.CreatePatient();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome.Should().NotBeNull();
        outcome!.Issue[0].Diagnostics.Should().Contain("Content-Type");
    }

    [Fact]
    public async Task CreatePatient_NoNameOrIdentifier_ReturnsBadRequest()
    {
        // Arrange
        var patient = new Patient(); // No name, no identifier
        var patientJson = _serializer.SerializeToString(patient);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(patientJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";

        // Act
        var result = await _controller.CreatePatient();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var outcome = badRequestResult!.Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("name or identifier");
    }

    #endregion

    #region SearchPatients Tests

    [Fact]
    public async Task SearchPatients_ByName_ReturnsMatchingPatients()
    {
        // Arrange
        await SeedPatient(CreateTestPatient("García", "Juan", "111"));
        await SeedPatient(CreateTestPatient("Smith", "John", "222"));
        await SeedPatient(CreateTestPatient("García", "Maria", "333"));

        // Act
        var result = await _controller.SearchPatients(name: "García", null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var bundle = okResult!.Value as Bundle;

        bundle.Should().NotBeNull();
        bundle!.Type.Should().Be(Bundle.BundleType.Searchset);
        bundle.Total.Should().Be(2);
        bundle.Entry.Should().HaveCount(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((Patient)e.Resource).Name[0].Family == "García");
    }

    [Fact]
    public async Task SearchPatients_ByIdentifier_ReturnsMatchingPatient()
    {
        // Arrange
        await SeedPatient(CreateTestPatient("García", "Juan", "12345678"));
        await SeedPatient(CreateTestPatient("Smith", "John", "87654321"));

        // Act
        var result = await _controller.SearchPatients(null, identifier: "12345678", null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(1);
        ((Patient)bundle.Entry[0].Resource).Identifier[0].Value.Should().Be("12345678");
    }

    [Fact]
    public async Task SearchPatients_ByBirthdate_ReturnsMatchingPatients()
    {
        // Arrange
        await SeedPatient(CreateTestPatient("García", "Juan", birthDate: "1985-03-15"));
        await SeedPatient(CreateTestPatient("Smith", "John", birthDate: "1990-05-20"));

        // Act
        var result = await _controller.SearchPatients(null, null, birthdate: "1985-03-15", null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(1);
        ((Patient)bundle.Entry[0].Resource).BirthDate.Should().Be("1985-03-15");
    }

    [Fact]
    public async Task SearchPatients_ByGender_ReturnsMatchingPatients()
    {
        // Arrange
        await SeedPatient(CreateTestPatient("García", "Juan", gender: AdministrativeGender.Male));
        await SeedPatient(CreateTestPatient("Smith", "Jane", gender: AdministrativeGender.Female));
        await SeedPatient(CreateTestPatient("Doe", "John", gender: AdministrativeGender.Male));

        // Act
        var result = await _controller.SearchPatients(null, null, null, gender: "male", null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(2);
        bundle.Entry.Should().OnlyContain(e =>
            ((Patient)e.Resource).Gender == AdministrativeGender.Male);
    }

    [Fact]
    public async Task SearchPatients_CombinedFilters_ReturnsMatchingPatients()
    {
        // Arrange
        await SeedPatient(CreateTestPatient("García", "Juan", gender: AdministrativeGender.Male));
        await SeedPatient(CreateTestPatient("García", "Maria", gender: AdministrativeGender.Female));
        await SeedPatient(CreateTestPatient("Smith", "John", gender: AdministrativeGender.Male));

        // Act
        var result = await _controller.SearchPatients(name: "García", null, null, gender: "male", null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(1);
        var patient = (Patient)bundle.Entry[0].Resource;
        patient.Name[0].Family.Should().Be("García");
        patient.Gender.Should().Be(AdministrativeGender.Male);
    }

    [Fact]
    public async Task SearchPatients_NoMatches_ReturnsEmptyBundle()
    {
        // Arrange
        await SeedPatient(CreateTestPatient("García", "Juan"));

        // Act
        var result = await _controller.SearchPatients(name: "NonExistent", null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(0);
        bundle.Entry.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchPatients_InvalidBirthdate_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchPatients(null, null, birthdate: "invalid-date", null, null, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("Invalid birthdate format");
    }

    [Fact]
    public async Task SearchPatients_InvalidGender_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchPatients(null, null, null, gender: "invalid-gender", null, null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("Invalid gender value");
    }

    #endregion

    #region UpdatePatient Tests

    [Fact]
    public async Task UpdatePatient_ValidPatient_ReturnsOk()
    {
        // Arrange
        var patient = CreateTestPatient("García", "Juan", "12345678");
        var patientId = await SeedPatient(patient);

        // Update patient data
        patient.Id = patientId;
        patient.Name[0].Family = "García-Modified";
        patient.Name[0].Given = new[] { "Juan Carlos" };
        patient.BirthDate = "1990-05-20";
        patient.Gender = AdministrativeGender.Male;

        var patientJson = _serializer.SerializeToString(patient);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(patientJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = patientJson.Length;

        // Act
        var result = await _controller.UpdatePatient(patientId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedPatient = okResult!.Value as Patient;

        returnedPatient.Should().NotBeNull();
        returnedPatient!.Id.Should().Be(patientId);
        returnedPatient.Name[0].Family.Should().Be("García-Modified");
        returnedPatient.Name[0].Given.Should().Contain("Juan Carlos");
        returnedPatient.BirthDate.Should().Be("1990-05-20");
        returnedPatient.Gender.Should().Be(AdministrativeGender.Male);
        returnedPatient.Meta!.VersionId.Should().Be("2");

        // Verify database was updated
        var updatedEntity = await _context.Patients.FindAsync(patientId);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.FamilyName.Should().Be("García-Modified");
        updatedEntity.VersionId.Should().Be(2);
    }

    [Fact]
    public async Task UpdatePatient_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();
        var patient = CreateTestPatient("García", "Juan");
        patient.Id = nonExistentId;

        var patientJson = _serializer.SerializeToString(patient);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(patientJson));
        _controller.HttpContext.Request.Body = stream;
        _controller.HttpContext.Request.ContentType = "application/json";
        _controller.HttpContext.Request.ContentLength = patientJson.Length;

        // Act
        var result = await _controller.UpdatePatient(nonExistentId);

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

    #region DeletePatient Tests

    [Fact]
    public async Task DeletePatient_ValidId_ReturnsNoContent()
    {
        // Arrange
        var patient = CreateTestPatient("García", "Juan", "12345678");
        var patientId = await SeedPatient(patient);

        // Act
        var result = await _controller.DeletePatient(patientId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify soft delete
        var deletedEntity = await _context.Patients.FindAsync(patientId);
        deletedEntity.Should().NotBeNull();
        deletedEntity!.IsDeleted.Should().BeTrue();
        deletedEntity.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeletePatient_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _controller.DeletePatient(nonExistentId);

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
    public async Task SearchPatients_WithDefaultPagination_Returns20Results()
    {
        // Arrange - Seed 30 patients
        for (int i = 1; i <= 30; i++)
        {
            await SeedPatient(CreateTestPatient($"Family{i:D2}", $"Given{i:D2}", $"ID{i:D3}"));
            await Task.Delay(10); // Ensure different LastUpdated timestamps
        }

        // Act - No pagination parameters (should default to _count=20, _offset=0)
        var result = await _controller.SearchPatients(null, null, null, null, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Type.Should().Be(Bundle.BundleType.Searchset);
        bundle.Total.Should().Be(30); // Total count
        bundle.Entry.Should().HaveCount(20); // Page size

        // Verify links
        bundle.Link.Should().Contain(l => l.Relation == "self");
        bundle.Link.Should().Contain(l => l.Relation == "next");
        bundle.Link.Should().NotContain(l => l.Relation == "previous"); // First page has no previous
    }

    [Fact]
    public async Task SearchPatients_WithCustomCount_ReturnsCorrectPageSize()
    {
        // Arrange - Seed 15 patients
        for (int i = 1; i <= 15; i++)
        {
            await SeedPatient(CreateTestPatient($"Family{i:D2}", $"Given{i:D2}"));
        }

        // Act - Request 5 results per page
        var result = await _controller.SearchPatients(null, null, null, null, _count: 5, _offset: null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(15);
        bundle.Entry.Should().HaveCount(5);

        // Verify links
        bundle.Link.Should().Contain(l => l.Relation == "next");
    }

    [Fact]
    public async Task SearchPatients_WithOffset_ReturnsCorrectPage()
    {
        // Arrange - Seed 10 patients with known order
        var ids = new List<string>();
        for (int i = 1; i <= 10; i++)
        {
            var patient = CreateTestPatient($"Family{i:D2}", $"Given{i:D2}");
            ids.Add(await SeedPatient(patient));
            await Task.Delay(10); // Ensure different LastUpdated timestamps
        }

        // Act - Request second page (offset=5, count=5)
        var result = await _controller.SearchPatients(null, null, null, null, _count: 5, _offset: 5);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(10);
        bundle.Entry.Should().HaveCount(5); // Last 5 results

        // Verify links
        bundle.Link.Should().Contain(l => l.Relation == "self");
        bundle.Link.Should().Contain(l => l.Relation == "previous");
        bundle.Link.Should().NotContain(l => l.Relation == "next"); // Last page has no next
    }

    [Fact]
    public async Task SearchPatients_CountTooLarge_ReturnsBadRequest()
    {
        // Act - Request more than 100 results
        var result = await _controller.SearchPatients(null, null, null, null, _count: 101, _offset: null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("_count parameter must be between 1 and 100");
    }

    [Fact]
    public async Task SearchPatients_CountZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchPatients(null, null, null, null, _count: 0, _offset: null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("_count parameter must be between 1 and 100");
    }

    [Fact]
    public async Task SearchPatients_NegativeOffset_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchPatients(null, null, null, null, _count: null, _offset: -1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var outcome = ((BadRequestObjectResult)result).Value as OperationOutcome;

        outcome!.Issue[0].Diagnostics.Should().Contain("_offset parameter must be non-negative");
    }

    [Fact]
    public async Task SearchPatients_PaginationWithFilters_PreservesQueryParams()
    {
        // Arrange - Seed patients with García surname
        for (int i = 1; i <= 25; i++)
        {
            await SeedPatient(CreateTestPatient("García", $"Given{i:D2}"));
        }

        // Act - Search by name with pagination
        var result = await _controller.SearchPatients(name: "García", null, null, null, _count: 10, _offset: 0);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(25);
        bundle.Entry.Should().HaveCount(10);

        // Verify that pagination links preserve the 'name' parameter (URL-encoded)
        var nextLink = bundle.Link.FirstOrDefault(l => l.Relation == "next");
        nextLink.Should().NotBeNull();
        nextLink!.Url.Should().Contain("name=Garc%C3%ADa"); // URL-encoded García
        nextLink.Url.Should().Contain("_count=10");
        nextLink.Url.Should().Contain("_offset=10");
    }

    [Fact]
    public async Task SearchPatients_OffsetBeyondResults_ReturnsEmptyPage()
    {
        // Arrange - Seed only 5 patients
        for (int i = 1; i <= 5; i++)
        {
            await SeedPatient(CreateTestPatient($"Family{i}", $"Given{i}"));
        }

        // Act - Request page beyond available results
        var result = await _controller.SearchPatients(null, null, null, null, _count: 10, _offset: 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var bundle = ((OkObjectResult)result).Value as Bundle;

        bundle!.Total.Should().Be(5); // Total count
        bundle.Entry.Should().BeEmpty(); // No results on this page

        // Verify links
        bundle.Link.Should().Contain(l => l.Relation == "previous");
        bundle.Link.Should().NotContain(l => l.Relation == "next");
    }

    #endregion
}
