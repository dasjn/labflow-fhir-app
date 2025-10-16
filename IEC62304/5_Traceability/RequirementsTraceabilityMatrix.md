# Requirements Traceability Matrix
## LabFlow FHIR API - Version 1.0

**Document Control**
- **Author**: David Sosa Junquera
- **Date**: 2025-10-16
- **Version**: 1.0
- **Status**: Released

---

## 1. Purpose

This traceability matrix demonstrates complete linkage between:
- **Requirements** (SRS) → **Architecture** (Design) → **Implementation** (Code) → **Verification** (Tests) → **Risk Controls**

Ensures all requirements are implemented, tested, and risks are mitigated.

---

## 2. Traceability Matrix

### 2.1 FHIR Compliance Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-FHIR-001 | FHIR R4 compliance | ARCH-001 | Firely SDK v5.12.2 | All tests use Firely parser/serializer | RISK-INT-001 |
| REQ-FHIR-002 | JSON format support | ARCH-002 | `[Produces("application/fhir+json")]` attributes | Content-type validation tests | - |
| REQ-FHIR-003 | CapabilityStatement | ARCH-003 | MetadataController.cs:28-445 | GET /metadata endpoint | - |
| REQ-FHIR-004 | OperationOutcome errors | ARCH-004 | CreateOperationOutcome() methods | Error handling tests (40+ tests) | - |

### 2.2 Patient Resource Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-PAT-001 | Patient CRUD | ARCH-005 | PatientController.cs | TEST-PAT-001 to TEST-PAT-008 (8 tests) | - |
| REQ-PAT-002 | Patient search | ARCH-005 | PatientController.cs:SearchPatients() | TEST-PAT-009 to TEST-PAT-017 (9 tests) | - |
| REQ-PAT-003 | Hybrid storage | ARCH-006 | PatientEntity.cs | Entity model validation | - |
| REQ-PAT-004 | Soft delete | ARCH-007 | PatientController.cs:DeletePatient() | TEST-PAT-007, TEST-PAT-008 | RISK-DATA-001 |
| REQ-PAT-005 | Version tracking | ARCH-008 | PatientEntity.VersionId | TEST-PAT-005 (UPDATE test) | RISK-DATA-002 |

### 2.3 Observation Resource Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-OBS-001 | Observation CRUD | ARCH-009 | ObservationController.cs | TEST-OBS-001 to TEST-OBS-008 (8 tests) | - |
| REQ-OBS-002 | Required fields | ARCH-009 | Validation in CreateObservation() | TEST-OBS-003, TEST-OBS-004 | RISK-INT-001 |
| REQ-OBS-003 | LOINC codes | ARCH-010 | ObservationEntity.Code/CodeDisplay | Tests use LOINC codes (2339-0, 718-7) | - |
| REQ-OBS-004 | Patient validation | ARCH-011 | Reference check before save | TEST-OBS-005 (non-existent patient) | RISK-REF-001 |
| REQ-OBS-005 | Value types | ARCH-009 | Quantity & CodeableConcept support | Tests with numeric and coded values | - |
| REQ-OBS-006 | Search parameters | ARCH-009 | SearchObservations() | TEST-OBS-010 to TEST-OBS-020 (11 tests) | - |

### 2.4 DiagnosticReport Resource Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-REP-001 | DiagnosticReport CRUD | ARCH-012 | DiagnosticReportController.cs | TEST-REP-001 to TEST-REP-008 (8 tests) | - |
| REQ-REP-002 | Required fields | ARCH-012 | Validation in CreateDiagnosticReport() | TEST-REP-004 (missing code) | RISK-INT-001 |
| REQ-REP-003 | Multiple observations | ARCH-012 | DiagnosticReport.Result list | TEST-REP-002 (CBC panel with 3 obs) | - |
| REQ-REP-004 | Comprehensive validation | ARCH-013 | Multi-step reference checks | TEST-REP-005, 006, 007 | RISK-REF-001 |
| REQ-REP-005 | LOINC panel codes | ARCH-012 | DiagnosticReportEntity.Code | Tests use 58410-2 (CBC panel) | - |
| REQ-REP-006 | Clinical conclusion | ARCH-012 | DiagnosticReportEntity.Conclusion | TEST-REP-002 | - |

