using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;

namespace LabFlow.API.Controllers;

/// <summary>
/// FHIR Metadata endpoint - CapabilityStatement
/// REQ-FHIR-003: Provide server capability documentation per FHIR R4 specification
/// </summary>
[ApiController]
[Route("")]
[Produces("application/fhir+json", "application/json")]
public class MetadataController : ControllerBase
{
    private readonly ILogger<MetadataController> _logger;

    public MetadataController(ILogger<MetadataController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get server capability statement
    /// FHIR operation: GET [base]/metadata
    /// Standard endpoint that describes what this FHIR server can do
    /// </summary>
    /// <returns>CapabilityStatement resource</returns>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(CapabilityStatement), StatusCodes.Status200OK)]
    public IActionResult GetCapabilityStatement()
    {
        _logger.LogInformation("GET /metadata - Returning CapabilityStatement");

        var capability = new CapabilityStatement
        {
            // Required fields
            Status = PublicationStatus.Active,
            Date = "2025-10-13",
            Kind = CapabilityStatementKind.Instance,
            FhirVersion = FHIRVersion.N4_0_1,

            // Software information
            Software = new CapabilityStatement.SoftwareComponent
            {
                Name = "LabFlow FHIR API",
                Version = "1.0.0",
                ReleaseDate = "2025-10-13"
            },

            // Implementation details
            Implementation = new CapabilityStatement.ImplementationComponent
            {
                Description = "Laboratory Results Interoperability System - FHIR R4 compliant API for seamless laboratory results exchange",
                Url = $"{Request.Scheme}://{Request.Host}"
            },

            // Supported formats
            Format = new List<string> { "json" },

            // REST capabilities
            Rest = new List<CapabilityStatement.RestComponent>
            {
                new CapabilityStatement.RestComponent
                {
                    Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                    Documentation = "Main FHIR endpoint for laboratory results exchange",

                    // Security (JWT Bearer token authentication)
                    Security = new CapabilityStatement.SecurityComponent
                    {
                        Cors = true,
                        Service = new List<CodeableConcept>
                        {
                            new CodeableConcept
                            {
                                Coding = new List<Coding>
                                {
                                    new Coding
                                    {
                                        System = "http://terminology.hl7.org/CodeSystem/restful-security-service",
                                        Code = "OAuth",
                                        Display = "OAuth"
                                    }
                                },
                                Text = "JWT Bearer Token Authentication"
                            }
                        },
                        Description = @"JWT Bearer token authentication required for all FHIR resource endpoints (except /metadata).

**Authentication Flow:**
1. Register: POST /Auth/register (email, password, role)
2. Login: POST /Auth/login → Returns JWT token
3. Use token: Include 'Authorization: Bearer {token}' header in all FHIR requests

**Roles and Permissions:**
- **Doctor**: Can manage patients, order tests (ServiceRequest), and view all results
- **LabTechnician**: Can create/view observations and diagnostic reports
- **Admin**: Full access to all resources

**Token Details:**
- Algorithm: HS256 (HMAC-SHA256)
- Expiration: 60 minutes
- Claims: sub (userId), email, role, fhirUser, scope

**Example:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**HTTPS Required:** All authentication endpoints require HTTPS in production."
                    },

                    // Supported resources
                    Resource = new List<CapabilityStatement.ResourceComponent>
                    {
                        // Patient resource capabilities
                        new CapabilityStatement.ResourceComponent
                        {
                            Type = "Patient",
                            Profile = "http://hl7.org/fhir/StructureDefinition/Patient",
                            Documentation = "Patient demographics and identification. Full CRUD operations supported.",

                            // Supported interactions
                            Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                            {
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Read,
                                    Documentation = "Read Patient by ID"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Create,
                                    Documentation = "Create new Patient resource"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Update,
                                    Documentation = "Update existing Patient resource"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Delete,
                                    Documentation = "Delete Patient resource (soft delete - preserves audit trail)"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.SearchType,
                                    Documentation = "Search for Patient resources with pagination support"
                                }
                            },

                            // Supported search parameters
                            SearchParam = new List<CapabilityStatement.SearchParamComponent>
                            {
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "name",
                                    Type = SearchParamType.String,
                                    Documentation = "Search by patient name (family or given). Supports partial matching."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "identifier",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by patient identifier (e.g., medical record number). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "birthdate",
                                    Type = SearchParamType.Date,
                                    Documentation = "Search by birth date. Format: YYYY-MM-DD. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "gender",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by gender. Values: male, female, other, unknown. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "_count",
                                    Type = SearchParamType.Number,
                                    Documentation = "Number of results per page (default: 20, max: 100)"
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "_offset",
                                    Type = SearchParamType.Number,
                                    Documentation = "Number of results to skip for pagination (default: 0)"
                                }
                            },

                            Versioning = CapabilityStatement.ResourceVersionPolicy.Versioned,
                            ReadHistory = false,
                            UpdateCreate = false,
                            ConditionalCreate = false,
                            ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                            ConditionalUpdate = false,
                            ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported
                        },

                        // Observation resource capabilities
                        new CapabilityStatement.ResourceComponent
                        {
                            Type = "Observation",
                            Profile = "http://hl7.org/fhir/StructureDefinition/Observation",
                            Documentation = "Laboratory test results and measurements. Full CRUD operations supported.",

                            // Supported interactions
                            Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                            {
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Read,
                                    Documentation = "Read Observation by ID"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Create,
                                    Documentation = "Create new Observation resource. Validates patient reference exists."
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Update,
                                    Documentation = "Update existing Observation resource"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Delete,
                                    Documentation = "Delete Observation resource (soft delete - preserves audit trail)"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.SearchType,
                                    Documentation = "Search for Observation resources with pagination support"
                                }
                            },

                            // Supported search parameters
                            SearchParam = new List<CapabilityStatement.SearchParamComponent>
                            {
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "patient",
                                    Type = SearchParamType.Reference,
                                    Documentation = "Search by patient reference. Accepts 'Patient/123' or '123'. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "code",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by observation code (LOINC preferred). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "category",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by category (e.g., laboratory, vital-signs). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "date",
                                    Type = SearchParamType.Date,
                                    Documentation = "Search by observation date. Format: YYYY-MM-DD. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "status",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by status (final, preliminary, etc.). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "_count",
                                    Type = SearchParamType.Number,
                                    Documentation = "Number of results per page (default: 20, max: 100)"
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "_offset",
                                    Type = SearchParamType.Number,
                                    Documentation = "Number of results to skip for pagination (default: 0)"
                                }
                            },

                            Versioning = CapabilityStatement.ResourceVersionPolicy.Versioned,
                            ReadHistory = false,
                            UpdateCreate = false,
                            ConditionalCreate = false,
                            ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                            ConditionalUpdate = false,
                            ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported
                        },

                        // DiagnosticReport resource capabilities
                        new CapabilityStatement.ResourceComponent
                        {
                            Type = "DiagnosticReport",
                            Profile = "http://hl7.org/fhir/StructureDefinition/DiagnosticReport",
                            Documentation = "Grouped laboratory reports with multiple test results. Full CRUD operations supported.",

                            // Supported interactions
                            Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                            {
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Read,
                                    Documentation = "Read DiagnosticReport by ID"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Create,
                                    Documentation = "Create new DiagnosticReport resource. Validates patient and observation references exist."
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Update,
                                    Documentation = "Update existing DiagnosticReport resource"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Delete,
                                    Documentation = "Delete DiagnosticReport resource (soft delete - preserves audit trail)"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.SearchType,
                                    Documentation = "Search for DiagnosticReport resources with pagination support"
                                }
                            },

                            // Supported search parameters
                            SearchParam = new List<CapabilityStatement.SearchParamComponent>
                            {
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "patient",
                                    Type = SearchParamType.Reference,
                                    Documentation = "Search by patient reference. Accepts 'Patient/123' or '123'. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "code",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by report code (LOINC panel codes). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "category",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by service category (LAB, RAD, PATH, etc.). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "date",
                                    Type = SearchParamType.Date,
                                    Documentation = "Search by effective date (when study was performed). Format: YYYY-MM-DD. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "issued",
                                    Type = SearchParamType.Date,
                                    Documentation = "Search by issued date (when report was published). Format: YYYY-MM-DD. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "status",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by status (registered, partial, preliminary, final, etc.). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "_count",
                                    Type = SearchParamType.Number,
                                    Documentation = "Number of results per page (default: 20, max: 100)"
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "_offset",
                                    Type = SearchParamType.Number,
                                    Documentation = "Number of results to skip for pagination (default: 0)"
                                }
                            },

                            Versioning = CapabilityStatement.ResourceVersionPolicy.Versioned,
                            ReadHistory = false,
                            UpdateCreate = false,
                            ConditionalCreate = false,
                            ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                            ConditionalUpdate = false,
                            ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported
                        },

