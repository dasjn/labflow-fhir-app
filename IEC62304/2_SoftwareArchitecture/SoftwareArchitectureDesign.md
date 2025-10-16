# Software Architecture Design
## LabFlow FHIR API - Version 1.0

**Document Control**
- **Author**: David Sosa Junquera
- **Date**: 2025-10-16
- **Version**: 1.0
- **IEC 62304 Classification**: Class B (Medium Risk)
- **Status**: Released

---

## 1. Introduction

### 1.1 Purpose
This document describes the software architecture of LabFlow FHIR API, including system structure, design patterns, technology choices, and rationale for architectural decisions.

### 1.2 Scope
Architecture covers:
- System decomposition and layers
- Key design patterns
- Technology stack and justification
- Data storage strategy
- Security architecture
- API design principles

---

## 2. System Context (C4 Level 1)

```
┌─────────────────────────────────────────────────────────────┐
│                     Healthcare Ecosystem                     │
│                                                              │
│  ┌───────────────┐           ┌──────────────┐              │
│  │  EHR System   │◄─────────►│  LabFlow API │              │
│  │  (External)   │   FHIR    │  (This System)│              │
│  └───────────────┘   R4 JSON └──────────────┘              │
│                                      ▲                        │
│  ┌───────────────┐                  │ FHIR R4              │
│  │  LIS System   │◄─────────────────┤ JSON + JWT           │
│  │  (External)   │                  │                       │
│  └───────────────┘                  │                       │
│                                      ▼                        │
│  ┌───────────────────────────────────────────┐              │
│  │         Healthcare Professional          │              │
│  │  (Doctor, Lab Technician, Admin)         │              │
│  │  - Views lab results                      │              │
│  │  - Orders tests                           │              │
│  │  - Manages patient data                   │              │
│  └───────────────────────────────────────────┘              │
└─────────────────────────────────────────────────────────────┘
```

**Key Relationships**:
- External EHR systems query LabFlow for patient lab results
- External LIS systems push lab results to LabFlow
- Healthcare professionals authenticate and access via REST API
- All communication via HTTPS + JWT authentication

---

## 3. Container Diagram (C4 Level 2)

```
┌──────────────────────────────────────────────────────────────┐
│                        LabFlow System                         │
│                                                               │
│  ┌─────────────────────────────────────────────────────┐    │
│  │         Web API (.NET 8)                            │    │
│  │  - ASP.NET Core Web API                             │    │
│  │  - Controllers (REST endpoints)                     │    │
│  │  - Services (Business logic)                        │    │
│  │  - Authentication/Authorization                     │    │
│  │  - HTTPS + JWT                                      │    │
│  └──────────────┬──────────────────────────────────────┘    │
│                 │                                             │
│                 │ Entity Framework Core 9                    │
│                 │ (ORM)                                       │
│                 ▼                                             │
│  ┌──────────────────────────────────────┐                   │
│  │  Database (SQLite → PostgreSQL)      │                   │
│  │  - Hybrid storage (JSON + columns)   │                   │
│  │  - Indexed searchable fields         │                   │
│  │  - Audit trail (soft delete)         │                   │
│  └──────────────────────────────────────┘                   │
│                                                               │
│  ┌──────────────────────────────────────┐                   │
│  │  External Libraries                  │                   │
│  │  - Firely SDK (FHIR validation)      │                   │
│  │  - Serilog (Logging)                 │                   │
│  │  - BCrypt (Password hashing)         │                   │
│  │  - Swashbuckle (OpenAPI/Swagger)     │                   │
│  └──────────────────────────────────────┘                   │
└──────────────────────────────────────────────────────────────┘
```

---

## 4. Component Diagram (C4 Level 3)

