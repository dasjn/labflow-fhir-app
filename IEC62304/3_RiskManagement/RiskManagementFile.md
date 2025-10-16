# Software Risk Management File
## LabFlow FHIR API - Version 1.0

**Document Control**
- **Author**: David Sosa Junquera
- **Date**: 2025-10-16
- **Version**: 1.0
- **IEC 62304 Classification**: Class B (Medium Risk)
- **Status**: Released
- **Standards**: ISO 14971 (Risk Management for Medical Devices), IEC 62304 Section 7

---

## 1. Introduction

### 1.1 Purpose
This document identifies, analyzes, evaluates, and controls software risks for LabFlow FHIR API according to ISO 14971 and IEC 62304 requirements.

### 1.2 Scope
Risk analysis covers:
- Security risks (unauthorized access, data breaches)
- Data integrity risks (corruption, loss, inconsistency)
- Interoperability risks (FHIR non-compliance, broken references)
- Availability risks (system failures, performance degradation)

### 1.3 Risk Classification
**Software Safety Classification**: Class B (Medium Risk)

**Rationale**:
- Software handles Protected Health Information (PHI)
- Software does not directly control medical devices or life-support systems
- Data integrity errors could lead to incorrect clinical decisions
- Software includes multiple security and data integrity controls

---

## 2. Risk Management Process

### 2.1 Risk Analysis Method
**Simplified FMEA (Failure Mode and Effects Analysis)**:
- Identify potential failure modes
- Assess severity and probability
- Calculate risk level
- Define control measures
- Evaluate residual risk

### 2.2 Severity Classification

| Level | Description | Clinical Impact |
|-------|-------------|-----------------|
| **Critical** | Death or serious injury possible | Patient harm, wrong diagnosis/treatment |
| **High** | Significant clinical impact | Delayed care, incorrect data |
| **Medium** | Moderate impact | Minor errors, inconvenience |
| **Low** | Minimal impact | No clinical consequences |

### 2.3 Probability Classification

| Level | Description | Likelihood |
|-------|-------------|-----------|
| **High** | Occurs frequently | > 10% of uses |
| **Medium** | Occurs occasionally | 1-10% of uses |
| **Low** | Occurs rarely | 0.1-1% of uses |
| **Very Low** | Occurs very rarely | < 0.1% of uses |

### 2.4 Risk Level Matrix

|            | **Low Severity** | **Medium Severity** | **High Severity** | **Critical Severity** |
|------------|------------------|---------------------|-------------------|-----------------------|
| **High Prob** | Medium | High | High | Critical |
| **Med Prob** | Low | Medium | High | Critical |
| **Low Prob** | Low | Low | Medium | High |
| **Very Low** | Low | Low | Low | Medium |

---

## 3. Risk Analysis Table

### 3.1 Security Risks

#### RISK-SEC-001: Unauthorized Access to Patient Data
**Hazard**: Attacker gains unauthorized access to PHI
**Hazardous Situation**: Unauthorized user views/modifies patient records
**Harm**: HIPAA violation, patient privacy breach, legal liability
**Severity**: **High** (Privacy breach, potential identity theft)
**Probability Before Controls**: **High** (Common attack vector)
**Risk Level Before Controls**: **High**

**Control Measures**:
1. **REQ-SEC-001**: JWT authentication required for all FHIR endpoints (except /metadata)
   - Trace: ARCH-016 (JWT Middleware)
   - Verification: TEST-AUTH-ALL
2. **REQ-SEC-006**: HTTPS enforcement + HSTS
   - Trace: ARCH-019
   - Verification: Configuration review
3. **REQ-SEC-005**: Role-based access control (Doctor, LabTechnician, Admin)
   - Trace: ARCH-018
   - Verification: Authorization policy tests
4. **REQ-SEC-003**: JWT token expiration (60 minutes)
   - Trace: ARCH-016
   - Verification: TEST-AUTH-007

