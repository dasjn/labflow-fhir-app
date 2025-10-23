# LabFlow FHIR API

[![.NET Build & Test](https://github.com/dasjn/labflow-fhir-app/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dasjn/labflow-fhir-app/actions/workflows/dotnet.yml)

**Laboratory Results Interoperability System**

A production-ready FHIR R4 compliant API for seamless laboratory results exchange between healthcare systems.

---

## 🎯 Project Status

**Timeline**: Week 5 of development sprint
**Current Phase**: Phase 5 Complete - JWT Authentication & Pagination + Custom FHIR Serialization
**Last Updated**: 2025-10-23
**Tests**: 132/132 passing ✅

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
  - [x] Custom FhirJsonOutputFormatter for FHIR R4 compliant serialization
- [x] Database setup
  - [x] FhirDbContext with SQLite (dev) + PostgreSQL-ready
  - [x] PatientEntity with hybrid storage (JSON + indexed fields)
  - [x] Initial migration created and applied
  - [x] Database file created (`labflow.db`)

### FHIR Resources
- [x] **Patient Resource** (COMPLETED ✅)
  - [x] Entity model created
  - [x] Database schema with indexes
  - [x] Controller with GET/POST/PUT/DELETE/SEARCH endpoints (full CRUD)
  - [x] FHIR validation (basic)
  - [x] Firely SDK integration (serialization/deserialization)
  - [x] **Tested successfully** - All CRUD operations working correctly
  - [x] **Search endpoints implemented** - name, identifier, birthdate, gender with Bundle responses
  - [x] **Soft delete** - IsDeleted flag for audit trail (no hard deletes)
  - [x] **Version tracking** - VersionId increments on updates
  - [x] **Unit tests** - 17 focused tests covering GET, POST, PUT, DELETE, and SEARCH operations (all passing ✅)

- [x] **Observation Resource** (COMPLETED ✅)
  - [x] Entity model with laboratory-focused fields
  - [x] Database schema with optimized indexes (patient+date compound index)
  - [x] Controller with GET/POST/PUT/DELETE/SEARCH endpoints (full CRUD)
  - [x] Patient reference validation
  - [x] LOINC code support
  - [x] Search by patient, code, category, date, status
  - [x] **Soft delete** - IsDeleted flag for audit trail
  - [x] **Version tracking** - VersionId increments on updates
  - [x] **Unit tests** - 20 focused tests covering GET, POST, PUT, DELETE, and SEARCH operations (all passing ✅)

- [x] **DiagnosticReport Resource** (COMPLETED ✅)
  - [x] Entity model for grouped laboratory reports
  - [x] Database schema with optimized indexes (patient+issued compound index)
  - [x] Controller with GET/POST/PUT/DELETE/SEARCH endpoints (full CRUD)
  - [x] Patient and observation references validation
  - [x] LOINC panel code support (e.g., CBC, Lipid Panel)
  - [x] Search by patient, code, category, date, issued, status
  - [x] **Soft delete** - IsDeleted flag for audit trail
  - [x] **Version tracking** - VersionId increments on updates
  - [x] **Unit tests** - 23 focused tests covering GET, POST, PUT, DELETE, and SEARCH operations (all passing ✅)

- [x] **ServiceRequest Resource** (COMPLETED ✅)
  - [x] Entity model for laboratory test orders
  - [x] Database schema with optimized indexes (patient+authored compound index)
  - [x] Controller with GET/POST/PUT/DELETE/SEARCH endpoints (full CRUD)
  - [x] Patient reference validation
  - [x] LOINC code support for test orders
  - [x] Search by patient, code, status, intent, category, authored, requester, performer
  - [x] **Soft delete** - IsDeleted flag for audit trail
  - [x] **Version tracking** - VersionId increments on updates
  - [x] **Unit tests** - 20 focused tests covering GET, POST, PUT, DELETE, and SEARCH operations (all passing ✅)

- [x] **CapabilityStatement** (COMPLETED ✅)
  - [x] GET /metadata endpoint
  - [x] Documents all supported resources (Patient, Observation, DiagnosticReport, ServiceRequest)
  - [x] Lists all interactions (read, create, search-type)
  - [x] Details all search parameters with types and documentation
  - [x] FHIR R4 compliance

### CI/CD & Automation
- [x] **GitHub Actions** - Automated build & test pipeline
  - [x] Runs on every push to main
  - [x] Executes all 132 unit tests
  - [x] Build status badge in README

### FHIR Pagination
- [x] **Offset-based pagination** for all search endpoints
  - [x] `_count` parameter (default: 20, max: 100) - results per page
  - [x] `_offset` parameter (default: 0) - number of results to skip
  - [x] Bundle.Link navigation (self, next, previous)
  - [x] Total count preserved in Bundle.Total
  - [x] Query parameter preservation across pages
  - [x] FHIR R4 compliant pagination links
  - [x] 26 pagination-specific unit tests (all passing ✅)

---

## 🚧 In Progress

- None - All Phase 4 core resources completed!

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

## 🔐 Authentication

**LabFlow FHIR API uses JWT Bearer token authentication** for all FHIR resource endpoints (except `/metadata` which is public per FHIR spec).

### Security Features

✅ **BCrypt Password Hashing** (work factor 11, 2^11 iterations)
✅ **JWT with HS256** (HMAC-SHA256 signing)
✅ **60-minute token expiration**
✅ **Role-based authorization** (Doctor, LabTechnician, Admin)
✅ **HTTPS enforcement** + HSTS (HTTP Strict Transport Security)
✅ **CORS restricted** to specific origins
✅ **Comprehensive audit logging** (WHO accessed WHAT WHEN)
✅ **FHIR-compatible** (documented in CapabilityStatement security section)

### User Roles & Permissions

| Role | Patient | Observation | DiagnosticReport | ServiceRequest |
|------|---------|-------------|------------------|----------------|
| **Doctor** | ✅ Full | ✅ Full | ✅ Full | ✅ Full (can order tests) |
| **LabTechnician** | ❌ No access | ✅ Full | ✅ Full | ✅ Read only |
| **Admin** | ✅ Full | ✅ Full | ✅ Full | ✅ Full |

### Authentication Endpoints

#### 1. Register a New User

```bash
POST /Auth/register
Content-Type: application/json

{
  "email": "doctor@hospital.com",
  "password": "SecurePassword123!",
  "role": "Doctor",
  "fullName": "Dr. Jane Smith"
}

# Response (201 Created):
{
  "userId": "abc123",
  "email": "doctor@hospital.com",
  "role": "Doctor",
  "message": "User registered successfully"
}
```

**Available Roles**: `Doctor`, `LabTechnician`, `Admin`

**Password Requirements**:
- Minimum 8 characters
- Stored as BCrypt hash (never plain text)

#### 2. Login and Get JWT Token

```bash
POST /Auth/login
Content-Type: application/json

{
  "email": "doctor@hospital.com",
  "password": "SecurePassword123!"
}

# Response (200 OK):
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhYmMxMjMiLCJlbWFpbCI6ImRvY3RvckBob3NwaXRhbC5jb20iLCJyb2xlIjoiRG9jdG9yIiwianRpIjoiZGVmNDU2IiwiaWF0IjoiMTczOTUzMDAwMCIsImZoaXJVc2VyIjoiUHJhY3RpdGlvbmVyL2FiYzEyMyIsInNjb3BlIjoidXNlci8qLnJlYWQgdXNlci9QYXRpZW50LndyaXRlIHVzZXIvT2JzZXJ2YXRpb24ucmVhZCB1c2VyL0RpYWdub3N0aWNSZXBvcnQucmVhZCIsImV4cCI6MTczOTUzMzYwMCwiaXNzIjoiTGFiRmxvd0FQSSIsImF1ZCI6IkxhYkZsb3dDbGllbnRzIn0.xxxxxxxxxxxxxxxxxxxxx",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "message": "Login successful"
}
```

**JWT Claims** (automatically included):
- `sub`: User ID
- `email`: User email
- `role`: User role (for authorization)
- `fhirUser`: FHIR Practitioner reference (e.g., "Practitioner/abc123")
- `scope`: SMART on FHIR compatible scopes (future-ready)
- `exp`: Expiration timestamp (60 minutes)
- `iss`: Issuer ("LabFlowAPI")
- `aud`: Audience ("LabFlowClients")

#### 3. Get Current User Info

```bash
GET /Auth/me
Authorization: Bearer {your-token-here}

# Response (200 OK):
{
  "userId": "abc123",
  "email": "doctor@hospital.com",
  "role": "Doctor",
  "fullName": "Dr. Jane Smith"
}
```

### Using JWT Tokens with FHIR Endpoints

**All FHIR resource endpoints require the JWT token in the Authorization header:**

```bash
# Example: Get a patient (requires Doctor or Admin role)
GET /Patient/123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# Example: Create an observation (any authenticated user)
POST /Observation
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/fhir+json

{
  "resourceType": "Observation",
  "status": "final",
  "code": {
    "coding": [{
      "system": "http://loinc.org",
      "code": "2339-0",
      "display": "Glucose [Mass/volume] in Blood"
    }]
  },
  "subject": { "reference": "Patient/123" },
  "valueQuantity": {
    "value": 95,
    "unit": "mg/dL",
    "system": "http://unitsofmeasure.org",
    "code": "mg/dL"
  }
}
```

### Error Responses

**401 Unauthorized** (missing or invalid token):
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

**403 Forbidden** (insufficient permissions):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

### Testing with curl

**Complete authentication flow:**

```bash
# 1. Register a doctor
curl -X POST https://localhost:7xxx/Auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "doctor@hospital.com",
    "password": "SecurePass123!",
    "role": "Doctor",
    "fullName": "Dr. Jane Smith"
  }'

# 2. Login and get token
TOKEN=$(curl -X POST https://localhost:7xxx/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "doctor@hospital.com",
    "password": "SecurePass123!"
  }' | jq -r '.token')

# 3. Create a patient (requires token)
curl -X POST https://localhost:7xxx/Patient \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/fhir+json" \
  -d '{
    "resourceType": "Patient",
    "name": [{
      "family": "García",
      "given": ["Juan", "Carlos"]
    }],
    "gender": "male",
    "birthDate": "1985-03-15"
  }'

# 4. Search patients (requires token)
curl -X GET "https://localhost:7xxx/Patient?name=García" \
  -H "Authorization: Bearer $TOKEN"
```

### Swagger UI Authentication

1. **Open Swagger UI** at `https://localhost:7xxx/`
2. **Click "Authorize" button** (lock icon in top right)
3. **Enter your JWT token** (with or without "Bearer " prefix)
4. **Click "Authorize"**
5. **All requests will now include the Authorization header automatically**

### Security Configuration

**appsettings.json** (development - DO NOT commit secrets to git):
```json
{
  "JwtSettings": {
    "SecretKey": "ThisIsADevelopmentSecretKeyWithAtLeast32CharactersForHS256Algorithm",
    "Issuer": "LabFlowAPI",
    "Audience": "LabFlowClients",
    "ExpirationMinutes": 60,
    "AuthProvider": "JWT"
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173",
      "https://localhost:7000"
    ]
  }
}
```

**Production:** Use environment variables or Azure Key Vault for secrets:
```bash
export JwtSettings__SecretKey="your-production-secret-key-minimum-32-characters-random"
export JwtSettings__Issuer="https://yourapi.com"
export JwtSettings__Audience="https://yourapi.com/clients"
```

### FHIR Compliance

JWT authentication is **documented in the CapabilityStatement** (GET `/metadata`):

```json
{
  "security": {
    "cors": true,
    "service": [{
      "coding": [{
        "system": "http://terminology.hl7.org/CodeSystem/restful-security-service",
        "code": "OAuth",
        "display": "OAuth"
      }],
      "text": "JWT Bearer Token Authentication"
    }],
    "description": "JWT Bearer token authentication required..."
  }
}
```

This follows **FHIR R4 security best practices** and is compatible with future migration to **SMART on FHIR** (OAuth 2.0).

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

# Update patient
PUT /Patient/{id}
Content-Type: application/json
Body: { "resourceType": "Patient", ... }

# Delete patient (soft delete)
DELETE /Patient/{id}

# Search patients
GET /Patient?name=García
GET /Patient?identifier=12345678
GET /Patient?birthdate=1985-03-15
GET /Patient?gender=male
GET /Patient?name=Smith&gender=male  # Combined search

# Pagination (FHIR R4 standard)
GET /Patient?name=García&_count=10&_offset=0   # First page (10 results)
GET /Patient?name=García&_count=10&_offset=10  # Second page
# Bundle.Link contains self, next, previous URLs for navigation
```

**Observation Resource (Laboratory Results):**
```bash
# Create observation
POST /Observation
Content-Type: application/json
Body: { "resourceType": "Observation", "subject": { "reference": "Patient/123" }, ... }

# Read observation by ID
GET /Observation/{id}

# Update observation
PUT /Observation/{id}
Content-Type: application/json
Body: { "resourceType": "Observation", ... }

# Delete observation (soft delete)
DELETE /Observation/{id}

# Search observations
GET /Observation?patient=Patient/123          # All observations for a patient
GET /Observation?patient=123                  # Also accepts just the ID
GET /Observation?code=2339-0                  # By LOINC code (Glucose)
GET /Observation?category=laboratory          # By category
GET /Observation?date=2025-10-13             # By observation date
GET /Observation?status=final                 # By status
GET /Observation?patient=123&code=2339-0      # Combined search

# Pagination
GET /Observation?patient=123&_count=20&_offset=0   # First 20 results
GET /Observation?patient=123&_count=20&_offset=20  # Next 20 results
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

# Update diagnostic report
PUT /DiagnosticReport/{id}
Content-Type: application/json
Body: { "resourceType": "DiagnosticReport", ... }

# Delete diagnostic report (soft delete)
DELETE /DiagnosticReport/{id}

# Search diagnostic reports
GET /DiagnosticReport?patient=Patient/123     # All reports for a patient
GET /DiagnosticReport?patient=123             # Also accepts just the ID
GET /DiagnosticReport?code=58410-2            # By LOINC panel code (CBC)
GET /DiagnosticReport?category=LAB            # By category (LAB, RAD, PATH)
GET /DiagnosticReport?date=2025-10-13         # By effective date (study performed)
GET /DiagnosticReport?issued=2025-10-13       # By issued date (report published)
GET /DiagnosticReport?status=final            # By status
GET /DiagnosticReport?patient=123&code=58410-2  # Combined search

# Pagination
GET /DiagnosticReport?patient=123&_count=15&_offset=0  # First 15 results
```

**ServiceRequest Resource (Laboratory Orders):**
```bash
# Create service request (laboratory test order)
POST /ServiceRequest
Content-Type: application/json
Body: {
  "resourceType": "ServiceRequest",
  "status": "active",
  "intent": "order",
  "code": {
    "coding": [{
      "system": "http://loinc.org",
      "code": "2339-0",
      "display": "Glucose [Mass/volume] in Blood"
    }]
  },
  "subject": { "reference": "Patient/123" },
  ...
}

# Read service request by ID
GET /ServiceRequest/{id}

# Update service request
PUT /ServiceRequest/{id}
Content-Type: application/json
Body: { "resourceType": "ServiceRequest", ... }

# Delete service request (soft delete)
DELETE /ServiceRequest/{id}

# Search service requests
GET /ServiceRequest?patient=Patient/123       # All orders for a patient
GET /ServiceRequest?patient=123               # Also accepts just the ID
GET /ServiceRequest?code=2339-0               # By LOINC code (Glucose test)
GET /ServiceRequest?status=active             # By status
GET /ServiceRequest?intent=order              # By intent (order, plan, etc.)
GET /ServiceRequest?category=108252007        # By category (SNOMED CT)
GET /ServiceRequest?authored=2025-10-13       # By authored date
GET /ServiceRequest?requester=Practitioner/456  # By requester
GET /ServiceRequest?performer=Organization/789  # By performer
GET /ServiceRequest?patient=123&status=active   # Combined search

# Pagination
GET /ServiceRequest?patient=123&_count=25&_offset=0  # First 25 results
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
- **132 focused unit tests** covering Patient, Observation, DiagnosticReport, ServiceRequest, Authentication, and Pagination
- **Patient** (25 tests):
  - GetPatient (2): Valid ID, Not Found scenarios
  - CreatePatient (3): Valid resource, Invalid content-type, FHIR validation
  - UpdatePatient (2): Valid update, Not Found scenarios
  - DeletePatient (2): Valid delete (soft), Not Found scenarios
  - SearchPatients (8): Individual parameters (name, identifier, birthdate, gender), combined filters, empty results, invalid inputs
  - Pagination (8): Default pagination, custom count, offset, count validation, negative offset, filter preservation, empty pages
- **Observation** (26 tests):
  - GetObservation (2): Valid ID, Not Found scenarios
  - CreateObservation (5): Valid resource, Invalid content-type, Missing status/code, Non-existent patient reference
  - UpdateObservation (2): Valid update, Not Found scenarios
  - DeleteObservation (2): Valid delete (soft), Not Found scenarios
  - SearchObservations (9): Individual parameters (patient, code, category, date, status), patient reference format handling, combined filters, empty results, invalid date
  - Pagination (6): Default pagination, custom count, offset, count validation, negative offset, filter preservation
- **DiagnosticReport** (29 tests):
  - GetDiagnosticReport (2): Valid ID, Not Found scenarios
  - CreateDiagnosticReport (7): Valid report, Invalid content-type, Missing status/code, Non-existent patient, Non-existent observation, Invalid reference type
  - UpdateDiagnosticReport (2): Valid update, Not Found scenarios
  - DeleteDiagnosticReport (2): Valid delete (soft), Not Found scenarios
  - SearchDiagnosticReports (10): Individual parameters (patient, code, category, date, issued, status), combined filters, empty results, invalid date/issued
  - Pagination (6): Default pagination, custom count, offset, count validation, negative offset, filter preservation
- **ServiceRequest** (26 tests):
  - GetServiceRequest (2): Valid ID, Not Found scenarios
  - CreateServiceRequest (5): Valid request, Invalid content-type, Missing status/intent, Non-existent patient reference
  - UpdateServiceRequest (2): Valid update, Not Found scenarios
  - DeleteServiceRequest (2): Valid delete (soft), Not Found scenarios
  - SearchServiceRequests (10): Individual parameters (patient, code, status, intent, category, authored), patient reference format handling, combined filters, empty results, invalid authored date
  - Pagination (6): Default pagination, custom count, offset, count validation, negative offset, filter preservation
- **AuthService** (15 tests):
  - Password hashing (3): Hash generation, verification success/failure
  - JWT generation (4): Token structure, claims validation, expiration, signature
  - Token validation (8): Valid token, expired, invalid signature, malformed, missing claims
- **AuthController** (11 tests):
  - Register (4): Valid registration, duplicate email, invalid role, password requirements
  - Login (4): Valid login, wrong password, non-existent user, token structure
  - GetMe (3): Valid token, invalid token, missing token
- **Test isolation**: Each test uses unique in-memory database
- **Framework**: xUnit + FluentAssertions + EF Core InMemory

All tests passing ✅ (132/132)

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

### ServiceRequestEntity Fields

**Searchable Fields** (indexed for performance):
- `PatientId` → FHIR search: `patient` or `subject`
- `Code` → FHIR search: `code` (LOINC test codes, e.g., "2339-0" for Glucose)
- `Status` → FHIR search: `status` (draft, active, on-hold, revoked, completed, etc.)
- `Intent` → FHIR search: `intent` (proposal, plan, order, etc.)
- `Category` → FHIR search: `category` (service category codes)
- `Priority` → FHIR search: `priority` (routine, urgent, asap, stat)
- `AuthoredOn` → FHIR search: `authored` (when order was created)
- `RequesterId` → FHIR search: `requester` (who ordered the test)
- `PerformerId` → FHIR search: `performer` (who will perform the test)
- `OccurrenceDateTime` → FHIR search: `occurrence` (when test should occur)
- `LastUpdated` → FHIR search: `_lastUpdated`

**Compound Index**:
- `PatientId + AuthoredOn` - Optimized for "get patient's recent lab orders"

**Metadata**: Same as Patient (VersionId, IsDeleted, CreatedAt, LastUpdated)

---

## 🎯 Roadmap

### Phase 1: Core Resources (Week 1) - COMPLETED ✅
- [x] **Patient** (Full CRUD + search + 17 tests) ✅
- [x] **Observation** (Full CRUD + search + patient validation + 20 tests) ✅

### Phase 2: FHIR Compliance & Automation (Week 2) - COMPLETED ✅
- [x] **CapabilityStatement** (GET /metadata) - FHIR standard server documentation ✅
- [x] **CI/CD Pipeline** (GitHub Actions) - Automated build & test ✅

### Phase 3: Grouped Reports & References (Week 3) - COMPLETED ✅
- [x] **DiagnosticReport** (Full CRUD + search + patient/observation validation + 23 tests) ✅

### Phase 4: Laboratory Order Workflow (Week 4) - COMPLETED ✅
- [x] **ServiceRequest** (Full CRUD + search + patient validation + 20 tests) ✅
- [x] Complete laboratory workflow: Order (ServiceRequest) → Result (Observation) → Report (DiagnosticReport)
- [x] **Full CRUD operations** for all 4 FHIR resources (CREATE, READ, UPDATE, DELETE, SEARCH)

### Phase 5: Security & Authentication (Week 5) - COMPLETED ✅
- [x] **JWT Bearer Token Authentication** ✅
  - User registration and login endpoints
  - BCrypt password hashing (work factor 11)
  - HS256 JWT signing with 60-minute expiration
  - Role-based authorization (Doctor, LabTechnician, Admin)
  - HTTPS enforcement + HSTS
  - CORS restricted to specific origins
  - Comprehensive audit logging (WHO, WHAT, WHEN)
  - CapabilityStatement security documentation updated
  - Swagger UI with Bearer token support
- [x] **FHIR Pagination** ✅
  - Offset-based pagination with `_count` and `_offset` parameters
  - FHIR R4 compliant Bundle.Link navigation (self, next, previous)
  - Total count preserved in Bundle.Total
  - Query parameter preservation across pages
  - Default 20 results per page, max 100
  - 26 comprehensive pagination tests

### Phase 6: Advanced Features & Testing (Next)
- [ ] **Integration tests** (TestServer end-to-end validation OR TestContainers with PostgreSQL)
- [ ] Advanced FHIR search (_include, _revinclude, _sort, _lastUpdated)
- [ ] PostgreSQL migration (from SQLite to production-ready database)
- [ ] Azure deployment (App Service + Azure SQL/PostgreSQL)

### Phase 6: Enhanced Validation (Future)
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

## 📋 IEC 62304 Documentation

**LabFlow FHIR API is documented according to IEC 62304** (Medical Device Software Lifecycle Standard) to demonstrate awareness of regulatory compliance for medical device software development.

### Software Safety Classification

**Class B (Medium Risk)**: Software manages Protected Health Information (PHI) but does not directly control medical devices or life-support systems.

### Documentation Structure

```
IEC62304/
├── 1_SoftwareRequirements/
│   └── SoftwareRequirementsSpecification.md    # 53 requirements (FHIR, Security, Data Integrity)
├── 2_SoftwareArchitecture/
│   └── SoftwareArchitectureDesign.md           # 26 architecture components + C4 diagrams
├── 3_RiskManagement/
│   └── RiskManagementFile.md                   # 12 risks analyzed (FMEA approach)
├── 4_Testing/
│   └── TestingReport.md                        # 132 tests, 100% requirements verified
└── 5_Traceability/
    └── RequirementsTraceabilityMatrix.md       # Complete Requirements → Tests traceability
```

### Key Documentation

| Document | Description | Highlights |
|----------|-------------|------------|
| **Requirements (SRS)** | 53 software requirements across 11 categories | 100% traced to architecture, tests, and risk controls |
| **Architecture Design** | 26 architecture components with justification | C4 diagrams, design patterns, ADRs (Architecture Decision Records) |
| **Risk Management** | 12 identified risks with controls | Defense-in-depth security, all residual risks acceptable |
| **Testing Report** | 132 unit tests, 100% pass rate | 89% automated verification, 11% manual configuration review |
| **Traceability Matrix** | Complete forward/backward traceability | Requirements → Architecture → Implementation → Tests → Risks |

### IEC 62304 Compliance Status

✅ **Section 5.1**: Software unit verification completed (132 tests)
✅ **Section 5.2**: Unit testing conducted (xUnit + FluentAssertions)
⚠️ **Section 5.3**: Integration testing (planned for Phase 6)
✅ **Section 5.4**: System testing documented
✅ **Section 5.7**: Software release ready (development/demonstration)
✅ **Section 7**: Risk management process complete (ISO 14971 aligned)
✅ **Section 8**: Configuration management (Git + semantic versioning)
✅ **Section 9**: Problem resolution (GitHub Issues + tracking)

### Highlights

**Security Architecture** (Defense-in-Depth):
- Layer 1: HTTPS + TLS 1.2+ (network security)
- Layer 2: JWT authentication (HS256, 60min expiration)
- Layer 3: Role-based authorization (Doctor, LabTechnician, Admin)
- Layer 4: FHIR validation (Firely SDK)
- Layer 5: Data protection (BCrypt, soft delete, version tracking)
- Layer 6: Audit logging (Serilog structured logs)

**Data Integrity Controls**:
- Soft delete (audit trail, regulatory compliance)
- Version tracking (optimistic concurrency)
- Reference validation (prevents orphaned data)
- FHIR validation (ensures interoperability)

**Risk Analysis** (Simplified FMEA):
- 12 risks identified and analyzed
- All risks have control measures implemented
- 0 critical or high residual risks
- All residual risks accepted for Class B software

**Traceability**:
- 53 requirements → 26 architecture components
- 26 architecture components → Implementation files
- 53 requirements → 132 verification tests
- 12 risks → 18 control requirements

### Portfolio Value

This IEC 62304 documentation demonstrates:
- ✅ Understanding of medical device regulations
- ✅ Software lifecycle process knowledge
- ✅ Risk-based thinking in healthcare software
- ✅ Professional documentation practices
- ✅ Traceability and verification rigor
- ✅ Readiness for FDA/CE submission processes

### Intended Use

**Development/Portfolio**: Current documentation is appropriate for demonstration and portfolio purposes.

**Production Deployment**: Would require:
- Integration testing (TestContainers + PostgreSQL)
- Performance testing and validation
- Security audit and penetration testing
- High availability infrastructure
- Backup/recovery procedures
- Incident response plan
- User training materials
- Post-market surveillance plan

---

## 📖 Resources

- [FHIR R4 Specification](http://hl7.org/fhir/R4/)
- [Firely SDK Documentation](https://docs.fire.ly/projects/Firely-NET-SDK/)
- [IEC 62304 Standard](https://www.iso.org/standard/38421.html) - Medical Device Software Lifecycle
- [ISO 14971](https://www.iso.org/standard/72704.html) - Risk Management for Medical Devices
- [Testing Strategy](./TESTING_STRATEGY.md) - Pragmatic testing approach and roadmap
- [Project Planning](./labflow-fhir-readme.md)
- [Learning Path](./fhir_learning_path.md)

---

## 👨‍💻 Developer

**Background**: 6 years Field Service Engineer (pharma/medical devices) + 2 years .NET development
**Goal**: Demonstrate FHIR + IEC 62304 awareness + production-ready skills for Medical Device Software Engineer roles

---

## 📄 License

MIT License - See LICENSE file for details