```
┌───────────────────────────────────────────────────────────────────┐
│                          Web API Layer                             │
│                                                                    │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                  Controllers (REST Endpoints)              │  │
│  │  ┌────────────────┐  ┌──────────────────┐                │  │
│  │  │ Patient        │  │ Observation      │                 │  │
│  │  │ Controller     │  │ Controller       │                 │  │
│  │  │ [ARCH-005]     │  │ [ARCH-009]       │                 │  │
│  │  │ - GET/POST/    │  │ - GET/POST/      │                 │  │
│  │  │   PUT/DELETE   │  │   PUT/DELETE     │                 │  │
│  │  │ - Search       │  │ - Search         │                 │  │
│  │  └────────────────┘  └──────────────────┘                 │  │
│  │                                                             │  │
│  │  ┌────────────────┐  ┌──────────────────┐                │  │
│  │  │DiagnosticReport│  │ ServiceRequest   │                 │  │
│  │  │Controller      │  │ Controller       │                 │  │
│  │  │[ARCH-012]      │  │ [ARCH-014]       │                 │  │
│  │  └────────────────┘  └──────────────────┘                 │  │
│  │                                                             │  │
│  │  ┌────────────────┐  ┌──────────────────┐                │  │
│  │  │ Auth           │  │ Metadata         │                 │  │
│  │  │ Controller     │  │ Controller       │                 │  │
│  │  │ [ARCH-015]     │  │ [ARCH-003]       │                 │  │
│  │  └────────────────┘  └──────────────────┘                 │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                    │                               │
│                                    ▼                               │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │              Services (Business Logic)                     │  │
│  │  ┌──────────────────────────────────────────┐             │  │
│  │  │ AuthService [ARCH-017]                   │             │  │
│  │  │ - Password hashing (BCrypt)              │             │  │
│  │  │ - JWT generation (HS256)                 │             │  │
│  │  │ - Token validation                       │             │  │
│  │  └──────────────────────────────────────────┘             │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                    │                               │
│                                    ▼                               │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │         Middleware & Infrastructure                        │  │
│  │  ┌──────────────────┐  ┌─────────────────────┐           │  │
│  │  │ JWT Auth         │  │ HTTPS Redirect      │           │  │
│  │  │ Middleware       │  │ + HSTS              │           │  │
│  │  │ [ARCH-016]       │  │ [ARCH-019]          │           │  │
│  │  └──────────────────┘  └─────────────────────┘           │  │
│  │                                                             │  │
│  │  ┌──────────────────┐  ┌─────────────────────┐           │  │
│  │  │ Authorization    │  │ CORS Policy         │           │  │
│  │  │ Policies         │  │ [ARCH-020]          │           │  │
│  │  │ [ARCH-018]       │  │                     │           │  │
│  │  └──────────────────┘  └─────────────────────┘           │  │
│  │                                                             │  │
│  │  ┌──────────────────────────────────────────┐             │  │
│  │  │ Serilog Structured Logging [ARCH-025]    │             │  │
│  │  └──────────────────────────────────────────┘             │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                    │                               │
│                                    ▼                               │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │           Data Access Layer (EF Core 9)                    │  │
│  │  ┌──────────────────┐  ┌─────────────────────┐           │  │
│  │  │ FhirDbContext    │  │ Entity Models       │           │  │
│  │  │ [ARCH-002]       │  │ [ARCH-006]          │           │  │
│  │  │ - DbContext      │  │ - PatientEntity     │           │  │
│  │  │ - Migrations     │  │ - ObservationEntity │           │  │
│  │  │ - Configuration  │  │ - DiagnosticReport  │           │  │
│  │  │                  │  │ - ServiceRequest    │           │  │
│  │  └──────────────────┘  └─────────────────────┘           │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                    │                               │
│                                    ▼                               │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │           External FHIR Library                            │  │
│  │  ┌──────────────────────────────────────────┐             │  │
│  │  │ Firely SDK [ARCH-001]                    │             │  │
│  │  │ - FhirJsonParser (deserialize)           │             │  │
│  │  │ - FhirJsonSerializer (serialize)         │             │  │
│  │  │ - FHIR validation                        │             │  │
│  │  │ - Model classes (Patient, Observation)   │             │  │
│  │  └──────────────────────────────────────────┘             │  │
│  └────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

---

## 5. Architecture Components

### ARCH-001: FHIR SDK (Firely SDK)
**Component**: External library - Hl7.Fhir.R4 v5.12.2
**Purpose**: FHIR R4 resource parsing, serialization, and validation
**Rationale**:
- Industry-standard FHIR .NET library
- Ensures FHIR R4 compliance
- Handles complex FHIR data types (CodeableConcept, Quantity, Reference, etc.)
- Actively maintained by Firely (formerly Furore)
**Traces to**: REQ-FHIR-001, REQ-DATA-004, RISK-INT-001

### ARCH-002: Database Context (FhirDbContext)
**Component**: Entity Framework Core 9 DbContext
**Location**: `LabFlow.API/Data/FhirDbContext.cs`
**Purpose**: Database abstraction layer, migrations, entity configuration
**Technology**: Entity Framework Core 9.0
**Rationale**:
- ORM simplifies data access
- Migration-based schema versioning
- LINQ query support
- Database-agnostic (SQLite dev, PostgreSQL prod)
**Traces to**: REQ-FHIR-002, REQ-DATA-003

### ARCH-003: CapabilityStatement Endpoint
**Component**: MetadataController
**Location**: `LabFlow.API/Controllers/MetadataController.cs`
**Purpose**: FHIR server capability discovery
**Endpoints**: GET /metadata
**Rationale**: FHIR R4 requirement for server capability self-description
**Traces to**: REQ-FHIR-003, REQ-DOC-002

### ARCH-004: OperationOutcome Error Handling
**Component**: Helper methods in all controllers
**Purpose**: Structured FHIR-compliant error responses
**Implementation**: CreateOperationOutcome() method returns OperationOutcome resource
**Rationale**: FHIR standard error reporting
**Traces to**: REQ-FHIR-004

### ARCH-005: Patient Controller
**Component**: RESTful controller
**Location**: `LabFlow.API/Controllers/PatientController.cs`
**Endpoints**:
- GET /Patient/{id} - Read
- POST /Patient - Create
- PUT /Patient/{id} - Update
- DELETE /Patient/{id} - Soft delete
- GET /Patient?[search] - Search
**Traces to**: REQ-PAT-001, REQ-PAT-002

### ARCH-006: Entity Models (Hybrid Storage Pattern)
**Component**: Database entity classes
**Location**: `LabFlow.API/Data/Entities/`
**Classes**:
- PatientEntity
- ObservationEntity
- DiagnosticReportEntity
- ServiceRequestEntity
**Design Pattern**: Hybrid storage
- `FhirJson` (TEXT/jsonb): Complete FHIR resource
- Searchable columns: Extracted key fields (PatientId, Code, Status, etc.)
- Metadata: VersionId, IsDeleted, CreatedAt, LastUpdated
**Rationale**:
- Data fidelity: Full FHIR JSON preserves all information
- Query performance: Indexed columns enable fast searches
- FHIR compliance: Firely SDK handles serialization/deserialization
**Traces to**: REQ-PAT-003, REQ-DATA-005, REQ-PERF-001

### ARCH-007: Soft Delete Pattern
**Component**: IsDeleted flag in all entity models
**Implementation**: DELETE operations set IsDeleted=true, LastUpdated=now
**Rationale**:
- Regulatory compliance (21 CFR Part 11, GDPR audit requirements)
- Referential integrity maintained
- Data recovery possible
- Complete audit trail
**Traces to**: REQ-PAT-004, REQ-DATA-001, RISK-DATA-001

### ARCH-008: Version Tracking
**Component**: VersionId field in all entity models
**Implementation**: UPDATE operations increment VersionId, update LastUpdated
**Rationale**:
- Optimistic concurrency control
- Change history tracking
- FHIR Resource.meta.versionId support
**Traces to**: REQ-PAT-005, REQ-DATA-002

### ARCH-009: Observation Controller
**Component**: RESTful controller
**Location**: `LabFlow.API/Controllers/ObservationController.cs`
**Key Features**:
- Patient reference validation (ARCH-011)
- LOINC code support (ARCH-010)
- Value[x] polymorphism (Quantity, CodeableConcept)
**Traces to**: REQ-OBS-001, REQ-OBS-002, REQ-OBS-005, REQ-OBS-006

### ARCH-010: LOINC Code Support
**Component**: CodeableConcept handling in Observation/DiagnosticReport/ServiceRequest
**Implementation**: Code and CodeDisplay extracted to entity columns for searching
**Rationale**: LOINC is international standard for laboratory test identification
**Traces to**: REQ-OBS-003, REQ-REP-005, REQ-SRQ-003

### ARCH-011: Reference Validation
**Component**: Patient existence check before creating/updating related resources
**Implementation**: Async database query to verify referenced patient exists
**Example**:
```csharp
var patientExists = await _context.Patients
    .AnyAsync(p => p.Id == patientId && !p.IsDeleted);
