# Software Requirements Specification (SRS)
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
This document specifies the software requirements for LabFlow FHIR API, a laboratory results interoperability system compliant with HL7 FHIR R4 standard.

### 1.2 Scope
LabFlow FHIR API provides RESTful endpoints for managing laboratory workflows including:
- Patient demographics management
- Laboratory test orders (ServiceRequest)
- Laboratory test results (Observation)
- Diagnostic reports grouping multiple results (DiagnosticReport)
- Secure authentication and authorization

### 1.3 Intended Use
The software is intended for use in healthcare settings to facilitate electronic exchange of laboratory data between clinical systems, following FHIR R4 interoperability standards.

### 1.4 Risk Classification Rationale
**Class B (Medium Risk)** per IEC 62304:
- Software manages patient health information (PHI)
- Software does not directly control medical devices or life-support systems
- Data integrity errors could lead to incorrect clinical decisions
- Software includes security controls (authentication, authorization, audit trails)

---

## 2. FHIR Compliance Requirements

### REQ-FHIR-001: FHIR R4 Standard Compliance
**Description**: The system SHALL implement FHIR R4 resources according to HL7 FHIR R4 specification (http://hl7.org/fhir/R4/)
**Type**: Functional
**Priority**: Critical
**Rationale**: Ensures interoperability with other FHIR-compliant systems
**Verification**: Unit tests + FHIR resource validation via Firely SDK
**Trace to**: ARCH-001, TEST-FHIR-ALL

### REQ-FHIR-002: JSON Format Support
**Description**: The system SHALL support application/fhir+json and application/json content types for all FHIR operations
**Type**: Functional
**Priority**: High
**Verification**: HTTP Content-Type header validation in tests
**Trace to**: ARCH-002, TEST-FHIR-002

### REQ-FHIR-003: CapabilityStatement Endpoint
**Description**: The system SHALL provide a GET /metadata endpoint returning a FHIR CapabilityStatement resource documenting all supported operations
**Type**: Functional
**Priority**: High
**Rationale**: Required by FHIR standard for server capability discovery
**Verification**: MetadataController implementation and endpoint testing
**Trace to**: ARCH-003, MetadataController.cs

### REQ-FHIR-004: OperationOutcome for Errors
**Description**: The system SHALL return FHIR OperationOutcome resources for all error conditions
**Type**: Functional
**Priority**: High
**Rationale**: Provides structured error information per FHIR standard
**Verification**: Error handling tests across all controllers
**Trace to**: ARCH-004, TEST-ERROR-ALL

---

## 3. Patient Resource Requirements

### REQ-PAT-001: Patient CRUD Operations
**Description**: The system SHALL support CREATE, READ, UPDATE, DELETE operations for Patient resources
**Type**: Functional
**Priority**: Critical
**Verification**: PatientControllerTests (17 tests covering all CRUD operations)
**Trace to**: ARCH-005, PatientController.cs, TEST-PAT-001 to TEST-PAT-017

### REQ-PAT-002: Patient Search Parameters
**Description**: The system SHALL support searching patients by: name (partial match), identifier (exact), birthdate (exact), gender (exact)
**Type**: Functional
**Priority**: High
**Verification**: SearchPatients unit tests (8 search-specific tests)
**Trace to**: ARCH-005, TEST-PAT-009 to TEST-PAT-017

### REQ-PAT-003: Patient Data Persistence
**Description**: Patient data SHALL be persisted using hybrid storage (full FHIR JSON + indexed searchable fields)
**Type**: Functional
**Priority**: Critical
**Rationale**: Ensures data fidelity while maintaining search performance
**Verification**: Database schema validation, persistence tests
**Trace to**: ARCH-006, PatientEntity.cs

### REQ-PAT-004: Patient Soft Delete
**Description**: Patient deletion SHALL use soft delete (IsDeleted flag) to preserve audit trail
**Type**: Functional
**Priority**: High
**Rationale**: Regulatory compliance, data integrity, referential integrity
**Verification**: DELETE operation tests verify IsDeleted flag set
**Trace to**: ARCH-007, RISK-DATA-001, TEST-PAT-007 to TEST-PAT-008

### REQ-PAT-005: Patient Version Tracking
**Description**: Patient updates SHALL increment VersionId and update LastUpdated timestamp
**Type**: Functional
**Priority**: High
**Rationale**: Enables optimistic concurrency control and change tracking
**Verification**: UPDATE operation tests verify VersionId increment
**Trace to**: ARCH-008, TEST-PAT-005

---

## 4. Observation Resource Requirements

### REQ-OBS-001: Observation CRUD Operations
**Description**: The system SHALL support CREATE, READ, UPDATE, DELETE operations for Observation resources
**Type**: Functional
**Priority**: Critical
**Verification**: ObservationControllerTests (20 tests)
**Trace to**: ARCH-009, ObservationController.cs, TEST-OBS-001 to TEST-OBS-020

### REQ-OBS-002: Observation Required Fields
**Description**: Observation resources SHALL require: status, code, subject (Patient reference)
**Type**: Functional
**Priority**: Critical
**Rationale**: FHIR R4 Observation cardinality requirements
**Verification**: Validation tests for missing required fields
**Trace to**: ARCH-009, TEST-OBS-003, TEST-OBS-004

### REQ-OBS-003: LOINC Code Support
**Description**: The system SHALL support LOINC codes for laboratory test identification
**Type**: Functional
**Priority**: High
**Rationale**: LOINC is international standard for laboratory test codes
**Verification**: Tests with standard LOINC codes (2339-0, 718-7, etc.)
**Trace to**: ARCH-010, TEST-OBS-ALL

### REQ-OBS-004: Patient Reference Validation
**Description**: The system SHALL validate that referenced Patient exists before creating/updating Observation
**Type**: Functional
**Priority**: Critical
**Rationale**: Prevents orphaned data, ensures referential integrity
**Verification**: Tests for non-existent patient references return 400 Bad Request
**Trace to**: ARCH-011, RISK-REF-001, TEST-OBS-005

### REQ-OBS-005: Value Types Support
**Description**: Observation value[x] SHALL support Quantity (numeric with units) and CodeableConcept (coded values)
**Type**: Functional
**Priority**: High
**Verification**: Tests with both value types
**Trace to**: ARCH-009, TEST-OBS-ALL

### REQ-OBS-006: Observation Search Parameters
**Description**: The system SHALL support searching observations by: patient, code, category, date, status
**Type**: Functional
**Priority**: High
**Verification**: Search tests covering all parameters
**Trace to**: ARCH-009, TEST-OBS-010 to TEST-OBS-020

---

## 5. DiagnosticReport Resource Requirements

### REQ-REP-001: DiagnosticReport CRUD Operations
**Description**: The system SHALL support CREATE, READ, UPDATE, DELETE operations for DiagnosticReport resources
**Type**: Functional
**Priority**: Critical
**Verification**: DiagnosticReportControllerTests (23 tests)
**Trace to**: ARCH-012, DiagnosticReportController.cs, TEST-REP-001 to TEST-REP-023

### REQ-REP-002: DiagnosticReport Required Fields
**Description**: DiagnosticReport resources SHALL require: status, code (panel type), subject (Patient reference)
**Type**: Functional
**Priority**: Critical
**Verification**: Validation tests for missing required fields
**Trace to**: ARCH-012, TEST-REP-004

### REQ-REP-003: Multiple Observation Grouping
**Description**: DiagnosticReport SHALL support grouping multiple Observation results via result references
**Type**: Functional
**Priority**: High
**Rationale**: Enables panel reporting (CBC, Lipid panel, etc.)
**Verification**: Tests with multiple result references
**Trace to**: ARCH-012, TEST-REP-002

### REQ-REP-004: Comprehensive Reference Validation
**Description**: The system SHALL validate:
- Referenced Patient exists
- All referenced Observations exist
- Result references are type "Observation" (not other resource types)
**Type**: Functional
**Priority**: Critical
**Rationale**: Prevents broken references, ensures data integrity
**Verification**: Tests for non-existent patient/observations, invalid reference types
**Trace to**: ARCH-013, RISK-REF-001, TEST-REP-005, TEST-REP-006, TEST-REP-007

### REQ-REP-005: LOINC Panel Code Support
**Description**: DiagnosticReport code SHALL support LOINC panel codes (e.g., 58410-2 for CBC)
**Type**: Functional
**Priority**: High
**Verification**: Tests with standard panel codes
**Trace to**: ARCH-012, TEST-REP-ALL

### REQ-REP-006: Clinical Conclusion Support
**Description**: DiagnosticReport SHALL support optional conclusion field for clinical interpretation
**Type**: Functional
**Priority**: Medium
**Verification**: Tests with conclusion text
**Trace to**: ARCH-012, TEST-REP-002

---

## 6. ServiceRequest Resource Requirements

### REQ-SRQ-001: ServiceRequest CRUD Operations
**Description**: The system SHALL support CREATE, READ, UPDATE, DELETE operations for ServiceRequest resources
**Type**: Functional
**Priority**: Critical
**Verification**: ServiceRequestControllerTests (20 tests)
**Trace to**: ARCH-014, ServiceRequestController.cs, TEST-SRQ-001 to TEST-SRQ-020

### REQ-SRQ-002: ServiceRequest Required Fields
**Description**: ServiceRequest resources SHALL require: status, intent, subject (Patient reference)
**Type**: Functional
**Priority**: Critical
**Verification**: Validation tests for missing status/intent
**Trace to**: ARCH-014, TEST-SRQ-004, TEST-SRQ-005

### REQ-SRQ-003: Laboratory Test Order Support
**Description**: ServiceRequest SHALL support LOINC codes for laboratory test orders
**Type**: Functional
**Priority**: High
**Rationale**: Enables structured test ordering
**Verification**: Tests with LOINC test codes (2339-0 for Glucose, etc.)
**Trace to**: ARCH-014, TEST-SRQ-ALL

### REQ-SRQ-004: Order Status Tracking
**Description**: ServiceRequest SHALL support status values: draft, active, on-hold, revoked, completed, entered-in-error, unknown
**Type**: Functional
**Priority**: High
**Rationale**: Tracks order lifecycle
**Verification**: Search by status tests
**Trace to**: ARCH-014, TEST-SRQ-014

### REQ-SRQ-005: Order Intent Classification
**Description**: ServiceRequest SHALL support intent values: proposal, plan, directive, order, original-order, reflex-order, filler-order, instance-order, option
**Type**: Functional
**Priority**: High
**Verification**: Tests with various intent values
**Trace to**: ARCH-014, TEST-SRQ-ALL

### REQ-SRQ-006: ServiceRequest Search Parameters
**Description**: The system SHALL support searching service requests by: patient, code, status, intent, category, authored, requester, performer
**Type**: Functional
**Priority**: High
**Verification**: Search tests covering all parameters (10 search tests)
**Trace to**: ARCH-014, TEST-SRQ-011 to TEST-SRQ-020

---

## 7. Security Requirements

### REQ-SEC-001: Authentication Required
**Description**: All FHIR resource endpoints (except GET /metadata) SHALL require valid JWT authentication
**Type**: Security
**Priority**: Critical
**Rationale**: Protects patient health information (PHI) from unauthorized access
**Verification**: [Authorize] attribute on all controllers, authorization tests
**Trace to**: ARCH-015, RISK-SEC-001, AuthController.cs

### REQ-SEC-002: JWT Token Generation
**Description**: The system SHALL generate JWT tokens using HS256 (HMAC-SHA256) algorithm with configurable secret
**Type**: Security
**Priority**: Critical
**Verification**: JWT generation tests, token structure validation
**Trace to**: ARCH-016, TEST-AUTH-005 to TEST-AUTH-008

### REQ-SEC-003: Token Expiration
**Description**: JWT tokens SHALL expire after 60 minutes
**Type**: Security
**Priority**: High
**Rationale**: Limits exposure window for compromised tokens
**Verification**: Token expiration claim tests
**Trace to**: ARCH-016, TEST-AUTH-007

### REQ-SEC-004: Password Hashing
**Description**: User passwords SHALL be hashed using BCrypt with work factor 11 (2^11 iterations)
**Type**: Security
**Priority**: Critical
**Rationale**: Protects credentials against rainbow table and brute-force attacks
**Verification**: Password hashing tests
**Trace to**: ARCH-017, RISK-SEC-002, TEST-AUTH-001 to TEST-AUTH-003

### REQ-SEC-005: Role-Based Access Control (RBAC)
**Description**: The system SHALL enforce role-based permissions:
- **Doctor**: Full access to Patient, Observation, DiagnosticReport, ServiceRequest
- **LabTechnician**: No Patient access, Full Observation/DiagnosticReport, Read-only ServiceRequest
- **Admin**: Full access to all resources
**Type**: Security
**Priority**: Critical
**Rationale**: Principle of least privilege, data access control
**Verification**: Authorization policy configuration, role-based tests
**Trace to**: ARCH-018, RISK-SEC-003

### REQ-SEC-006: HTTPS Enforcement
**Description**: The system SHALL enforce HTTPS in production and enable HTTP Strict Transport Security (HSTS)
**Type**: Security
**Priority**: Critical
**Rationale**: Protects data in transit, prevents man-in-the-middle attacks
**Verification**: Program.cs configuration, deployment validation
**Trace to**: ARCH-019, RISK-SEC-004

### REQ-SEC-007: CORS Configuration
**Description**: The system SHALL restrict CORS to specific origins (no AllowAnyOrigin in production)
**Type**: Security
**Priority**: High
**Rationale**: Prevents cross-site request forgery (CSRF) attacks
**Verification**: CORS configuration review
**Trace to**: ARCH-020, RISK-SEC-005

### REQ-SEC-008: Authentication Endpoints
**Description**: The system SHALL provide:
- POST /Auth/register - User registration with role
- POST /Auth/login - Authentication and JWT token issuance
- GET /Auth/me - Current user information retrieval
**Type**: Security
**Priority**: Critical
**Verification**: AuthControllerTests (11 tests)
**Trace to**: ARCH-015, TEST-AUTH-009 to TEST-AUTH-019

---

## 8. Data Integrity Requirements

### REQ-DATA-001: Soft Delete Audit Trail
**Description**: DELETE operations SHALL use soft delete (IsDeleted flag) rather than physical deletion
**Type**: Functional
**Priority**: Critical
**Rationale**: Regulatory compliance (21 CFR Part 11), audit trail, data recovery
**Verification**: All DELETE tests verify IsDeleted flag, data still in database
**Trace to**: ARCH-007, RISK-DATA-001, TEST-PAT-007, TEST-OBS-007, TEST-REP-007, TEST-SRQ-007

### REQ-DATA-002: Version Tracking
**Description**: All resources SHALL track version changes via VersionId field
**Type**: Functional
**Priority**: High
**Rationale**: Change tracking, optimistic concurrency control
**Verification**: UPDATE tests verify VersionId increments
**Trace to**: ARCH-008, TEST-PAT-005, TEST-OBS-005, TEST-REP-005, TEST-SRQ-005

### REQ-DATA-003: Timestamp Tracking
**Description**: All resources SHALL track CreatedAt (immutable) and LastUpdated (updated on changes) timestamps
**Type**: Functional
**Priority**: High
**Rationale**: Audit trail, temporal queries
**Verification**: Entity models include timestamps, tests verify population
**Trace to**: ARCH-006, All Entity classes

### REQ-DATA-004: FHIR Validation
**Description**: All incoming FHIR resources SHALL be validated using Firely SDK parser before persistence
**Type**: Functional
**Priority**: Critical
**Rationale**: Ensures FHIR compliance, prevents malformed data
**Verification**: Invalid FHIR tests return 400 Bad Request
**Trace to**: ARCH-001, RISK-INT-001, TEST-FHIR-003

### REQ-DATA-005: Hybrid Storage Pattern
**Description**: The system SHALL store:
- Complete FHIR JSON resource (data fidelity)
- Extracted searchable fields in database columns (query performance)
**Type**: Functional
**Priority**: Critical
**Rationale**: Balances FHIR compliance with search performance
**Verification**: Entity models contain both FhirJson and indexed fields
**Trace to**: ARCH-006, All Entity classes

---

## 9. Search and Query Requirements

### REQ-SEARCH-001: FHIR Search Parameters
**Description**: The system SHALL support FHIR standard search parameters for each resource type
**Type**: Functional
**Priority**: High
**Verification**: Comprehensive search tests for each resource (40+ search tests total)
**Trace to**: ARCH-021, All search tests

### REQ-SEARCH-002: Search Result Bundle
**Description**: Search operations SHALL return FHIR Bundle (type: searchset) with total count and entry list
**Type**: Functional
**Priority**: High
**Rationale**: FHIR standard search result format
**Verification**: All search tests verify Bundle structure
**Trace to**: ARCH-022, All search tests

### REQ-SEARCH-003: Pagination Support
**Description**: The system SHALL support pagination via _count (results per page, default 20, max 100) and _offset (skip count) parameters
**Type**: Functional
**Priority**: High
**Rationale**: Enables efficient large dataset handling
**Verification**: Pagination tests (26 tests)
**Trace to**: ARCH-023, TEST-PAG-001 to TEST-PAG-026

### REQ-SEARCH-004: Pagination Links
**Description**: Paginated search results SHALL include Bundle.Link components for navigation (self, next, previous)
**Type**: Functional
**Priority**: Medium
**Rationale**: FHIR standard pagination navigation
**Verification**: Tests verify self/next/previous links presence and correctness
**Trace to**: ARCH-023, TEST-PAG-ALL

### REQ-SEARCH-005: Query Parameter Preservation
**Description**: Pagination links SHALL preserve all search filter parameters
**Type**: Functional
**Priority**: Medium
**Rationale**: Maintains search context across pages
**Verification**: Tests verify filter parameters in pagination URLs
**Trace to**: ARCH-023, TEST-PAG-024, TEST-PAG-025, TEST-PAG-026

### REQ-SEARCH-006: Soft-Deleted Exclusion
**Description**: Search operations SHALL exclude soft-deleted resources (IsDeleted = true)
**Type**: Functional
**Priority**: Critical
**Rationale**: Deleted resources should not appear in search results
**Verification**: Search tests after soft delete verify resource not returned
**Trace to**: ARCH-007, All search tests

---

## 10. Performance Requirements

### REQ-PERF-001: Database Indexes
**Description**: The system SHALL use database indexes on searchable fields and compound indexes for common query patterns
**Type**: Non-Functional
**Priority**: High
**Rationale**: Ensures acceptable query performance at scale
**Verification**: Database migrations include index definitions
**Trace to**: ARCH-024, Migration files

### REQ-PERF-002: Compound Indexes
**Description**: The system SHALL use compound indexes:
- Observation: (PatientId, EffectiveDateTime) - "recent results for patient"
- DiagnosticReport: (PatientId, Issued) - "recent reports for patient"
- ServiceRequest: (PatientId, AuthoredOn) - "recent orders for patient"
**Type**: Non-Functional
**Priority**: High
**Rationale**: Optimizes most common query patterns
**Verification**: Database schema validation
**Trace to**: ARCH-024, Entity configurations

### REQ-PERF-003: Pagination Default Limit
**Description**: Search operations SHALL default to 20 results per page and enforce maximum of 100
**Type**: Non-Functional
**Priority**: Medium
**Rationale**: Prevents excessive data transfer, ensures reasonable response times
**Verification**: Pagination tests verify limits
**Trace to**: ARCH-023, TEST-PAG-002, TEST-PAG-003

---

## 11. Logging and Monitoring Requirements

### REQ-LOG-001: Structured Logging
**Description**: The system SHALL use structured logging (Serilog) for all operations
**Type**: Non-Functional
**Priority**: High
**Rationale**: Enables troubleshooting, audit trail, monitoring
**Verification**: Code review, log output validation
**Trace to**: ARCH-025, Program.cs

### REQ-LOG-002: Operation Logging
**Description**: The system SHALL log:
- All CRUD operations (resource type, ID, operation)
- Authentication events (login, registration, failures)
- Validation failures with details
- Search operations with parameters
**Type**: Non-Functional
**Priority**: High
**Rationale**: Audit trail, troubleshooting, security monitoring
**Verification**: Log statements in all controllers
**Trace to**: ARCH-025, All controllers

---

## 12. API Documentation Requirements

### REQ-DOC-001: OpenAPI/Swagger Specification
**Description**: The system SHALL provide OpenAPI 3.0 specification via Swagger UI
**Type**: Non-Functional
**Priority**: Medium
**Rationale**: API discoverability, developer experience
**Verification**: Swagger endpoint accessible, complete API documentation
**Trace to**: ARCH-026, Swashbuckle configuration

### REQ-DOC-002: CapabilityStatement Documentation
**Description**: The CapabilityStatement SHALL document:
- All supported resources and interactions (CRUD operations)
- All search parameters with types and descriptions
- Authentication and authorization requirements
- Pagination support
**Type**: Functional
**Priority**: High
**Rationale**: FHIR standard server capability discovery
**Verification**: GET /metadata returns complete CapabilityStatement
**Trace to**: ARCH-003, MetadataController.cs

---

## 13. Requirements Summary

| Category | Total Requirements | Critical | High | Medium | Low |
|----------|-------------------|----------|------|--------|-----|
| FHIR Compliance | 4 | 1 | 3 | 0 | 0 |
| Patient Resource | 5 | 2 | 3 | 0 | 0 |
| Observation Resource | 6 | 3 | 3 | 0 | 0 |
| DiagnosticReport Resource | 6 | 3 | 2 | 1 | 0 |
| ServiceRequest Resource | 6 | 2 | 4 | 0 | 0 |
| Security | 8 | 5 | 2 | 0 | 0 |
| Data Integrity | 5 | 3 | 2 | 0 | 0 |
| Search and Query | 6 | 1 | 2 | 3 | 0 |
| Performance | 3 | 0 | 2 | 1 | 0 |
| Logging | 2 | 0 | 2 | 0 | 0 |
| Documentation | 2 | 0 | 1 | 1 | 0 |
| **TOTAL** | **53** | **20** | **26** | **6** | **0** |

---

## 14. Requirement Verification Methods

| Method | Description | Requirements Count |
|--------|-------------|-------------------|
| Unit Tests | Automated tests with xUnit + FluentAssertions | 45 |
| Code Review | Manual inspection of implementation | 6 |
| Integration Tests | (Future) End-to-end testing with real database | 0 |
| Static Analysis | (Future) Code quality and security scanning | 2 |

**Current Test Coverage**: 132 unit tests covering 45 of 53 requirements (85% automated verification)

---

## 15. Requirements Acceptance Criteria

All requirements are considered satisfied when:
1. ✅ Implementation exists and is traceable to architecture component
2. ✅ Unit tests pass (132/132 passing as of 2025-10-16)
3. ✅ Code review confirms implementation matches requirement
4. ✅ Risk controls are in place where applicable
5. ✅ Documentation is complete and accurate

---

**Document Status**: Released
**Next Review Date**: Upon Phase 6 implementation or significant changes
**Approval**: David Sosa Junquera - Software Developer - 2025-10-16
