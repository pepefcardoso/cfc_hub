using CFCHub.Application.Common.Pagination;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Application.Common.Pagination;

public class CursorEncoderTests
{
    [Fact]
    public void EncodeAndDecode_ShouldRoundTripCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var encoded = CursorEncoder.Encode(id, timestamp);
        var decoded = CursorEncoder.Decode(encoded);

        // Assert
        encoded.Should().NotBeNullOrWhiteSpace();
        decoded.Id.Should().Be(id);
        decoded.Timestamp.Should().Be(timestamp);
    }
}