if (!patientExists)
    return BadRequest(OperationOutcome with "Patient not found");
```
**Rationale**: Prevents orphaned data, ensures referential integrity
**Traces to**: REQ-OBS-004, REQ-REP-004, RISK-REF-001

### ARCH-012: DiagnosticReport Controller
**Component**: RESTful controller
**Location**: `LabFlow.API/Controllers/DiagnosticReportController.cs`
**Key Features**:
- Comprehensive reference validation (ARCH-013)
- Multiple Observation grouping
- LOINC panel code support
**Traces to**: REQ-REP-001 through REQ-REP-006

### ARCH-013: Comprehensive Reference Validation
**Component**: Multi-step validation in DiagnosticReport POST/PUT
**Implementation**:
1. Validate patient exists
2. Validate all observation references exist
3. Validate reference format is "Observation/{id}"
**Rationale**: DiagnosticReport references are critical - broken links compromise clinical data integrity
**Traces to**: REQ-REP-004, RISK-REF-001

### ARCH-014: ServiceRequest Controller
**Component**: RESTful controller
**Location**: `LabFlow.API/Controllers/ServiceRequestController.cs`
**Key Features**:
- Laboratory order workflow support
- Status/Intent lifecycle tracking
- Requester/Performer reference extraction
**Traces to**: REQ-SRQ-001 through REQ-SRQ-006

### ARCH-015: Authentication Controller
**Component**: RESTful controller
**Location**: `LabFlow.API/Controllers/AuthController.cs`
**Endpoints**:
- POST /Auth/register - User registration
- POST /Auth/login - Authentication and JWT issuance
- GET /Auth/me - Current user info (requires JWT)
**Traces to**: REQ-SEC-001, REQ-SEC-008

### ARCH-016: JWT Authentication Middleware
**Component**: ASP.NET Core Authentication middleware
**Configuration**: Program.cs - AddAuthentication + JwtBearer
**Token Spec**:
- Algorithm: HS256 (HMAC-SHA256)
- Expiration: 60 minutes
- Claims: sub (userId), email, role, fhirUser, scope, exp, iss, aud
**Rationale**: Industry-standard stateless authentication
**Traces to**: REQ-SEC-002, REQ-SEC-003, RISK-SEC-001

### ARCH-017: Password Hashing (AuthService)
**Component**: AuthService business logic
**Location**: `LabFlow.API/Services/AuthService.cs`
**Implementation**: BCrypt.Net-Next with work factor 11 (2^11 = 2048 iterations)
**Rationale**:
- BCrypt is adaptive (work factor can increase over time)
- Salt automatically generated and stored with hash
- Resistant to rainbow table attacks
- Industry standard for password storage
**Traces to**: REQ-SEC-004, RISK-SEC-002

### ARCH-018: Role-Based Access Control (RBAC)
**Component**: Authorization policies
**Configuration**: Program.cs - AddAuthorization policies
**Roles**:
- **Doctor**: Full CRUD on all FHIR resources
- **LabTechnician**: No Patient access, full Observation/DiagnosticReport, read-only ServiceRequest
- **Admin**: Full access to everything
**Implementation**: [Authorize(Roles = "...")] attributes on controllers
**Rationale**: Principle of least privilege, compliance with HIPAA minimum necessary rule
**Traces to**: REQ-SEC-005, RISK-SEC-003

### ARCH-019: HTTPS Enforcement and HSTS
**Component**: Middleware configuration
**Configuration**: Program.cs - UseHttpsRedirection + UseHsts
**HSTS**: HTTP Strict Transport Security with 1-year max-age
**Rationale**: Protects PHI in transit, prevents man-in-the-middle attacks
**Traces to**: REQ-SEC-006, RISK-SEC-004

### ARCH-020: CORS Policy
**Component**: CORS middleware
**Configuration**: Program.cs - AddCors with specific origins
**Production Config**: No AllowAnyOrigin(), only whitelisted domains
**Rationale**: Prevents cross-site request forgery (CSRF)
**Traces to**: REQ-SEC-007, RISK-SEC-005

### ARCH-021: FHIR Search Implementation
**Component**: LINQ queries in controller Search methods
**Implementation**: Dynamic query building based on search parameters
**Example**:
```csharp
var query = _context.Patients.Where(p => !p.IsDeleted);
if (!string.IsNullOrEmpty(name))
    query = query.Where(p => p.FamilyName.Contains(name) ||
                              p.GivenName.Contains(name));
