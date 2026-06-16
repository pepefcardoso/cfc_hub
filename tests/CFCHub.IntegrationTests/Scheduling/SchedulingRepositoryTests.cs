using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit;

namespace CFCHub.IntegrationTests.Scheduling;

public class SchedulingRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AppDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private ISystemClock _clock = null!;
    private IDataProtectionService _dataProtectionService = null!;
    private ISchedulingRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.SchemaName.Returns("cfc_test");
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _tenantContext.TenantSlug.Returns("test");
        _tenantContext.IsResolved.Returns(true);

        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _dataProtectionService = Substitute.For<IDataProtectionService>();
        _dataProtectionService.Encrypt(Arg.Any<string>(), Arg.Any<string>()).Returns(x => x[0]);
        _dataProtectionService.Decrypt(Arg.Any<string>(), Arg.Any<string>()).Returns(x => x[0]);

        var services = new ServiceCollection();
        services.AddSingleton(_tenantContext);
        services.AddSingleton(_clock);
        services.AddSingleton(_dataProtectionService);
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        var serviceProvider = services.BuildServiceProvider();
        _dbContext = serviceProvider.GetRequiredService<AppDbContext>();

#pragma warning disable EF1002
        await _dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{_tenantContext.SchemaName}\";");
#pragma warning restore EF1002
        
        await _dbContext.Database.EnsureCreatedAsync();

        _sut = new SchedulingRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetByStudentAsync_WithCursor_ReturnsPaginatedResults()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());
        var instructorId = new InstructorId(Guid.NewGuid());
        var vehicleId = new VehicleId(Guid.NewGuid());
        var trackId = new TrackId(Guid.NewGuid());

        var baseTime = new DateTimeOffset(2026, 6, 16, 10, 0, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(baseTime.AddDays(-1)); // Allow booking

        // Create 5 slots for the same student
        for (int i = 0; i < 5; i++)
        {
            var slot = SchedulingSlot.Book(
                new SchedulingSlotId(Guid.NewGuid()),
                instructorId,
                vehicleId,
                trackId,
                studentId,
                baseTime.AddHours(i),
                CnhCategory.B,
                _clock);
            
            _dbContext.Set<SchedulingSlot>().Add(slot);
        }
        await _dbContext.SaveChangesAsync();

        // Act - Page 1 (Limit 2)
        var page1 = await _sut.GetByStudentAsync(studentId, null, 2, CancellationToken.None);

        // Assert - Page 1
        page1.Items.Should().HaveCount(2);
        page1.HasMore.Should().BeTrue();
        page1.NextCursor.Should().NotBeNullOrEmpty();

        // Act - Page 2 (Limit 2)
        var page2 = await _sut.GetByStudentAsync(studentId, page1.NextCursor, 2, CancellationToken.None);

        // Assert - Page 2
        page2.Items.Should().HaveCount(2);
        page2.HasMore.Should().BeTrue();
        page2.NextCursor.Should().NotBeNullOrEmpty();
        page2.Items[0].Id.Should().NotBe(page1.Items[0].Id);
        page2.Items[0].Id.Should().NotBe(page1.Items[1].Id);

        // Act - Page 3 (Limit 2)
        var page3 = await _sut.GetByStudentAsync(studentId, page2.NextCursor, 2, CancellationToken.None);

        // Assert - Page 3
        page3.Items.Should().HaveCount(1);
        page3.HasMore.Should().BeFalse();
        // Since we reached the end, NextCursor could be populated or null depending on implementation, 
        // but items count is definitely 1.
    }
}