                        // ServiceRequest resource capabilities
                        new CapabilityStatement.ResourceComponent
                        {
                            Type = "ServiceRequest",
                            Profile = "http://hl7.org/fhir/StructureDefinition/ServiceRequest",
                            Documentation = "Laboratory test orders and service requests. Full CRUD operations supported.",

                            // Supported interactions
                            Interaction = new List<CapabilityStatement.ResourceInteractionComponent>
                            {
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Read,
                                    Documentation = "Read ServiceRequest by ID"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Create,
                                    Documentation = "Create new ServiceRequest resource. Validates patient reference exists."
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Update,
                                    Documentation = "Update existing ServiceRequest resource"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.Delete,
                                    Documentation = "Delete ServiceRequest resource (soft delete - preserves audit trail)"
                                },
                                new CapabilityStatement.ResourceInteractionComponent
                                {
                                    Code = CapabilityStatement.TypeRestfulInteraction.SearchType,
                                    Documentation = "Search for ServiceRequest resources with pagination support"
                                }
                            },

                            // Supported search parameters
                            SearchParam = new List<CapabilityStatement.SearchParamComponent>
                            {
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "patient",
                                    Type = SearchParamType.Reference,
                                    Documentation = "Search by patient reference. Accepts 'Patient/123' or '123'. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "code",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by service code (LOINC test codes). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "status",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by status (draft, active, on-hold, revoked, completed, entered-in-error, unknown). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "intent",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by intent (proposal, plan, directive, order, original-order, reflex-order, filler-order, instance-order, option). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "category",
                                    Type = SearchParamType.Token,
                                    Documentation = "Search by service category. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "authored",
                                    Type = SearchParamType.Date,
                                    Documentation = "Search by authored date (when order was created). Format: YYYY-MM-DD. Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "requester",
                                    Type = SearchParamType.Reference,
                                    Documentation = "Search by requester reference (who ordered the test). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "performer",
                                    Type = SearchParamType.Reference,
                                    Documentation = "Search by performer reference (who will perform the test). Exact match."
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "_count",
                                    Type = SearchParamType.Number,
                                    Documentation = "Number of results per page (default: 20, max: 100)"
                                },
                                new CapabilityStatement.SearchParamComponent
                                {
                                    Name = "_offset",
                                    Type = SearchParamType.Number,
                                    Documentation = "Number of results to skip for pagination (default: 0)"
                                }
                            },

                            Versioning = CapabilityStatement.ResourceVersionPolicy.Versioned,
                            ReadHistory = false,
                            UpdateCreate = false,
                            ConditionalCreate = false,
                            ConditionalRead = CapabilityStatement.ConditionalReadStatus.NotSupported,
                            ConditionalUpdate = false,
                            ConditionalDelete = CapabilityStatement.ConditionalDeleteStatus.NotSupported
                        }
                    }
                }
            }
        };

        return Ok(capability);
    }
}
