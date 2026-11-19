using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PestiTrackBackend.Swagger;

public sealed class CsrfHeaderOperationFilter : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;

        // CSRF validation is required for state-changing requests.
        if (method is null ||
            method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("TRACE", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-CSRF-TOKEN",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Request token obtained from GET /api/auth/csrf.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            }
        });
    }
}