### 2.5 ServiceRequest Resource Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-SRQ-001 | ServiceRequest CRUD | ARCH-014 | ServiceRequestController.cs | TEST-SRQ-001 to TEST-SRQ-008 (8 tests) | - |
| REQ-SRQ-002 | Required fields | ARCH-014 | Validation in CreateServiceRequest() | TEST-SRQ-004, TEST-SRQ-005 | RISK-INT-001 |
| REQ-SRQ-003 | LOINC test codes | ARCH-014 | ServiceRequestEntity.Code | Tests use 2339-0 (Glucose) | - |
| REQ-SRQ-004 | Status tracking | ARCH-014 | ServiceRequestEntity.Status | TEST-SRQ-014 (search by status) | - |
| REQ-SRQ-005 | Intent classification | ARCH-014 | ServiceRequestEntity.Intent | Tests with various intents | - |
| REQ-SRQ-006 | Search parameters | ARCH-014 | SearchServiceRequests() | TEST-SRQ-011 to TEST-SRQ-020 (10 tests) | - |

### 2.6 Security Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-SEC-001 | Authentication required | ARCH-015, ARCH-016 | [Authorize] + JWT middleware | Authorization tests | RISK-SEC-001 |
| REQ-SEC-002 | JWT generation | ARCH-016 | AuthService.GenerateJwtToken() | TEST-AUTH-005 to TEST-AUTH-008 | RISK-SEC-001 |
| REQ-SEC-003 | Token expiration | ARCH-016 | Claims["exp"] = 60min | TEST-AUTH-007 | RISK-SEC-001 |
| REQ-SEC-004 | Password hashing | ARCH-017 | BCrypt.HashPassword(work factor 11) | TEST-AUTH-001 to TEST-AUTH-003 | RISK-SEC-002 |
| REQ-SEC-005 | RBAC | ARCH-018 | Authorization policies + [Authorize(Roles)] | Policy configuration review | RISK-SEC-003 |
| REQ-SEC-006 | HTTPS enforcement | ARCH-019 | UseHttpsRedirection + UseHsts | Program.cs configuration | RISK-SEC-004 |
| REQ-SEC-007 | CORS policy | ARCH-020 | AddCors with specific origins | Program.cs configuration | RISK-SEC-005 |
| REQ-SEC-008 | Auth endpoints | ARCH-015 | AuthController.cs | TEST-AUTH-009 to TEST-AUTH-019 (11 tests) | RISK-SEC-001 |

### 2.7 Data Integrity Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-DATA-001 | Soft delete | ARCH-007 | IsDeleted flag | All DELETE tests | RISK-DATA-001 |
| REQ-DATA-002 | Version tracking | ARCH-008 | VersionId + LastUpdated | All UPDATE tests | RISK-DATA-002 |
| REQ-DATA-003 | Timestamp tracking | ARCH-006 | CreatedAt, LastUpdated | Entity models | - |
| REQ-DATA-004 | FHIR validation | ARCH-001 | FhirJsonParser.Parse() | Invalid FHIR tests (400 responses) | RISK-INT-001 |
| REQ-DATA-005 | Hybrid storage | ARCH-006 | FhirJson + indexed columns | All entity models | - |

### 2.8 Search and Query Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-SEARCH-001 | FHIR search params | ARCH-021 | Dynamic LINQ queries | 40+ search tests | - |
| REQ-SEARCH-002 | Bundle result | ARCH-022 | Bundle construction | All search tests verify Bundle | - |
| REQ-SEARCH-003 | Pagination | ARCH-023 | _count, _offset parameters | TEST-PAG-001 to TEST-PAG-026 (26 tests) | - |
| REQ-SEARCH-004 | Pagination links | ARCH-023 | Bundle.Link components | Pagination tests verify links | - |
| REQ-SEARCH-005 | Query preservation | ARCH-023 | URL encoding in pagination links | TEST-PAG-024, 025, 026 | - |
| REQ-SEARCH-006 | Soft-delete exclusion | ARCH-007 | Where(!IsDeleted) | All search tests | - |

### 2.9 Performance Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-PERF-001 | Database indexes | ARCH-024 | Entity configurations | Migration files | RISK-PERF-001 |
| REQ-PERF-002 | Compound indexes | ARCH-024 | (PatientId, DateTime) indexes | Migration files | RISK-PERF-001 |
| REQ-PERF-003 | Pagination limits | ARCH-023 | Default 20, max 100 | TEST-PAG-002, TEST-PAG-003 | RISK-PERF-001 |

