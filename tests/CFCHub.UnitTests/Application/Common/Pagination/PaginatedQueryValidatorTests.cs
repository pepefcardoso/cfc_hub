using CFCHub.Application.Common.Pagination;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using FluentValidation;
using Xunit;

namespace CFCHub.UnitTests.Application.Common.Pagination;

public class PaginatedQueryValidatorTests
{
    private record TestQuery : PaginatedQuery<string>
    {
        public TestQuery(string? cursor, int limit) : base(cursor, limit) { }
    }

    private class TestQueryValidator : PaginatedQueryValidator<TestQuery, string>
    {
    }

    [Fact]
    public void Validate_WithLimitGreaterThan100_ShouldThrowDomainValidationException()
    {
        // Arrange
        var query = new TestQuery(null, 101);
        var validator = new TestQueryValidator();

        // Act
        Action act = () => validator.Validate(query);

        // Assert
        act.Should().Throw<CFCHub.Domain.Shared.Exceptions.ValidationException>()
            .WithMessage("Page limit cannot exceed 100.")
            .And.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public void Validate_WithValidLimit_ShouldNotThrow()
    {
        // Arrange
        var query = new TestQuery(null, 100);
        var validator = new TestQueryValidator();

        // Act
        Action act = () => validator.Validate(query);

        // Assert
        act.Should().NotThrow();
    }
}
