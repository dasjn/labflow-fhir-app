# Testing Strategy

## Philosophy

This project follows a **pragmatic testing approach** optimized for rapid portfolio development while maintaining quality:

1. **Comprehensive unit tests** for business logic (90 tests)
2. **Clear documentation** of testing limitations and trade-offs
3. **Roadmap for production-grade testing** (TestContainers + PostgreSQL)

---

## Current Test Coverage (90 Tests)

### Unit Tests by Resource

**Patient Resource** (13 tests):
- GET operations: Valid ID, Not Found
- POST operations: Valid resource, Invalid content-type, FHIR validation
- SEARCH operations: name, identifier, birthdate, gender filters + combined + edge cases

**Observation Resource** (16 tests):
- GET operations: Valid ID, Not Found
- POST operations: Valid resource, Invalid content-type, Missing required fields, Patient reference validation
- SEARCH operations: patient, code, category, date, status filters + combined + edge cases

**DiagnosticReport Resource** (19 tests):
- GET operations: Valid ID, Not Found
- POST operations: Valid resource, Patient validation, Observation validation, Invalid reference types
- SEARCH operations: patient, code, category, date, issued, status filters + combined + edge cases

**ServiceRequest Resource** (16 tests):
- GET operations: Valid ID, Not Found
- POST operations: Valid resource, Invalid content-type, Missing required fields, Patient reference validation
- SEARCH operations: patient, code, status, intent, category, authored filters + combined + edge cases

**Authentication Service** (15 tests):
- Password hashing: BCrypt hash generation, verification success/failure
- JWT generation: Token structure, claims validation, expiration, signature
- Token validation: Valid token, expired, invalid signature, malformed, missing claims

**Authentication Controller** (11 tests):
- Register: Valid registration, duplicate email, invalid role, password requirements
- Login: Valid login, wrong password, non-existent user, token structure
- GetMe: Valid token, invalid token, missing token

---

## Current Limitations

### No Integration Tests with Real Database

**Current State**: Tests use EF Core InMemory database for fast feedback.

**Why This Decision:**
- **Rapid Development**: InMemory tests run in milliseconds, enabling TDD workflow
- **No Dependencies**: No need for Docker, PostgreSQL, or complex CI/CD setup
- **Portfolio Context**: This is a demonstration project, not production software serving users

**Trade-offs:**
- InMemory behavior differs from PostgreSQL (constraints, triggers, SQL-specific features not tested)
- Database migrations are not validated during tests
- No true end-to-end validation of the complete stack

**Mitigation Strategies:**
1. **Manual Smoke Tests**: Critical workflows validated with Postman/curl against real PostgreSQL
2. **Comprehensive Unit Tests**: 90 tests cover business logic, validation, edge cases
3. **Future Migration Path**: Clear roadmap to TestContainers for production-grade testing

---

## Testing Best Practices Applied

### ✅ What We Do Well

1. **Test Isolation**: Each test uses unique in-memory database (Guid-based names)
2. **AAA Pattern**: Arrange-Act-Assert structure for readability
3. **Pragmatic Coverage**: 70-80% estimated coverage focusing on business logic
4. **FluentAssertions**: Readable assertions with clear error messages
5. **Helper Methods**: Reusable test data builders (CreateTestPatient, SeedPatient, etc.)
6. **No Framework Testing**: Tests focus on our code, not EF Core or ASP.NET behavior

### 📝 Documentation

- **REQ-XXX Tags**: Traceability from requirements to tests
- **Descriptive Test Names**: `Operation_Scenario_ExpectedBehavior` pattern
- **Inline Comments**: Complex test logic is explained

---

## Future Testing Roadmap

### Phase 1: TestContainers (Recommended Next Step)

**Goal**: True integration tests with real PostgreSQL database.

**Implementation**:
```csharp
public class IntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("labflow_test")
            .Build();

        await _postgres.StartAsync();

        // Apply real migrations
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        // Run dotnet ef database update
    }
}
```

**Benefits**:
- Real PostgreSQL behavior
- Migration validation
- CI/CD compatible (GitHub Actions supports Docker)
- Same database engine as production

**Effort**: 1-2 days

---

### Phase 2: Performance Tests

**Goal**: Validate query performance with realistic data volumes.

**Scenarios**:
- 10,000 patients + 100,000 observations
- Search with pagination (100 results per page)
- Complex queries with multiple filters
- Concurrent requests (load testing)

**Tools**: BenchmarkDotNet, k6, or JMeter

**Effort**: 2-3 days

---

### Phase 3: End-to-End Tests

**Goal**: Validate complete user workflows via UI or API.

**Scenarios**:
- Register user → Login → Create Patient → Order Lab Test → Enter Results → View Report
- Multi-user scenarios (Doctor orders, Lab Tech enters results)
- Authorization enforcement across workflows

**Tools**: Playwright (if UI), RestSharp/Refit (if API-only)

**Effort**: 3-5 days

---

## Running Tests

### All Tests
```bash
dotnet test
# Expected: 90 tests passing
```

### Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~PatientControllerTests"
```

### With Detailed Output
```bash
dotnet test --verbosity normal
```

### CI/CD (GitHub Actions)
```yaml
- name: Run Tests
  run: dotnet test --no-build --verbosity normal
```

---

## Why This Approach is Production-Aware

This testing strategy demonstrates **professional software engineering judgment**:

1. **Context-Appropriate**: Portfolio project ≠ Enterprise production system
2. **Trade-off Awareness**: Explicitly documents limitations and mitigation strategies
3. **Scalable Architecture**: Clear path to production-grade testing when needed
4. **Pragmatic Over Perfect**: 90 solid unit tests > 10 flaky integration tests

### In Interviews

This approach shows:
- Understanding of different testing levels (unit, integration, E2E)
- Awareness of infrastructure requirements (Docker, CI/CD)
- Ability to make pragmatic decisions based on context
- Clear technical communication (documentation)

---

## References

- [Martin Fowler - TestPyramid](https://martinfowler.com/bliki/TestPyramid.html)
- [TestContainers Documentation](https://testcontainers.com/)
- [xUnit Best Practices](https://xunit.net/docs/comparisons)
- [FHIR R4 Testing Guide](http://hl7.org/fhir/R4/testing.html)

---

**Last Updated**: 2025-10-16
**Version**: 1.0
**Status**: Active (90/90 tests passing ✅)
