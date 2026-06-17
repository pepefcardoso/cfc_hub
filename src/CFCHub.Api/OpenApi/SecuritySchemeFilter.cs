using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace CFCHub.Api.OpenApi;

public sealed class SecuritySchemeFilter : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        };

        if (document.Components == null)
        {
            document.Components = new OpenApiComponents();
        }

        if (document.Components.SecuritySchemes == null)
        {
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
        }

        document.Components.SecuritySchemes["Bearer"] = securityScheme;

        if (document.Components.Schemas == null)
        {
            document.Components.Schemas = new Dictionary<string, IOpenApiSchema>();
        }

        if (!document.Components.Schemas.ContainsKey("ProblemDetails"))
        {
            document.Components.Schemas["ProblemDetails"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["type"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["title"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
                    ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            };
        }

        if (document.Paths != null)
        {
            foreach (var path in document.Paths.Values)
            {
                if (path.Operations != null)
                {
                    foreach (var operation in path.Operations.Values)
                    {
                        if (operation.Security == null)
                        {
                            operation.Security = new List<OpenApiSecurityRequirement>();
                        }
                        operation.Security.Add(requirement);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}
