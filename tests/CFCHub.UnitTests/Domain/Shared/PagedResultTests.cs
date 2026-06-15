using System.Collections.Generic;
using CFCHub.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Shared;

public class PagedResultTests
{
    [Fact]
    public void Constructor_Should_InitializeProperties()
    {
        var items = new List<string> { "item1", "item2" };
        var pagedResult = new PagedResult<string>(items, "next_cursor", true, 2);
        
        pagedResult.Items.Should().BeEquivalentTo(items);
        pagedResult.NextCursor.Should().Be("next_cursor");
        pagedResult.HasMore.Should().BeTrue();
        pagedResult.Count.Should().Be(2);
    }
}