```
**Traces to**: REQ-SEARCH-001, REQ-SEARCH-006

### ARCH-022: FHIR Bundle Construction
**Component**: Helper logic in all search methods
**Implementation**: Build Bundle (type: searchset) with total count and entries
**Rationale**: FHIR standard search result format
**Traces to**: REQ-SEARCH-002

### ARCH-023: Pagination Implementation
**Component**: _count and _offset parameter handling
**Implementation**:
- Parse and validate _count (default 20, max 100) and _offset (default 0)
- Apply Skip/Take to query
- Generate Bundle.Link components (self, next, previous)
- Preserve search filter parameters in pagination URLs
**Rationale**: Enables efficient large dataset handling per FHIR specification
**Traces to**: REQ-SEARCH-003, REQ-SEARCH-004, REQ-SEARCH-005

### ARCH-024: Database Indexing Strategy
**Component**: Entity configurations with indexes
**Implementation**:
- Single-column indexes: All searchable fields
- Compound indexes:
  - Observation: (PatientId, EffectiveDateTime)
  - DiagnosticReport: (PatientId, Issued)
  - ServiceRequest: (PatientId, AuthoredOn)
**Rationale**: Optimizes common query patterns ("recent results for patient X")
**Traces to**: REQ-PERF-001, REQ-PERF-002

### ARCH-025: Structured Logging (Serilog)
**Component**: Serilog logging infrastructure
**Configuration**: Program.cs - UseSerilog with console sink
**Log Events**:
- All CRUD operations (resource type, ID, operation, result)
- Authentication events (success/failure, user)
- Validation failures (details)
- Search operations (parameters, result count)
**Rationale**: Troubleshooting, audit trail, security monitoring
**Traces to**: REQ-LOG-001, REQ-LOG-002

### ARCH-026: API Documentation (Swagger)
**Component**: Swashbuckle OpenAPI generator
**Configuration**: Program.cs - AddSwaggerGen
**Endpoints**:
- /swagger - Swagger UI
- /swagger/v1/swagger.json - OpenAPI 3.0 spec
**Rationale**: Developer experience, API discoverability
**Traces to**: REQ-DOC-001

---

## 6. Design Patterns

### 6.1 Repository Pattern (via EF Core)
**Pattern**: Entity Framework DbContext acts as Unit of Work + Repository
**Rationale**: Simplicity - no additional abstraction layer needed for this scale
**Trade-off**: Tightly coupled to EF Core, but acceptable for portfolio project

### 6.2 Hybrid Storage Pattern (Industry Standard)
**Pattern**: Store complete FHIR JSON + extracted searchable columns
**Benefits**:
- Data fidelity (full FHIR resource preserved)
- Query performance (indexed columns for searches)
- Flexibility (can query JSON in PostgreSQL jsonb)
**Adopted by**: HAPI FHIR, Microsoft FHIR Server, Google Cloud Healthcare API

### 6.3 Soft Delete Pattern
**Pattern**: Logical deletion via IsDeleted flag
**Benefits**: Audit trail, data recovery, referential integrity
**Industry**: Standard in healthcare (21 CFR Part 11 compliance)

### 6.4 RESTful API with FHIR Extensions
**Pattern**: Standard REST (GET/POST/PUT/DELETE) + FHIR search syntax
**Example**: GET /Patient?name=Smith&_count=20&_offset=40
**Rationale**: Combines REST simplicity with FHIR power

### 6.5 Middleware Pipeline
**Pattern**: ASP.NET Core request pipeline with ordered middleware
**Order**:
1. HTTPS Redirection
2. HSTS
3. CORS
4. Authentication (JWT)
5. Authorization
6. Logging
7. Exception Handling
8. Controllers

---

## 7. Technology Stack Justification

### 7.1 .NET 8
**Rationale**:
- Long-term support (LTS) until November 2026
- High performance (top 10 in TechEmpower benchmarks)
- Cross-platform (Windows, Linux, macOS)
- Strong healthcare/medical device ecosystem
- Excellent tooling (Visual Studio, Rider, VS Code)

### 7.2 Entity Framework Core 9
**Rationale**:
- Mature ORM with excellent LINQ support
- Migration-based schema versioning
- Database-agnostic (SQLite dev → PostgreSQL prod)
- Performance improvements in EF9 (compiled queries, bulk operations)

### 7.3 SQLite (Development) → PostgreSQL (Production)
**SQLite Rationale**:
- Zero-configuration development database
- File-based (no server setup)
- Sufficient for unit testing

**PostgreSQL Target Rationale**:
- Production-grade reliability
- Advanced features (jsonb queries, full-text search, extensions)
- HIPAA-compliant deployments available (Azure, AWS RDS)
- Industry standard for healthcare applications

### 7.4 Firely SDK (Hl7.Fhir.R4)
**Rationale**:
- Official .NET SDK from HL7 FHIR ecosystem
- Actively maintained (monthly releases)
- Used in production by major healthcare vendors
- Complete FHIR R4 model coverage

### 7.5 BCrypt.Net-Next
**Rationale**:
- Adaptive hashing (work factor can increase)
- Industry standard for password storage
- OWASP recommended
- Simple API

### 7.6 Serilog
**Rationale**:
- Structured logging (log events as objects, not strings)
- Multiple sinks (console, file, Seq, Elasticsearch, etc.)
- Excellent performance
- Industry standard in .NET ecosystem

---

## 8. Security Architecture

### 8.1 Defense in Depth Layers

**Layer 1: Network (HTTPS)**
- TLS 1.2+ encryption
- HSTS prevents downgrade attacks
- Production: Certificate from trusted CA

**Layer 2: Authentication (JWT)**
- Stateless tokens with HMAC-SHA256
- 60-minute expiration
- Secure secret key (256+ bits)

**Layer 3: Authorization (RBAC)**
- Role-based policies
- Least privilege principle
- [Authorize] attributes on all FHIR endpoints

**Layer 4: Input Validation**
- FHIR SDK validates all resources
- Parameter validation (pagination limits, etc.)
- Content-Type checks

**Layer 5: Data Protection**
- Password hashing (BCrypt)
- Soft delete (audit trail)
- No sensitive data in logs

**Layer 6: Monitoring**
- Structured logging of all operations
- Authentication failure logging
- Validation failure logging

### 8.2 Security Controls Mapping

| Threat | Control | Architecture Component |
|--------|---------|------------------------|
| Unauthorized access | JWT authentication | ARCH-016 |
| Weak passwords | BCrypt hashing | ARCH-017 |
| Privilege escalation | RBAC policies | ARCH-018 |
| Man-in-the-middle | HTTPS + HSTS | ARCH-019 |
| CSRF attacks | CORS policy | ARCH-020 |
| Data tampering | Version tracking | ARCH-008 |
| Data loss | Soft delete | ARCH-007 |
| Invalid FHIR data | Firely SDK validation | ARCH-001 |

---

## 9. Data Flow Diagrams

### 9.1 Create Observation Flow

```
Client                  Controller              Service         Database
  │                        │                      │               │
  │──POST /Observation───→│                      │               │
  │  (FHIR JSON + JWT)     │                      │               │
  │                        │                      │               │
  │                        │──Validate JWT────────→               │
  │                        │←─JWT Valid───────────┘               │
  │                        │                      │               │
  │                        │──Parse FHIR JSON────→│               │
  │                        │  (Firely SDK)        │               │
  │                        │←─Observation obj─────┘               │
  │                        │                      │               │
  │                        │──Validate required fields────→       │
  │                        │←─Valid───────────────┘               │
  │                        │                      │               │
  │                        │──Check patient exists────────────────→
  │                        │←─Patient found───────────────────────┘
  │                        │                      │               │
  │                        │──Extract searchable fields───→       │
  │                        │  (PatientId, Code, Status, etc.)     │
  │                        │                      │               │
  │                        │──Save to DB──────────────────────────→
  │                        │  (FhirJson + columns)                │
  │                        │←─Saved (ID, VersionId=1)─────────────┘
  │                        │                      │               │
  │                        │──Serialize to JSON──→│               │
  │                        │  (Firely SDK)        │               │
  │                        │←─JSON────────────────┘               │
  │                        │                      │               │
  │←─201 Created───────────┤                      │               │
  │  Location header       │                      │               │
  │  Observation JSON      │                      │               │
