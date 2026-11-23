using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ClaimsApi.OpenApi;

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Paste the accessToken from POST /api/auth/login (without the 'Bearer' prefix), then click Authorize.",
        };

        // Authorize requests
        foreach (var path in document.Paths.Values)
        {
            // Make Security an empty list if it is null
            if (path.Operations is null)
            {
                continue;
            }

            // Add the requirement (Security)
            foreach (var operation in path.Operations.Values)
            {
                operation.Security ??= [];

                operation.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
                    }
                );
            }
        }

        return Task.CompletedTask;
    }
}