**Residual Risk**:
- **Probability After Controls**: **Low** (Multiple layers of defense)
- **Severity**: **High** (Unchanged)
- **Residual Risk Level**: **Medium** ✅ **Acceptable**

**Acceptance Rationale**: Defense-in-depth approach with industry-standard controls reduces probability to acceptable level. No further risk reduction measures required for Class B software.

---

#### RISK-SEC-002: Compromised Passwords
**Hazard**: Attacker obtains user passwords via database breach or brute-force
**Hazardous Situation**: Attacker authenticates as legitimate user
**Harm**: Unauthorized data access, data manipulation
**Severity**: **High**
**Probability Before Controls**: **Medium** (Database breaches occur)
**Risk Level Before Controls**: **High**

**Control Measures**:
1. **REQ-SEC-004**: BCrypt password hashing with work factor 11
   - Trace: ARCH-017 (AuthService)
   - Verification: TEST-AUTH-001 to TEST-AUTH-003
2. **Password requirements**: Minimum 8 characters (future: complexity rules)
3. **Rate limiting** (future): Prevent brute-force attacks

**Residual Risk**:
- **Probability After Controls**: **Low**
- **Severity**: **High**
- **Residual Risk Level**: **Medium** ✅ **Acceptable**

**Acceptance Rationale**: BCrypt is industry standard with adaptive work factor. Rainbow table attacks prevented by salting. Future enhancements (rate limiting, password complexity) can further reduce risk if needed.

---

#### RISK-SEC-003: Privilege Escalation
**Hazard**: User gains access to resources beyond their authorized role
**Hazardous Situation**: LabTechnician accesses/modifies Patient demographics
**Harm**: Unauthorized data modification, compliance violation
**Severity**: **Medium** (Limited clinical impact, compliance issue)
**Probability Before Controls**: **Medium**
**Risk Level Before Controls**: **Medium**

**Control Measures**:
1. **REQ-SEC-005**: Role-based access control policies
   - Doctor: Full access
   - LabTechnician: No Patient access, full Observation/DiagnosticReport
   - Admin: Full access
   - Trace: ARCH-018
   - Verification: Authorization policy configuration
2. **[Authorize]** attributes on all controllers
3. **JWT role claim** validated on every request

**Residual Risk**:
- **Probability After Controls**: **Low**
- **Severity**: **Medium**
- **Residual Risk Level**: **Low** ✅ **Acceptable**

**Acceptance Rationale**: ASP.NET Core authorization middleware is battle-tested. Clear role definitions match real-world workflows.

---

#### RISK-SEC-004: Man-in-the-Middle (MITM) Attack
**Hazard**: Attacker intercepts network traffic
**Hazardous Situation**: PHI transmitted over unencrypted connection
**Harm**: Data exposure, HIPAA violation
**Severity**: **High**
**Probability Before Controls**: **Medium** (Especially on public networks)
**Risk Level Before Controls**: **High**

**Control Measures**:
1. **REQ-SEC-006**: HTTPS enforcement (TLS 1.2+)
   - Trace: ARCH-019
   - Verification: Middleware configuration