### 2.10 Logging Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-LOG-001 | Structured logging | ARCH-025 | Serilog | Program.cs configuration | - |
| REQ-LOG-002 | Operation logging | ARCH-025 | _logger.LogInformation() calls | Code review (all controllers) | - |

### 2.11 Documentation Requirements

| Req ID | Requirement | Architecture | Implementation | Verification | Risk Control |
|--------|-------------|--------------|----------------|--------------|--------------|
| REQ-DOC-001 | Swagger/OpenAPI | ARCH-026 | Swashbuckle | GET /swagger endpoint | - |
| REQ-DOC-002 | CapabilityStatement docs | ARCH-003 | MetadataController | GET /metadata completeness | - |

---

## 3. Test Coverage Summary

### 3.1 Test Distribution by Requirement Category

| Category | Total Requirements | Automated Tests | Manual Verification | Coverage |
|----------|-------------------|-----------------|---------------------|----------|
| FHIR Compliance | 4 | 4 | 0 | 100% |
| Patient Resource | 5 | 5 | 0 | 100% |
| Observation Resource | 6 | 6 | 0 | 100% |
| DiagnosticReport Resource | 6 | 6 | 0 | 100% |
| ServiceRequest Resource | 6 | 6 | 0 | 100% |
| Security | 8 | 6 | 2 | 100% |
| Data Integrity | 5 | 5 | 0 | 100% |
| Search and Query | 6 | 6 | 0 | 100% |
| Performance | 3 | 2 | 1 | 100% |
| Logging | 2 | 0 | 2 | 100% |
| Documentation | 2 | 1 | 1 | 100% |
| **TOTAL** | **53** | **47** | **6** | **100%** |

**Note**: Manual verification includes configuration reviews (HTTPS, CORS, logging, indexes) which are one-time checks rather than per-build automated tests.

### 3.2 Test Suite Breakdown

| Test Suite | Test Count | Requirements Covered | Pass Rate |
|------------|-----------|----------------------|-----------|
| PatientControllerTests | 17 | REQ-PAT-001 to REQ-PAT-005 | 17/17 (100%) |
| ObservationControllerTests | 20 | REQ-OBS-001 to REQ-OBS-006 | 20/20 (100%) |
| DiagnosticReportControllerTests | 23 | REQ-REP-001 to REQ-REP-006 | 23/23 (100%) |
| ServiceRequestControllerTests | 20 | REQ-SRQ-001 to REQ-SRQ-006 | 20/20 (100%) |
| AuthServiceTests | 15 | REQ-SEC-002, REQ-SEC-003, REQ-SEC-004 | 15/15 (100%) |
| AuthControllerTests | 11 | REQ-SEC-001, REQ-SEC-008 | 11/11 (100%) |
| Pagination Tests (all resources) | 26 | REQ-SEARCH-003, REQ-SEARCH-004, REQ-SEARCH-005 | 26/26 (100%) |
| **TOTAL** | **132** | **53** | **132/132 (100%)** |

---

## 4. Requirements Coverage Analysis

### 4.1 Requirements with Multiple Traces

Some requirements are traced through multiple components, demonstrating defense-in-depth:

**REQ-SEC-001 (Authentication Required)**:
- Architecture: ARCH-015 (AuthController), ARCH-016 (JWT Middleware)
- Implementation: [Authorize] attributes on 4 FHIR controllers
- Verification: TEST-AUTH-ALL (11 tests)
- Risk Control: RISK-SEC-001

**REQ-DATA-001 (Soft Delete)**:
- Architecture: ARCH-007 (Soft Delete Pattern)
- Implementation: IsDeleted flag in 4 entity models, 4 DELETE methods
- Verification: TEST-PAT-007/008, TEST-OBS-007/008, TEST-REP-007/008, TEST-SRQ-007/008 (8 tests)
- Risk Control: RISK-DATA-001

**REQ-DATA-004 (FHIR Validation)**:
- Architecture: ARCH-001 (Firely SDK)
- Implementation: FhirJsonParser.Parse() in all POST/PUT methods
- Verification: 16+ validation tests across all resources
- Risk Control: RISK-INT-001

