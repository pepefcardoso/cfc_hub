using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CFCHub.Api.Middleware;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CFCHub.IntegrationTests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private readonly DefaultHttpContext _context;
    private readonly MemoryStream _responseBodyStream;

    public GlobalExceptionMiddlewareTests()
    {
        _context = new DefaultHttpContext();
        _responseBodyStream = new MemoryStream();
        _context.Response.Body = _responseBodyStream;
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_DoesNotExposeStackTrace()
    {
        // Arrange
        var exception = new Exception("Critical internal failure with stack trace");
        
        var middleware = new GlobalExceptionMiddleware(
            ctx => throw exception,
            NullLogger<GlobalExceptionMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        
        _responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_responseBodyStream).ReadToEndAsync();
        
        responseBody.Should().NotContain("StackTrace");
        responseBody.Should().NotContain("Critical internal failure with stack trace");
        
        var json = JsonDocument.Parse(responseBody);
        json.RootElement.GetProperty("detail").GetString().Should().Be("Erro interno. Tente novamente.");
    }
}