2. **HSTS** (HTTP Strict Transport Security): Forces HTTPS, prevents downgrade
3. **Production certificate**: Trusted CA (future: Azure/Let's Encrypt)

**Residual Risk**:
- **Probability After Controls**: **Very Low**
- **Severity**: **High**
- **Residual Risk Level**: **Low** ✅ **Acceptable**

**Acceptance Rationale**: TLS is cryptographically secure. HSTS prevents accidental HTTP connections.

---

#### RISK-SEC-005: Cross-Site Request Forgery (CSRF)
**Hazard**: Malicious website tricks user's browser into making unauthorized requests
**Hazardous Situation**: Attacker creates/modifies FHIR resources via victim's session
**Harm**: Data corruption, unauthorized modifications
**Severity**: **Medium**
**Probability Before Controls**: **Low** (JWT in header, not cookie)
**Risk Level Before Controls**: **Low**

**Control Measures**:
1. **REQ-SEC-007**: CORS policy restricting origins
   - Trace: ARCH-020
   - Verification: CORS configuration
2. **JWT in Authorization header** (not cookie): Not automatically sent by browser
3. **No cookies used** for authentication

**Residual Risk**:
- **Probability After Controls**: **Very Low**
- **Severity**: **Medium**
- **Residual Risk Level**: **Low** ✅ **Acceptable**

**Acceptance Rationale**: JWT in header design inherently prevents CSRF. CORS provides additional defense layer.

---

### 3.2 Data Integrity Risks

#### RISK-DATA-001: Accidental Data Deletion
**Hazard**: User accidentally deletes critical patient data
**Hazardous Situation**: Patient record deleted, historical lab results lost
**Harm**: Loss of clinical history, incorrect treatment decisions
**Severity**: **Medium** (Can be recovered, but disruptive)
**Probability Before Controls**: **Low** (Requires explicit DELETE request)
**Risk Level Before Controls**: **Medium**

**Control Measures**:
1. **REQ-DATA-001**: Soft delete pattern (IsDeleted flag)
   - Trace: ARCH-007
   - Verification: TEST-PAT-007, TEST-OBS-007, TEST-REP-007, TEST-SRQ-007
2. **Audit trail**: CreatedAt, LastUpdated, IsDeleted timestamps
   - Trace: ARCH-008
   - Verification: Entity models
3. **Database backups** (future): Point-in-time recovery
4. **[Authorize]** on DELETE endpoints: Only authenticated users

**Residual Risk**:
- **Probability After Controls**: **Very Low**
- **Severity**: **Medium**
- **Residual Risk Level**: **Low** ✅ **Acceptable**

**Acceptance Rationale**: Soft delete is industry standard for healthcare. Data can be recovered by flipping IsDeleted flag. Physical database backups provide secondary recovery mechanism.

---

#### RISK-DATA-002: Concurrent Update Conflicts
**Hazard**: Two users update same resource simultaneously
**Hazardous Situation**: Last write wins, earlier changes lost
**Harm**: Data inconsistency, lost clinical information
**Severity**: **Medium**
**Probability Before Controls**: **Low** (Low concurrency expected)
**Risk Level Before Controls**: **Low**

**Control Measures**:
1. **REQ-DATA-002**: Version tracking (VersionId)
   - Trace: ARCH-008
   - Verification: TEST-PAT-005, TEST-OBS-005, TEST-REP-005, TEST-SRQ-005
2. **Future: Optimistic concurrency** via If-Match headers (FHIR standard)
3. **LastUpdated timestamp**: Tracks modification time

**Residual Risk**:
- **Probability After Controls**: **Low**
- **Severity**: **Medium**
- **Residual Risk Level**: **Low** ✅ **Acceptable**

**Acceptance Rationale**: VersionId provides foundation for optimistic locking. Low expected concurrency (laboratory workflows are typically sequential). Future If-Match implementation available if needed.

---

#### RISK-DATA-003: Database Corruption
**Hazard**: Database file corrupted due to disk failure, software bug, or crash
**Hazardous Situation**: Patient data unreadable or inconsistent
**Harm**: Complete data loss, system unavailable
**Severity**: **High** (Business continuity impact)
**Probability Before Controls**: **Low** (Modern filesystems/DBs have protections)
**Risk Level Before Controls**: **Medium**

**Control Measures**:
1. **SQLite Write-Ahead Logging (WAL)**: Crash-safe transactions
2. **PostgreSQL (production)**: ACID compliance, replication
3. **Automated backups** (future): Daily backups with retention
4. **Point-in-time recovery** (future): Azure Database for PostgreSQL feature

**Residual Risk**:
- **Probability After Controls**: **Very Low**
- **Severity**: **High**
- **Residual Risk Level**: **Low** ✅ **Acceptable**

**Acceptance Rationale**: SQLite WAL mode provides crash safety for development. PostgreSQL production deployment will include enterprise-grade backup/recovery.

---

### 3.3 Interoperability Risks

#### RISK-INT-001: Invalid FHIR Data Stored
**Hazard**: Malformed or non-compliant FHIR resources stored in database
**Hazardous Situation**: External system retrieves invalid FHIR resource
**Harm**: Integration failures, interoperability breakdown
**Severity**: **Medium** (System interoperability compromised)
**Probability Before Controls**: **Medium** (Complex FHIR spec)
**Risk Level Before Controls**: **Medium**

**Control Measures**:
1. **REQ-DATA-004**: Firely SDK validation on all incoming resources
   - Trace: ARCH-001
   - Verification: TEST-FHIR-003, all POST tests with invalid data
2. **REQ-FHIR-001**: FHIR R4 compliance via Firely SDK
3. **Unit tests**: 132 tests including validation scenarios
4. **OperationOutcome errors**: Structured error reporting

**Residual Risk**:
- **Probability After Controls**: **Low**
- **Severity**: **Medium**
- **Residual Risk Level**: **Low** ✅ **Acceptable**

**Acceptance Rationale**: Firely SDK is industry-standard validator used in production systems worldwide. Comprehensive unit tests cover edge cases.

---

#### RISK-REF-001: Broken References (Orphaned Data)
**Hazard**: Resource references non-existent patient/observation
**Hazardous Situation**: Observation references deleted patient
**Harm**: Incomplete clinical data, query failures
**Severity**: **Low** (Soft delete maintains references, queries handle gracefully)
**Probability Before Controls**: **Medium** (Common without validation)
**Risk Level Before Controls**: **Medium**

**Control Measures**:
1. **REQ-OBS-004**: Patient reference validation on create/update
   - Trace: ARCH-011
   - Verification: TEST-OBS-005
2. **REQ-REP-004**: Comprehensive reference validation (patient + all observations)
   - Trace: ARCH-013
   - Verification: TEST-REP-005, TEST-REP-006, TEST-REP-007
3. **REQ-DATA-001**: Soft delete preserves referential integrity
   - IsDeleted patients still in database, references valid
4. **Search queries**: Exclude soft-deleted resources automatically

**Residual Risk**:
- **Probability After Controls**: **Very Low**
- **Severity**: **Low**
- **Residual Risk Level**: **Very Low** ✅ **Acceptable**

**Acceptance Rationale**: Combination of reference validation + soft delete prevents orphaned data. References remain valid even if target is "deleted."

---

### 3.4 Availability Risks

#### RISK-AVAIL-001: System Downtime
**Hazard**: API becomes unavailable due to crash, deployment, or infrastructure failure
**Hazardous Situation**: Healthcare professionals cannot access lab results
**Harm**: Delayed care, clinician frustration
**Severity**: **Medium** (Not life-critical, workarounds exist)
**Probability Before Controls**: **Medium**
**Risk Level Before Controls**: **Medium**

**Control Measures**:
1. **Logging**: Serilog captures exceptions for troubleshooting
   - Trace: ARCH-025
   - Verification: REQ-LOG-001, REQ-LOG-002
2. **Health checks** (future): ASP.NET Core health check endpoints
3. **Auto-restart** (future): Azure App Service auto-restart on crash
4. **Load balancing** (future): Multiple instances for high availability

**Residual Risk**:
- **Probability After Controls**: **Low** (With future HA deployment)
- **Severity**: **Medium**
- **Residual Risk Level**: **Low** ✅ **Acceptable** (for development), ⚠️ **Requires monitoring** (for production)

**Acceptance Rationale**: Current single-instance deployment acceptable for portfolio/development. Production deployment will include high availability features.

---

#### RISK-PERF-001: Slow Query Performance
**Hazard**: Large datasets cause slow search responses
**Hazardous Situation**: Search queries take > 5 seconds
**Harm**: Poor user experience, timeouts
**Severity**: **Low** (Usability issue, not safety-critical)
**Probability Before Controls**: **Medium** (As data grows)
**Risk Level Before Controls**: **Low**

**Control Measures**:
1. **REQ-PERF-001**: Database indexes on searchable fields
   - Trace: ARCH-024
   - Verification: Migration files
2. **REQ-PERF-002**: Compound indexes for common patterns
   - (PatientId, EffectiveDateTime), (PatientId, Issued), (PatientId, AuthoredOn)
3. **REQ-SEARCH-003**: Pagination limits results (default 20, max 100)
   - Trace: ARCH-023
4. **Future: PostgreSQL jsonb**: Advanced JSON indexing

**Residual Risk**:
- **Probability After Controls**: **Low**
- **Severity**: **Low**
- **Residual Risk Level**: **Very Low** ✅ **Acceptable**

**Acceptance Rationale**: Proper indexing strategy in place. Pagination prevents unbounded result sets. PostgreSQL migration will provide additional optimization capabilities.

---

## 4. Risk Control Summary

### 4.1 Risk Control Traceability Matrix

| Risk ID | Risk Name | Severity | Prob (Before) | Risk (Before) | Control Requirements | Prob (After) | Risk (After) | Status |
|---------|-----------|----------|---------------|---------------|----------------------|--------------|--------------|--------|
| RISK-SEC-001 | Unauthorized access | High | High | High | REQ-SEC-001, 003, 005, 006 | Low | Medium | ✅ Acceptable |
| RISK-SEC-002 | Compromised passwords | High | Medium | High | REQ-SEC-004 | Low | Medium | ✅ Acceptable |
| RISK-SEC-003 | Privilege escalation | Medium | Medium | Medium | REQ-SEC-005 | Low | Low | ✅ Acceptable |
| RISK-SEC-004 | MITM attack | High | Medium | High | REQ-SEC-006 | Very Low | Low | ✅ Acceptable |
| RISK-SEC-005 | CSRF | Medium | Low | Low | REQ-SEC-007 | Very Low | Low | ✅ Acceptable |
| RISK-DATA-001 | Accidental deletion | Medium | Low | Medium | REQ-DATA-001 | Very Low | Low | ✅ Acceptable |
| RISK-DATA-002 | Concurrent updates | Medium | Low | Low | REQ-DATA-002 | Low | Low | ✅ Acceptable |
| RISK-DATA-003 | Database corruption | High | Low | Medium | WAL, PostgreSQL | Very Low | Low | ✅ Acceptable |
| RISK-INT-001 | Invalid FHIR data | Medium | Medium | Medium | REQ-DATA-004, REQ-FHIR-001 | Low | Low | ✅ Acceptable |
| RISK-REF-001 | Broken references | Low | Medium | Medium | REQ-OBS-004, REQ-REP-004, REQ-DATA-001 | Very Low | Very Low | ✅ Acceptable |
| RISK-AVAIL-001 | System downtime | Medium | Medium | Medium | Logging, future HA | Low | Low | ✅ Acceptable (dev) |
| RISK-PERF-001 | Slow queries | Low | Medium | Low | REQ-PERF-001, 002, REQ-SEARCH-003 | Low | Very Low | ✅ Acceptable |

### 4.2 Overall Risk Profile

**Total Risks Identified**: 12

**Risk Levels After Controls**:
- Critical: 0
- High: 0
- Medium: 2 (RISK-SEC-001, RISK-SEC-002)
- Low: 9
- Very Low: 1

**Acceptance**: All residual risks are **acceptable** for Class B software with current controls.

---

## 5. Risk Monitoring and Review

### 5.1 Ongoing Risk Monitoring

**Trigger Events for Risk Review**:
1. New feature implementation (Phase 6+)
2. Security vulnerability disclosure (dependency CVEs)
3. Incident or near-miss (authentication bypass, data loss)
4. Regulatory requirement changes (HIPAA updates, FDA guidance)
5. Production deployment readiness review

### 5.2 Risk Control Verification

| Control Requirement | Verification Method | Frequency | Status |
|---------------------|---------------------|-----------|--------|
| REQ-SEC-001 (JWT auth) | Unit tests | Every build | ✅ Passing (132/132) |
| REQ-SEC-004 (BCrypt) | Unit tests | Every build | ✅ Passing |
| REQ-SEC-006 (HTTPS) | Configuration review | Pre-deployment | ✅ Configured |
| REQ-DATA-001 (Soft delete) | Unit tests | Every build | ✅ Passing |
| REQ-DATA-004 (FHIR validation) | Unit tests | Every build | ✅ Passing |
| REQ-PERF-001 (Indexes) | Database schema review | Per migration | ✅ Verified |

### 5.3 Security Monitoring (Future - Production)

**Planned Monitoring**:
1. **Failed authentication attempts**: Alert on >5 failures/minute from single IP
2. **Unusual access patterns**: Alert on access to >100 patient records in 1 hour
3. **Data modification rate**: Alert on >50 DELETE operations in 1 hour
4. **System health**: Alert on >5% error rate or >3s response time

---

## 6. Residual Risk Acceptance

### 6.1 Acceptance Statement

All identified residual risks are **accepted** for the following reasons:

1. **Risk levels are appropriate for Class B software**: No critical or high residual risks
2. **Multiple layers of defense**: Defense-in-depth approach reduces single points of failure
3. **Industry-standard controls**: JWT, BCrypt, HTTPS, RBAC are proven technologies
4. **Comprehensive testing**: 132 unit tests verify control effectiveness
5. **Audit trail**: Soft delete + version tracking enable incident investigation
6. **Future mitigation path**: Clear roadmap for additional controls (rate limiting, HA, backups)

### 6.2 Acceptance Authority

**Accepted by**: David Sosa Junquera, Software Developer
**Date**: 2025-10-16
**Context**: Portfolio project demonstrating IEC 62304 compliance awareness
**Notes**:
- Current deployment: Development/demonstration environment
- Production deployment will require:
  - Comprehensive security audit
  - Penetration testing
  - Load testing and performance validation
  - High availability infrastructure
  - Backup/recovery testing
  - Incident response plan

---

## 7. Risk Management Plan Summary

### 7.1 Risk Management Activities Completed

- ✅ Risk analysis conducted (12 risks identified)
- ✅ Control measures defined and implemented
- ✅ Residual risks evaluated and accepted
- ✅ Traceability established (risks → requirements → architecture → tests)
- ✅ Risk control verification defined

### 7.2 Compliance with Standards

**ISO 14971 (Risk Management for Medical Devices)**:
- ✅ Section 4: Risk analysis performed
- ✅ Section 5: Risk evaluation completed
- ✅ Section 6: Risk control measures implemented
- ✅ Section 7: Residual risk evaluation and acceptance
- ⚠️ Section 8: Risk management review (ongoing process)
- ⚠️ Section 9: Production/post-production (future)

**IEC 62304 Section 7 (Software Risk Management)**:
- ✅ 7.1: Risk management process established
- ✅ 7.2: Risk analysis conducted
- ✅ 7.3: Risk control measures defined
- ✅ 7.4: Residual risk acceptance documented

### 7.3 Known Limitations and Future Work

**Current Limitations**:
1. Single-instance deployment (no high availability)
2. No rate limiting for brute-force prevention
3. No automated backups configured
4. No penetration testing performed
5. Performance testing not conducted

**Future Risk Reduction Measures** (Phase 6+):
1. Implement rate limiting (ASP.NET Core middleware)
2. Azure deployment with HA + auto-scaling
3. Automated daily backups with point-in-time recovery
4. Professional security audit and penetration testing
5. Load testing with realistic clinical data volumes
6. Optimistic concurrency via If-Match headers
7. Real-time security monitoring and alerting

---

**Document Status**: Released
**Next Review Date**: Upon Phase 6 implementation, security incident, or regulatory change
**Approval**: David Sosa Junquera - Software Developer - 2025-10-16
