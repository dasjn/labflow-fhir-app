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

                    // Security (for future JWT implementation)
                    Security = new CapabilityStatement.SecurityComponent
                    {
                        Description = "Currently no authentication required. JWT authentication planned for production deployment."
                    },

                    // Supported resources
                    Resource = new List<CapabilityStatement.ResourceComponent>
                    {
                        // Patient resource capabilities
                        new CapabilityStatement.ResourceComponent
                        {
                            Type = "Patient",
                            Profile = "http://hl7.org/fhir/StructureDefinition/Patient",
                            Documentation = "Patient demographics and identification",

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
                                    Code = CapabilityStatement.TypeRestfulInteraction.SearchType,
                                    Documentation = "Search for Patient resources"
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
                            Documentation = "Laboratory test results and measurements",

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
                                    Code = CapabilityStatement.TypeRestfulInteraction.SearchType,
                                    Documentation = "Search for Observation resources"
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
