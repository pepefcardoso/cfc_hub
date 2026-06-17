using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Scheduling.Commands.CompleteSlot;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Students;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Scheduling;

public class CompleteSlotCommandHandlerTests
{
    private readonly ISchedulingRepository _repositoryMock;
    private readonly ICurrentUserService _currentUserServiceMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ISystemClock _clockMock;
    private readonly CompleteSlotCommandHandler _handler;

    public CompleteSlotCommandHandlerTests()
    {
        _repositoryMock = Substitute.For<ISchedulingRepository>();
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _clockMock = Substitute.For<ISystemClock>();

        _clockMock.UtcNow.Returns(new DateTimeOffset(2023, 1, 1, 10, 0, 0, TimeSpan.Zero));

        _handler = new CompleteSlotCommandHandler(
            _repositoryMock,
            _currentUserServiceMock,
            _unitOfWorkMock,
            _clockMock);
    }

    [Fact]
    public async Task CompleteSlot_WithValidInstructor_CompletesSlot()
    {
        // Arrange
        var slotId = new SchedulingSlotId(Guid.NewGuid());
        var command = new CompleteSlotCommand(slotId.Value);

        var slotStartTime = _clockMock.UtcNow;
        _clockMock.UtcNow.Returns(slotStartTime.AddHours(-1)); // Mock to past for booking

        var slot = SchedulingSlot.Book(
            slotId,
            new InstructorId(Guid.NewGuid()),
            new VehicleId(Guid.NewGuid()),
            new TrackId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            slotStartTime,
            CnhCategory.B,
            _clockMock);
            
        _clockMock.UtcNow.Returns(slotStartTime.AddHours(2)); // Mock to future for completion

        _repositoryMock.GetSlotByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(slot);

        _currentUserServiceMock.Role.Returns(RoleType.Instructor);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        slot.Status.Should().Be(SlotStatus.Completed);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
