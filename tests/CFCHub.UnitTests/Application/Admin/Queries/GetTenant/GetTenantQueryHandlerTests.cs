using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Admin.Queries.GetTenant;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Admin.Queries.GetTenant;

public class GetTenantQueryHandlerTests
{
    private readonly ITenantRegistry _tenantRegistryMock;
    private readonly GetTenantQueryHandler _handler;

    public GetTenantQueryHandlerTests()
    {
        _tenantRegistryMock = Substitute.For<ITenantRegistry>();
        _handler = new GetTenantQueryHandler(_tenantRegistryMock);
    }

    [Fact]
    public async Task Handle_WithExistingSlug_ShouldReturnTenantResult()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var record = new TenantRecord(tenantId, "test_slug", "cfc_test_slug", "Active");
        _tenantRegistryMock.GetBySlugAsync("test_slug", Arg.Any<CancellationToken>())
            .Returns(record);

        var query = new GetTenantQuery("test_slug");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(tenantId);
        result.Slug.Should().Be("test_slug");
        result.SchemaName.Should().Be("cfc_test_slug");
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_WithNonExistingSlug_ShouldThrowTenantNotFoundException()
    {
        // Arrange
        _tenantRegistryMock.GetBySlugAsync("test_slug", Arg.Any<CancellationToken>())
            .Returns((TenantRecord?)null);

        var query = new GetTenantQuery("test_slug");

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TenantNotFoundException>();
    }
}
