using CFCHub.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Shared;

public class ResultTests
{
    [Fact]
    public void Success_Should_ReturnSuccessResult()
    {
        var result = Result<int>.Success(42);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }
    
    [Fact]
    public void Failure_Should_ReturnFailureResult()
    {
        var error = Error.Conflict("X", "y");
        var result = Result<int>.Failure(error);
        
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
        result.Value.Should().Be(default);
    }
    
    [Fact]
    public void ImplicitOperator_FromValue_Should_ReturnSuccessResult()
    {
        int value = 1;
        Result<int> r = value;
        
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(1);
    }
    
    [Fact]
    public void ImplicitOperator_FromError_Should_ReturnFailureResult()
    {
        var error = Error.Validation("V", "desc");
        Result<int> r = error;
        
        r.IsSuccess.Should().BeFalse();
        r.Error.Should().Be(error);
    }
}
