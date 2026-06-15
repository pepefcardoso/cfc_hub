using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Shared.Pagination;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Shared.Pagination;

public class CursorTests
{
    [Fact]
    public void Encode_ShouldProduceBase64String()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var cursor = new Cursor(id, timestamp);

        // Act
        var encoded = cursor.Encode();

        // Assert
        encoded.Should().NotBeNullOrWhiteSpace();
        Action parseBase64 = () => Convert.FromBase64String(encoded);
        parseBase64.Should().NotThrow<FormatException>();
    }

    [Fact]
    public void Parse_WithValidEncodedString_ShouldReturnEquivalentCursor()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var originalCursor = new Cursor(id, timestamp);
        var encoded = originalCursor.Encode();

        // Act
        var parsedCursor = Cursor.Parse(encoded);

        // Assert
        parsedCursor.Should().NotBeNull();
        parsedCursor.Id.Should().Be(id);
        parsedCursor.Timestamp.Should().Be(timestamp);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_WithEmptyString_ShouldThrowValidationException(string? emptyInput)
    {
        // Act
        Action act = () => Cursor.Parse(emptyInput!);

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("Cursor cannot be empty.")
            .And.ErrorCode.Should().Be("INVALID_CURSOR");
    }

    [Theory]
    [InlineData("not-base-64-string")] // Invalid base64
    [InlineData("dGhpcy1pcy1ub3QtanNvbg==")] // Base64 for "this-is-not-json"
    [InlineData("eyJJZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC0wMDAwLTAwMDAwMDAwMDAwMCIsIlRpbWVzdGFtcCI6IjIwMjYtMDYtMTVUMjA6NTA6MTVaIn0=")] // Valid JSON but empty Guid ID
    public void Parse_WithMalformedData_ShouldThrowValidationException(string malformedInput)
    {
        // Act
        Action act = () => Cursor.Parse(malformedInput);

        // Assert
        act.Should().Throw<ValidationException>()
            .And.ErrorCode.Should().Be("INVALID_CURSOR");
    }
}
