using CFCHub.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Shared;

public class ErrorTests
{
    [Fact]
    public void NotFound_Should_SetTypeToNotFound()
    {
        var error = Error.NotFound("STUDENT_NOT_FOUND", "desc");
        
        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("STUDENT_NOT_FOUND");
        error.Description.Should().Be("desc");
    }

    [Fact]
    public void Conflict_Should_SetTypeToConflict()
    {
        var error = Error.Conflict("CONFLICT", "desc");
        error.Type.Should().Be(ErrorType.Conflict);
    }
    
    [Fact]
    public void Validation_Should_SetTypeToValidation()
    {
        var error = Error.Validation("VAL", "desc");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Unauthorized_Should_SetTypeToUnauthorized()
    {
        var error = Error.Unauthorized("UNAUTH", "desc");
        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_Should_SetTypeToForbidden()
    {
        var error = Error.Forbidden("FORBID", "desc");
        error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void Unexpected_Should_SetTypeToUnexpected()
    {
        var error = Error.Unexpected("UNEXPECTED", "desc");
        error.Type.Should().Be(ErrorType.Unexpected);
    }
}
