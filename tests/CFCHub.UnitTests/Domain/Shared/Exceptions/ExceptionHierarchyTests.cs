using System;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Shared.Exceptions;

public class ExceptionHierarchyTests
{
    [Fact]
    public void SchedulingConflictException_ShouldBe_ConflictException_And_CfcHubException()
    {
        var ex = new SchedulingConflictException("Test message");

        ex.Should().BeAssignableTo<ConflictException>();
        ex.Should().BeAssignableTo<CfcHubException>();
        ex.ErrorCode.Should().Be("SCHEDULING_CONFLICT");
        ex.StatusCode.Should().Be(409);
    }

    [Fact]
    public void StorageException_ShouldBe_InfrastructureException_And_CfcHubException()
    {
        var ex = new StorageException("Test message");

        ex.Should().BeAssignableTo<InfrastructureException>();
        ex.Should().BeAssignableTo<CfcHubException>();
        ex.ErrorCode.Should().Be("STORAGE_ERROR");
        ex.StatusCode.Should().Be(500);
    }

    [Fact]
    public void EmailDeliveryException_ShouldBe_InfrastructureException_And_CfcHubException()
    {
        var ex = new EmailDeliveryException("Test message");

        ex.Should().BeAssignableTo<InfrastructureException>();
        ex.Should().BeAssignableTo<CfcHubException>();
        ex.ErrorCode.Should().Be("EMAIL_DELIVERY_ERROR");
        ex.StatusCode.Should().Be(500);
    }

    [Fact]
    public void TenantNotFoundException_ShouldBe_NotFoundException_And_CfcHubException()
    {
        var ex = new TenantNotFoundException("Test message");

        ex.Should().BeAssignableTo<NotFoundException>();
        ex.Should().BeAssignableTo<CfcHubException>();
        ex.ErrorCode.Should().Be("TENANT_NOT_FOUND");
        ex.StatusCode.Should().Be(404);
    }

    [Theory]
    [InlineData(typeof(ValidationException), "VALIDATION_ERROR", 400)]
    [InlineData(typeof(UnauthorizedException), "UNAUTHORIZED", 401)]
    [InlineData(typeof(ForbiddenException), "FORBIDDEN", 403)]
    [InlineData(typeof(NotFoundException), "NOT_FOUND", 404)]
    [InlineData(typeof(ConflictException), "CONFLICT", 409)]
    [InlineData(typeof(UnprocessableException), "UNPROCESSABLE", 422)]
    [InlineData(typeof(InfrastructureException), "INTERNAL_ERROR", 500)]
    public void BaseExceptions_ShouldHaveCorrect_StatusCodeAndErrorCode(Type exceptionType, string expectedErrorCode, int expectedStatusCode)
    {
        var ex = (CfcHubException)Activator.CreateInstance(exceptionType, "Test message", expectedErrorCode)!;

        ex.Should().BeAssignableTo<CfcHubException>();
        ex.ErrorCode.Should().Be(expectedErrorCode);
        ex.StatusCode.Should().Be(expectedStatusCode);
    }
}
