using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Scheduling.Commands.CompleteSlot;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Scheduling.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Scheduling.Commands;

public class CompleteSlotCommandHandlerTests
{
    private readonly ISchedulingRepository _repository = Substitute.For<ISchedulingRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISystemClock _clock = Substitute.For<ISystemClock>();
    private readonly CompleteSlotCommandHandler _handler;

    public CompleteSlotCommandHandlerTests()
    {
        _handler = new CompleteSlotCommandHandler(_repository, _currentUserService, _unitOfWork, _clock);
    }

    [Fact]
    public async Task CompleteSlot_PublishesCompletedEvent()
    {
        // Arrange
        var slotId = new SchedulingSlotId(Guid.NewGuid());
        
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(now);

        var slot = SchedulingSlot.Book(
            slotId,
            new InstructorId(Guid.NewGuid()),
            new VehicleId(Guid.NewGuid()),
            new TrackId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            now.AddDays(1), // 12:00:00
            CnhCategory.B,
            _clock);

        // Clear existing events from booking
        slot.ClearDomainEvents();

        _repository.GetSlotByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(slot);
        
        _currentUserService.Role.Returns(RoleType.Instructor);

        var command = new CompleteSlotCommand(slotId.Value);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        slot.Status.Should().Be(SlotStatus.Completed);
        var domainEvents = slot.DomainEvents;
        domainEvents.Should().ContainSingle(e => e is SchedulingSlotCompletedEvent);
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