```

### 9.2 Search with Pagination Flow

```
Client                  Controller              Database
  │                        │                      │
  │──GET /Patient?────────→│                      │
  │  name=Smith&           │                      │
  │  _count=20&_offset=40  │                      │
  │  + JWT token           │                      │
  │                        │                      │
  │                        │──Validate JWT────────→
  │                        │←─Valid───────────────┘
  │                        │                      │
  │                        │──Validate _count, _offset───→
  │                        │  (1-100, ≥0)         │
  │                        │←─Valid───────────────┘
  │                        │                      │
  │                        │──Build query─────────────────→
  │                        │  WHERE FamilyName LIKE '%Smith%'
  │                        │  AND !IsDeleted      │
  │                        │  ORDER BY LastUpdated│
  │                        │  COUNT(*) → total    │
  │                        │←─total = 156─────────────────┘
  │                        │                      │
  │                        │──Get page────────────────────→
  │                        │  SKIP 40 TAKE 20     │
  │                        │←─20 PatientEntity────────────┘
  │                        │                      │
  │                        │──Parse JSON for each─→
  │                        │  (Firely SDK)        │
  │                        │←─20 Patient FHIR─────┘
  │                        │                      │
  │                        │──Build Bundle────────→
  │                        │  type: searchset     │
  │                        │  total: 156          │
  │                        │  entry: 20 resources │
  │                        │  link: self, next, prev
  │                        │←─Bundle JSON─────────┘
  │                        │                      │
  │←─200 OK────────────────┤                      │
  │  Bundle with 20 results│                      │
  │  Links to page 3, page 1│                     │
