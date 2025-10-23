using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Task = System.Threading.Tasks.Task;

namespace LabFlow.API.Configuration;

/// <summary>
/// Custom OutputFormatter para serializar recursos FHIR usando FhirJsonSerializer
/// Esto asegura que los recursos FHIR se serialicen correctamente según el estándar FHIR R4
/// </summary>
public class FhirJsonOutputFormatter : TextOutputFormatter
{
    private readonly FhirJsonSerializer _fhirSerializer;

    public FhirJsonOutputFormatter(FhirJsonSerializer fhirSerializer)
    {
        _fhirSerializer = fhirSerializer;

        // Soporta application/fhir+json y application/json
        SupportedMediaTypes.Add("application/fhir+json");
        SupportedMediaTypes.Add("application/json");

        SupportedEncodings.Add(Encoding.UTF8);
    }

    /// <summary>
    /// Determina si este formatter puede serializar el tipo dado
    /// Solo serializa tipos que heredan de Base (recursos FHIR)
    /// </summary>
    protected override bool CanWriteType(Type? type)
    {
        if (type == null)
            return false;

        // Solo serializar recursos FHIR (Patient, Observation, Bundle, OperationOutcome, etc.)
        return typeof(Base).IsAssignableFrom(type);
    }

    /// <summary>
    /// Serializa el recurso FHIR a JSON usando FhirJsonSerializer
    /// </summary>
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        if (context.Object is not Base fhirResource)
            return;

        var httpContext = context.HttpContext;

        // Serializar usando FhirJsonSerializer (garantiza formato FHIR R4 correcto)
        var json = _fhirSerializer.SerializeToString(fhirResource);

        // Escribir response
        await httpContext.Response.WriteAsync(json, selectedEncoding);
    }
}
