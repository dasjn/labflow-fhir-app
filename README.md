# LabFlow FHIR API

**Laboratory Results Interoperability System**

A production-ready FHIR R4 compliant API for seamless laboratory results exchange between healthcare systems.

---

## 🎯 Project Status

**Timeline**: Week 1 of 2-week development sprint
**Current Phase**: Core Patient Resource Implementation
**Last Updated**: 2025-10-13

---

## ✅ Completed

### Infrastructure & Setup
- [x] Solution and project structure created
- [x] NuGet packages installed
  - [x] Firely SDK (Hl7.Fhir.R4) v5.12.2
  - [x] Entity Framework Core v9.0.9
  - [x] SQLite provider v9.0.9 (dev) / PostgreSQL v9.0.4 (prod-ready)
  - [x] Serilog v9.0.0
  - [x] Swashbuckle (Swagger) v9.0.6
  - [x] FluentAssertions v8.7.1 (tests)
  - [x] ASP.NET Core Testing v8.0.11
- [x] Program.cs configured
  - [x] Serilog structured logging
  - [x] Swagger/OpenAPI documentation
  - [x] CORS for development
  - [x] Health check endpoint
- [x] Database setup
  - [x] FhirDbContext with SQLite (dev) + PostgreSQL-ready
  - [x] PatientEntity with hybrid storage (JSON + indexed fields)
  - [x] Initial migration created and applied
  - [x] Database file created (`labflow.db`)

### FHIR Resources
- [x] **Patient Resource** (COMPLETED ✅)
  - [x] Entity model created
  - [x] Database schema with indexes
  - [x] Controller with GET/POST endpoints
  - [x] FHIR validation (basic)
  - [x] Firely SDK integration (serialization/deserialization)
  - [x] **Tested successfully** - GET and POST working correctly
  - [x] **Search endpoints implemented** - name, identifier, birthdate, gender with Bundle responses
  - [x] **Unit tests** - 13 focused tests covering GET, POST, and SEARCH operations (all passing ✅)

---

## 🚧 In Progress

- **Observation Resource** - Next phase for laboratory results

---

## 📦 Technology Stack

### Core
- **.NET 8** - Web API framework
- **Firely SDK (Hl7.Fhir.R4)** - FHIR R4 implementation
- **Entity Framework Core 9** - ORM
- **SQLite** (development) / **PostgreSQL** (production)

### Supporting
- **Serilog** - Structured logging
- **Swashbuckle** - OpenAPI/Swagger documentation
- **xUnit** - Unit testing
- **FluentAssertions** - Test assertions

---

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- (Optional) PostgreSQL if using production setup

### API Endpoints

**Patient Resource:**
```bash
# Create patient
POST /Patient
Content-Type: application/json
Body: { "resourceType": "Patient", ... }

# Read patient by ID
GET /Patient/{id}

# Search patients
GET /Patient?name=García
GET /Patient?identifier=12345678
GET /Patient?birthdate=1985-03-15
GET /Patient?gender=male
GET /Patient?name=Smith&gender=male  # Combined search
```

### Run the API

```bash
# Navigate to API project
cd LabFlow.API

# Run the application
dotnet run

# API will be available at:
# - https://localhost:7xxx (HTTPS)
# - http://localhost:5xxx (HTTP)
# - Swagger UI at root (/)
```

### Build

```bash
dotnet build
```

### Run Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~PatientControllerTests"
```

**Test Coverage:**
- **13 focused unit tests** covering Patient resource operations
- **GetPatient** (2 tests): Valid ID, Not Found scenarios
- **CreatePatient** (3 tests): Valid resource, Invalid content-type, FHIR validation
- **SearchPatients** (8 tests): Individual parameters (name, identifier, birthdate, gender), combined filters, empty results, invalid inputs
- **Test isolation**: Each test uses unique in-memory database
- **Framework**: xUnit + FluentAssertions + EF Core InMemory

All tests passing ✅ (13/13)

### Database Commands

```bash
# Create new migration
dotnet ef migrations add MigrationName --project LabFlow.API

# Apply migrations
dotnet ef database update --project LabFlow.API

# Remove last migration
dotnet ef migrations remove --project LabFlow.API
```

---

## 📁 Project Structure

```
LabFlow/
├── LabFlow.API/
│   ├── Controllers/          # API endpoints
│   ├── Data/
│   │   ├── Entities/         # Database entities
│   │   ├── Migrations/       # EF Core migrations
│   │   └── FhirDbContext.cs  # Database context
│   ├── Services/             # Business logic (future)
│   ├── Models/               # DTOs (future)
│   ├── Program.cs            # App configuration
│   ├── appsettings.json      # Configuration
│   └── labflow.db            # SQLite database (dev)
├── LabFlow.Tests/
│   └── (test files)
└── LabFlow.sln
```

---

## 🔍 FHIR Implementation Details

### Storage Strategy

**Hybrid Approach** (industry standard):
- Full FHIR resource stored as JSON (TEXT in SQLite, jsonb in PostgreSQL)
- Key fields extracted to columns for fast searching
- Firely SDK handles all FHIR validation and serialization

### PatientEntity Fields

**Searchable Fields** (indexed for performance):
- `FamilyName`, `GivenName` → FHIR search: `name`
- `Identifier` → FHIR search: `identifier`
- `BirthDate` → FHIR search: `birthdate`
- `Gender` → FHIR search: `gender`
- `LastUpdated` → FHIR search: `_lastUpdated`

**Metadata**:
- `VersionId` - Optimistic concurrency control
- `IsDeleted` - Soft delete for audit trail
- `CreatedAt`, `LastUpdated` - Tracking

---

## 🎯 Roadmap

### Phase 1: Core Resources (Week 1) ← WE ARE HERE
- [x] **Patient** (CRUD + search + tests) ✅
- [ ] Observation (laboratory results)
- [ ] Observation unit tests

### Phase 2: Extended Features (Week 2)
- [ ] DiagnosticReport
- [ ] ServiceRequest
- [ ] **CapabilityStatement** (GET /metadata) - FHIR standard server documentation
- [ ] JWT authentication
- [ ] Advanced FHIR search (_include, _revinclude)
- [ ] Integration tests
- [ ] CI/CD pipeline
- [ ] PostgreSQL migration
- [ ] Azure deployment

---

## 📝 Development Notes

### Database Provider

Currently using **SQLite** for rapid development. To switch to **PostgreSQL**:

1. Update `appsettings.json`:
```json
"ConnectionStrings": {
  "FhirDatabase": "Host=localhost;Database=labflow_fhir;Username=postgres;Password=xxx"
}
```

2. Update `Program.cs` (line 29):
```csharp
options.UseSqlite(...)
// Change to:
options.UseNpgsql(...)
```

3. Delete migrations and recreate:
```bash
rm -rf LabFlow.API/Migrations/
dotnet ef migrations add InitialCreate --project LabFlow.API
dotnet ef database update --project LabFlow.API
```

---

## 📖 Resources

- [FHIR R4 Specification](http://hl7.org/fhir/R4/)
- [Firely SDK Documentation](https://docs.fire.ly/projects/Firely-NET-SDK/)
- [Project Planning](./labflow-fhir-readme.md)
- [Learning Path](./fhir_learning_path.md)

---

## 👨‍💻 Developer

**Background**: 6 years Field Service Engineer (pharma/medical devices) + 2 years .NET development
**Goal**: Demonstrate FHIR + IEC 62304 awareness + production-ready skills for Medical Device Software Engineer roles

---

## 📄 License

MIT License - See LICENSE file for details
