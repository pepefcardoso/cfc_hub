using System;
using System.Diagnostics;
using CFCHub.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace CFCHub.Api.Endpoints.Common;

public class ApiResponse<T>
{
    public required T Data { get; init; }
    public required ApiMeta Meta { get; init; }
}

public class ApiPagedResponse<T>
{
    public required T Data { get; init; }
    public required ApiPagedMeta Meta { get; init; }
}

public class ApiMeta
{
    public required string TraceId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

public class ApiPagedMeta : ApiMeta
{
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
}

public static class ResultExtensions
{
    public static IResult ToApiResponse<T>(this Result<T> result, HttpContext context)
    {
        if (result.IsSuccess)
        {
            var response = new ApiResponse<T>
            {
                Data = result.Value!,
                Meta = new ApiMeta
                {
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                    Timestamp = DateTimeOffset.UtcNow
                }
            };
            return Results.Ok(response);
        }

        return Results.Problem(
            statusCode: MapErrorType(result.Error!.Type),
            title: result.Error.Code,
            detail: result.Error.Description,
            extensions: new System.Collections.Generic.Dictionary<string, object?> { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier }
        );
    }

    public static IResult ToCreatedApiResponse<T>(this Result<T> result, HttpContext context, string uri)
    {
        if (result.IsSuccess)
        {
            var response = new ApiResponse<T>
            {
                Data = result.Value!,
                Meta = new ApiMeta
                {
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                    Timestamp = DateTimeOffset.UtcNow
                }
            };
            return Results.Created(uri, response);
        }
        return ToApiResponse(result, context);
    }

    public static IResult ToApiPagedResponse<T>(this PagedResult<T> result, HttpContext context)
    {
        var response = new ApiPagedResponse<System.Collections.Generic.IReadOnlyList<T>>
        {
            Data = result.Items,
            Meta = new ApiPagedMeta
            {
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                Timestamp = DateTimeOffset.UtcNow,
                NextCursor = result.NextCursor,
                HasMore = result.HasMore
            }
        };
        return Results.Ok(response);
    }

    private static int MapErrorType(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}
