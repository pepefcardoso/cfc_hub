using System;
using System.Threading.Tasks;
using System.Transactions;
using CFCHub.Application.Scheduling.Commands.BookSlot;
using CFCHub.Domain.Students;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CFCHub.IntegrationTests.Outbox;

[Collection("Integration")] // Optional, but standard
public class OutboxAtomicityTests : IntegrationTestBase
{
    public OutboxAtomicityTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task BookSlot_OutboxMessage_InSameTransactionAsSlot()
    {
        var slotId = new SchedulingSlotId(Guid.NewGuid());
        var instructorId = new InstructorId(Guid.NewGuid());
        var vehicleId = new VehicleId(Guid.NewGuid());
        var trackId = new TrackId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());
        var clock = ServiceProvider.GetRequiredService<ISystemClock>();

        var slot = SchedulingSlot.Book(
            slotId,
            instructorId,
            vehicleId,
            trackId,
            studentId,
            new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1).AddHours(10), TimeSpan.Zero),
            CFCHub.Domain.Scheduling.CnhCategory.B,
            clock);

        var outboxMessage = CFCHub.Domain.Shared.Outbox.OutboxMessage.Create("Test", "Payload", clock.UtcNow);

        // Act
        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            DbContext.SchedulingSlots.Add(slot);
            DbContext.OutboxMessages.Add(outboxMessage);
            await DbContext.SaveChangesAsync();
            
            // We intentionally DO NOT call scope.Complete() to force a rollback
        }

        // Assert
        var slots = await DbContext.SchedulingSlots.ToListAsync();
        slots.Should().BeEmpty("Slot should not be saved due to transaction rollback");

        var outboxMessages = await DbContext.OutboxMessages.ToListAsync();
        outboxMessages.Should().BeEmpty("Outbox message should not be saved due to transaction rollback");
    }
}
