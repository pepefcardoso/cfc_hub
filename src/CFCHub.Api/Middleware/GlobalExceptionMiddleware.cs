using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CFCHub.Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CFCHub.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred while executing the request.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier }
        };

        if (exception is CfcHubException cfcException)
        {
            context.Response.StatusCode = MapStatusCode(cfcException);
            problemDetails.Status = context.Response.StatusCode;
            
            var errorInfo = GetErrorInfo(cfcException);
            problemDetails.Type = errorInfo.Type;
            problemDetails.Detail = errorInfo.Detail;

            if (cfcException is ValidationException validationException)
            {
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                
                problemDetails.Extensions["errors"] = errors;
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Type = "https://cfchub.com.br/errors/internal-error";
            problemDetails.Detail = "Erro interno. Tente novamente.";
        }

        var json = JsonSerializer.Serialize(problemDetails, _jsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static int MapStatusCode(CfcHubException ex) => ex switch
    {
        ValidationException => 400,
        UnauthorizedException => 401,
        ForbiddenException => 403,
        NotFoundException => 404,
        ConflictException => 409,
        UnprocessableException => 422,
        InfrastructureException => 500,
        _ => 500
    };

    private static (string Type, string Detail) GetErrorInfo(CfcHubException ex)
    {
        return ex.ErrorCode switch
        {
            "VALIDATION_ERROR" => ("https://cfchub.com.br/errors/validation-error", "Erro de validação nos dados da requisição."),
            "UNAUTHORIZED" => ("https://cfchub.com.br/errors/unauthorized", "Autenticação necessária para acessar este recurso."),
            "FORBIDDEN" => ("https://cfchub.com.br/errors/forbidden", "Permissões insuficientes para realizar esta ação."),
            "NOT_FOUND" => ("https://cfchub.com.br/errors/not-found", "O recurso solicitado não foi encontrado."),
            "TENANT_NOT_FOUND" => ("https://cfchub.com.br/errors/tenant-not-found", "Tenant não encontrado."),
            "CONFLICT" => ("https://cfchub.com.br/errors/conflict", "Conflito de estado do recurso."),
            "SCHEDULING_CONFLICT" => ("https://cfchub.com.br/errors/scheduling-conflict", "O instrutor já possui aula agendada neste horário."),
            "UNPROCESSABLE" => ("https://cfchub.com.br/errors/unprocessable", "Regra de negócio violada."),
            "INTERNAL_ERROR" => ("https://cfchub.com.br/errors/internal-error", "Erro interno do servidor."),
            
            _ => ex switch
            {
                ValidationException => ("https://cfchub.com.br/errors/validation-error", "Erro de validação nos dados da requisição."),
                UnauthorizedException => ("https://cfchub.com.br/errors/unauthorized", "Autenticação necessária para acessar este recurso."),
                ForbiddenException => ("https://cfchub.com.br/errors/forbidden", "Permissões insuficientes para realizar esta ação."),
                NotFoundException => ("https://cfchub.com.br/errors/not-found", "O recurso solicitado não foi encontrado."),
                ConflictException => ("https://cfchub.com.br/errors/conflict", "Conflito de estado do recurso."),
                UnprocessableException => ("https://cfchub.com.br/errors/unprocessable", "Regra de negócio violada."),
                _ => ("https://cfchub.com.br/errors/internal-error", "Erro interno. Tente novamente.")
            }
        };
    }
}