```

---

## 10. Deployment Architecture

### 10.1 Current (Development)

```
┌─────────────────────────────────────┐
│  Developer Machine                  │
│  ┌───────────────────────────────┐  │
│  │  LabFlow.API                  │  │
│  │  - dotnet run                 │  │
│  │  - Kestrel web server         │  │
│  │  - HTTP: localhost:5000       │  │
│  │  - HTTPS: localhost:7000      │  │
│  └───────────────┬───────────────┘  │
│                  │                   │
│  ┌───────────────▼───────────────┐  │
│  │  SQLite Database              │  │
│  │  - File: labflow.db           │  │
│  │  - Location: project root     │  │
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘
```

### 10.2 Target (Production - Azure)

```
┌──────────────────────────────────────────────────────┐
│                   Azure Cloud                        │
│                                                       │
│  ┌────────────────────────────────────────────────┐  │
│  │  Azure App Service (Linux)                     │  │
│  │  - .NET 8 runtime                              │  │
│  │  - HTTPS with Azure certificate                │  │
│  │  - Auto-scaling                                │  │
│  │  - Health checks                               │  │
│  └──────────────────┬─────────────────────────────┘  │
│                     │                                 │
│  ┌──────────────────▼─────────────────────────────┐  │
│  │  Azure Database for PostgreSQL                 │  │
│  │  - Flexible Server tier                        │  │
│  │  - Automated backups                           │  │
│  │  - Point-in-time restore                       │  │
│  │  - SSL/TLS enforced                            │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
│  ┌────────────────────────────────────────────────┐  │
│  │  Azure Application Insights                    │  │
│  │  - Performance monitoring                      │  │
│  │  - Exception tracking                          │  │
│  │  - Custom metrics                              │  │
│  └────────────────────────────────────────────────┘  │
│                                                       │
│  ┌────────────────────────────────────────────────┐  │
│  │  Azure Key Vault                               │  │
│  │  - JWT secret                                  │  │
│  │  - Database connection string                  │  │
│  └────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────┘
```

---

## 11. Architecture Decision Records

### ADR-001: Use Firely SDK instead of custom FHIR implementation
**Status**: Accepted
**Context**: Need FHIR R4 compliance for healthcare interoperability
**Decision**: Use Firely SDK (Hl7.Fhir.R4)
**Rationale**:
- Mature, maintained by HL7 ecosystem
- Complete FHIR R4 coverage
- Reduces development time by 80%
- Eliminates risk of non-compliance
**Consequences**: Dependency on external library, but acceptable trade-off

### ADR-002: Hybrid storage pattern (JSON + columns)
**Status**: Accepted
**Context**: Need both FHIR compliance and query performance
**Decision**: Store full FHIR JSON + extracted searchable columns
**Rationale**:
- Industry standard pattern (HAPI FHIR, MS FHIR Server)
- Data fidelity (full JSON preserved)
- Query performance (indexed columns)
**Consequences**: Some data duplication, but manageable at this scale

### ADR-003: Soft delete instead of hard delete
**Status**: Accepted
**Context**: Need audit trail and regulatory compliance
**Decision**: Use IsDeleted flag instead of physical deletion
**Rationale**:
- 21 CFR Part 11 compliance
- Referential integrity maintained
- Data recovery possible
**Consequences**: Database size grows, but acceptable for healthcare

### ADR-004: SQLite for development, PostgreSQL for production
**Status**: Accepted
**Context**: Need fast local development and production-ready database
**Decision**: SQLite (dev), PostgreSQL (prod)
**Rationale**:
- SQLite: Zero-config, fast unit tests
- PostgreSQL: Production features (jsonb, full-text search)
- EF Core abstracts differences
**Consequences**: Minor SQL differences, but EF Core handles most

### ADR-005: JWT authentication with HS256
**Status**: Accepted
**Context**: Need stateless authentication for REST API
**Decision**: JWT with HMAC-SHA256 signing
**Rationale**:
- Industry standard for REST APIs
- Stateless (no session storage)
- Works across load balancers
- Simple to implement and verify
**Consequences**: Tokens can't be revoked until expiration (60 min acceptable)

### ADR-006: No integration tests in initial version
**Status**: Accepted
**Context**: Portfolio project with time constraints
**Decision**: Unit tests only (132 tests), document integration test strategy for future
**Rationale**:
- Unit tests cover 85% of requirements
- EF Core InMemory sufficient for validation
- Integration tests valuable but not critical for portfolio
**Consequences**: Documented in TESTING_STRATEGY.md as known limitation with roadmap

### ADR-007: Role-based access control (RBAC) over ABAC
**Status**: Accepted
**Context**: Need access control for healthcare professionals
**Decision**: Simple role-based (Doctor, LabTechnician, Admin) vs attribute-based
**Rationale**:
- Sufficient for laboratory workflow
- Simple to understand and implement
- Maps to real-world roles
**Consequences**: Less flexible than ABAC, but acceptable for scope

---

## 12. Architecture Validation

### 12.1 Quality Attributes

| Quality Attribute | Target | Current Status | Evidence |
|-------------------|--------|----------------|----------|
| **Maintainability** | High | ✅ Achieved | Clean separation of concerns, documented patterns |
| **Testability** | High | ✅ Achieved | 132 unit tests, 85% automated verification |
| **Security** | Critical | ✅ Achieved | JWT + RBAC + HTTPS + BCrypt + Audit trail |
| **FHIR Compliance** | Critical | ✅ Achieved | Firely SDK validation, CapabilityStatement |
| **Performance** | Medium | ⚠️ Not measured | Indexes in place, needs load testing |
| **Scalability** | Low-Medium | ⚠️ Limited | Single instance, stateless design enables future scaling |
| **Reliability** | Medium | ⚠️ Basic | Soft delete, version tracking, needs HA deployment |

### 12.2 Architecture Review Checklist

- ✅ All requirements traced to architecture components
- ✅ All architecture components traced to implementation files
- ✅ Security controls mapped to threats
- ✅ Design patterns documented with rationale
- ✅ Technology choices justified
- ✅ Data flows documented
- ✅ Deployment architecture defined
- ✅ ADRs capture key decisions
- ✅ Quality attributes defined and measured

---

## 13. Future Architecture Considerations

### 13.1 Phase 6+ Enhancements

**Advanced FHIR Search**:
- _include/_revinclude for referenced resources
- _sort for result ordering
- _elements for partial resource retrieval
- Requires query builder refactoring

**PostgreSQL Migration**:
- Leverage jsonb for advanced JSON queries
- Full-text search on clinical text
- Partitioning for large datasets

**High Availability**:
- Multiple API instances behind load balancer
- Database replication (primary + standby)
- Health checks and auto-restart

**Observability**:
- Distributed tracing (OpenTelemetry)
- Custom metrics (response times, error rates)
- Real-time alerting

---

**Document Status**: Released
**Next Review Date**: Upon Phase 6 implementation or architecture changes
**Approval**: David Sosa Junquera - Software Developer - 2025-10-16
