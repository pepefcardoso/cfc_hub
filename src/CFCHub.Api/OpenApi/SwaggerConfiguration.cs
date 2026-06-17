using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CFCHub.Api.OpenApi;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<SecuritySchemeFilter>();
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                var defaultResponses = new[] { "400", "401", "403", "404", "409", "422", "500" };

                foreach (var statusCode in defaultResponses)
                {
                    if (operation.Responses != null && !operation.Responses.ContainsKey(statusCode))
                    {
                        var response = new OpenApiResponse
                        {
                            Description = "Problem Details",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/problem+json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchemaReference("ProblemDetails", context.Document)
                                }
                            }
                        };
                        operation.Responses.Add(statusCode, response);
                    }
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "CFCHub API V1");
                options.RoutePrefix = "swagger";
            });
        }

        return app;
    }
}
