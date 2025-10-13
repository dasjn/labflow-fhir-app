# LabFlow FHIR API

[![.NET Build & Test](https://github.com/dasjn/labflow-fhir-app/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dasjn/labflow-fhir-app/actions/workflows/dotnet.yml)

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

- [x] **Observation Resource** (COMPLETED ✅)
  - [x] Entity model with laboratory-focused fields
  - [x] Database schema with optimized indexes (patient+date compound index)
  - [x] Controller with GET/POST/SEARCH endpoints
  - [x] Patient reference validation
  - [x] LOINC code support
  - [x] Search by patient, code, category, date, status
  - [x] **Unit tests** - 16 focused tests covering GET, POST, and SEARCH operations (all passing ✅)

- [x] **DiagnosticReport Resource** (COMPLETED ✅)
  - [x] Entity model for grouped laboratory reports
  - [x] Database schema with optimized indexes (patient+issued compound index)
  - [x] Controller with GET/POST/SEARCH endpoints
  - [x] Patient and observation references validation
  - [x] LOINC panel code support (e.g., CBC, Lipid Panel)
  - [x] Search by patient, code, category, date, issued, status
  - [x] **Unit tests** - 18 focused tests covering GET, POST, and SEARCH operations (all passing ✅)

- [x] **CapabilityStatement** (COMPLETED ✅)
  - [x] GET /metadata endpoint
  - [x] Documents all supported resources (Patient, Observation, DiagnosticReport)
  - [x] Lists all interactions (read, create, search-type)
  - [x] Details all search parameters with types and documentation
  - [x] FHIR R4 compliance

### CI/CD & Automation
- [x] **GitHub Actions** - Automated build & test pipeline
  - [x] Runs on every push to main
  - [x] Executes all 48 unit tests
  - [x] Build status badge in README

---

## 🚧 In Progress

- None - All Phase 3 core resources completed!

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

**FHIR Metadata:**
```bash
# Get server capability statement
GET /metadata
# Returns CapabilityStatement with all supported resources and operations
```

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

**Observation Resource (Laboratory Results):**
```bash
# Create observation
POST /Observation
Content-Type: application/json
Body: { "resourceType": "Observation", "subject": { "reference": "Patient/123" }, ... }

# Read observation by ID
GET /Observation/{id}

# Search observations
GET /Observation?patient=Patient/123          # All observations for a patient
GET /Observation?patient=123                  # Also accepts just the ID
GET /Observation?code=2339-0                  # By LOINC code (Glucose)
GET /Observation?category=laboratory          # By category
GET /Observation?date=2025-10-13             # By observation date
GET /Observation?status=final                 # By status
GET /Observation?patient=123&code=2339-0      # Combined search
```

**DiagnosticReport Resource (Grouped Lab Reports):**
```bash
# Create diagnostic report (grouping multiple observations)
POST /DiagnosticReport
Content-Type: application/json
Body: {
  "resourceType": "DiagnosticReport",
  "subject": { "reference": "Patient/123" },
  "result": [
    { "reference": "Observation/obs1" },
    { "reference": "Observation/obs2" }
  ],
  ...
}

# Read diagnostic report by ID
GET /DiagnosticReport/{id}

# Search diagnostic reports
GET /DiagnosticReport?patient=Patient/123     # All reports for a patient
GET /DiagnosticReport?patient=123             # Also accepts just the ID
GET /DiagnosticReport?code=58410-2            # By LOINC panel code (CBC)
GET /DiagnosticReport?category=LAB            # By category (LAB, RAD, PATH)
GET /DiagnosticReport?date=2025-10-13         # By effective date (study performed)
GET /DiagnosticReport?issued=2025-10-13       # By issued date (report published)
GET /DiagnosticReport?status=final            # By status
GET /DiagnosticReport?patient=123&code=58410-2  # Combined search
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
- **48 focused unit tests** covering Patient, Observation, and DiagnosticReport resources
- **Patient** (13 tests):
  - GetPatient (2): Valid ID, Not Found scenarios
  - CreatePatient (3): Valid resource, Invalid content-type, FHIR validation
  - SearchPatients (8): Individual parameters (name, identifier, birthdate, gender), combined filters, empty results, invalid inputs
- **Observation** (16 tests):
  - GetObservation (2): Valid ID, Not Found scenarios
  - CreateObservation (5): Valid resource, Invalid content-type, Missing status/code, Non-existent patient reference
  - SearchObservations (9): Individual parameters (patient, code, category, date, status), patient reference format handling, combined filters, empty results, invalid date
- **DiagnosticReport** (18 tests):
  - GetDiagnosticReport (2): Valid ID, Not Found scenarios
  - CreateDiagnosticReport (7): Valid report, Invalid content-type, Missing status/code, Non-existent patient, Non-existent observation, Invalid reference type
  - SearchDiagnosticReports (10): Individual parameters (patient, code, category, date, issued, status), combined filters, empty results, invalid date/issued
- **Test isolation**: Each test uses unique in-memory database
- **Framework**: xUnit + FluentAssertions + EF Core InMemory

All tests passing ✅ (48/48)

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

### ObservationEntity Fields

**Searchable Fields** (indexed for performance):
- `PatientId` → FHIR search: `patient` or `subject`
- `Code` → FHIR search: `code` (LOINC codes for lab tests)
- `Category` → FHIR search: `category` (laboratory, vital-signs, etc.)
- `Status` → FHIR search: `status` (final, preliminary, etc.)
- `EffectiveDateTime` → FHIR search: `date`
- `ValueQuantity`, `ValueUnit` → FHIR search: `value-quantity`
- `ValueCodeableConcept` → FHIR search: `value-concept`
- `LastUpdated` → FHIR search: `_lastUpdated`

**Compound Index**:
- `PatientId + EffectiveDateTime` - Optimized for "get patient's recent lab results"

**Metadata**: Same as Patient (VersionId, IsDeleted, CreatedAt, LastUpdated)

### DiagnosticReportEntity Fields

**Searchable Fields** (indexed for performance):
- `PatientId` → FHIR search: `patient` or `subject`
- `Code` → FHIR search: `code` (LOINC panel codes, e.g., "58410-2" for CBC)
- `Category` → FHIR search: `category` (LAB, RAD, PATH, etc.)
- `Status` → FHIR search: `status` (registered, partial, preliminary, final, etc.)
- `EffectiveDateTime` → FHIR search: `date` (when study was performed)
- `Issued` → FHIR search: `issued` (when report was published)
- `ResultIds` → Comma-separated observation IDs (enables searching by result)
- `Conclusion` → Clinical interpretation text
- `LastUpdated` → FHIR search: `_lastUpdated`

**Compound Index**:
- `PatientId + Issued` - Optimized for "get patient's recent lab reports"

**Metadata**: Same as Patient (VersionId, IsDeleted, CreatedAt, LastUpdated)

---

## 🎯 Roadmap

### Phase 1: Core Resources (Week 1) - COMPLETED ✅
- [x] **Patient** (CRUD + search + 13 tests) ✅
- [x] **Observation** (CRUD + search + patient validation + 16 tests) ✅

### Phase 2: FHIR Compliance & Automation (Week 2) - COMPLETED ✅
- [x] **CapabilityStatement** (GET /metadata) - FHIR standard server documentation ✅
- [x] **CI/CD Pipeline** (GitHub Actions) - Automated build & test ✅

### Phase 3: Grouped Reports & References (Week 3) - COMPLETED ✅
- [x] **DiagnosticReport** (CRUD + search + patient/observation validation + 18 tests) ✅

### Phase 4: Advanced Features (Future)
- [ ] ServiceRequest - Laboratory order workflow
- [ ] JWT authentication
- [ ] Advanced FHIR search (_include, _revinclude)
- [ ] Integration tests
- [ ] PostgreSQL migration
- [ ] Azure deployment

### Phase 5: Enhanced Validation (Future)
- [ ] **LOINC code validation** for Observation.code
  - Format validation (regex: `^\d{1,5}-\d$`)
  - Optional: Validate against LOINC database subset
- [ ] **SNOMED CT validation** for clinical concepts
- [ ] **ICD-10 validation** for diagnosis codes
- [ ] **UCUM validation** for units of measurement (e.g., "mg/dL", "mmol/L")
- [ ] **Gender validation** against FHIR ValueSet (already validates male/female/other/unknown)
- [ ] **Reference validation** enhancement (already validates Patient exists for Observation)
  - Validate reference format (e.g., "Patient/[id]")
  - Validate referenced resource type matches

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