### 4.2 Requirements Without Direct Test Coverage

The following requirements are verified through configuration review rather than automated tests:

1. **REQ-SEC-006 (HTTPS)**: Program.cs configuration review
2. **REQ-SEC-007 (CORS)**: Program.cs configuration review
3. **REQ-LOG-001 (Structured logging)**: Program.cs + Serilog configuration
4. **REQ-LOG-002 (Operation logging)**: Code review of _logger calls
5. **REQ-PERF-001/002 (Indexes)**: Migration file review
6. **REQ-DOC-001 (Swagger)**: Manual endpoint testing

**Rationale**: These are infrastructure/configuration concerns validated once during setup, not business logic requiring per-build testing.

---

## 5. Risk Control Verification

All risk controls are traced to requirements and verified:

| Risk ID | Risk | Control Requirements | Verification | Status |
|---------|------|----------------------|--------------|--------|
| RISK-SEC-001 | Unauthorized access | REQ-SEC-001, 003, 005, 006 | 11 auth tests + config review | ✅ Verified |
| RISK-SEC-002 | Compromised passwords | REQ-SEC-004 | 3 BCrypt tests | ✅ Verified |
| RISK-SEC-003 | Privilege escalation | REQ-SEC-005 | Policy config review | ✅ Verified |
| RISK-SEC-004 | MITM attack | REQ-SEC-006 | HTTPS config review | ✅ Verified |
| RISK-SEC-005 | CSRF | REQ-SEC-007 | CORS config review | ✅ Verified |
| RISK-DATA-001 | Accidental deletion | REQ-DATA-001 | 8 soft delete tests | ✅ Verified |
| RISK-DATA-002 | Concurrent updates | REQ-DATA-002 | 4 version tracking tests | ✅ Verified |
| RISK-DATA-003 | Database corruption | WAL, PostgreSQL | Config review | ✅ Verified |
| RISK-INT-001 | Invalid FHIR data | REQ-DATA-004, REQ-FHIR-001 | 16+ validation tests | ✅ Verified |
| RISK-REF-001 | Broken references | REQ-OBS-004, REQ-REP-004, REQ-DATA-001 | 6 reference validation tests | ✅ Verified |
| RISK-AVAIL-001 | System downtime | REQ-LOG-001, REQ-LOG-002 | Logging configured | ⚠️ Partial (dev) |
| RISK-PERF-001 | Slow queries | REQ-PERF-001, 002, 003 | Index review + pagination tests | ✅ Verified |

---

## 6. Forward Traceability (Requirements → Tests)

**Complete**: All 53 requirements have verification methods defined and executed.

## 7. Backward Traceability (Tests → Requirements)

**Complete**: All 132 tests trace back to specific requirements.

**Orphaned Tests**: 0 (no tests without requirement linkage)

---

## 8. Traceability Gaps and Mitigations

### 8.1 Identified Gaps

**None**. All requirements have:
- Architecture component assignment
- Implementation in codebase
- Verification method (automated test or manual review)
- Risk control mapping (where applicable)

### 8.2 Future Enhancements

When Phase 6 features are implemented:
1. New requirements will be added to SRS
2. Architecture components will be assigned
3. Implementation will be traced to files/methods
4. Tests will be written before considering requirement "complete"
5. This traceability matrix will be updated

---

## 9. Traceability Maintenance

### 9.1 Update Triggers

This matrix SHALL be updated when:
1. New requirements are added
2. Requirements are modified or removed
3. Architecture components change
4. Tests are added, modified, or removed
5. Risk analysis identifies new risks or controls

### 9.2 Review Frequency

- **Per Phase**: Before closing each development phase
- **Per Release**: Before any software release
- **Annually**: Comprehensive review even without changes

---

## 10. Traceability Tool

**Current**: Manual maintenance in Markdown
**Format**: Markdown tables (human-readable, version-controllable)
**Location**: IEC62304/5_Traceability/RequirementsTraceabilityMatrix.md

**Future Consideration**: Specialized traceability tools (Jama, Polarion, etc.) if project scales beyond portfolio scope.

---

**Document Status**: Released
**Next Review Date**: Upon Phase 6 implementation or requirement changes
**Approval**: David Sosa Junquera - Software Developer - 2025-10-16
