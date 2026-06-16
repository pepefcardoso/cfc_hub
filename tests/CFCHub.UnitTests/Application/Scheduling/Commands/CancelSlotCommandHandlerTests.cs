using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Scheduling.Commands.CancelSlot;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Students;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Scheduling.Commands;

public class CancelSlotCommandHandlerTests
{
    private readonly ISchedulingRepository _repository = Substitute.For<ISchedulingRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISystemClock _clock = Substitute.For<ISystemClock>();
    private readonly CancelSlotCommandHandler _handler;

    public CancelSlotCommandHandlerTests()
    {
        _handler = new CancelSlotCommandHandler(_repository, _currentUserService, _unitOfWork, _clock);
    }

    [Fact]
    public async Task CancelSlot_ByOtherStudent_ThrowsForbidden()
    {
        // Arrange
        var slotId = new SchedulingSlotId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());
        var otherStudentId = Guid.NewGuid();
        
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(now);

        var slot = SchedulingSlot.Book(
            slotId,
            new InstructorId(Guid.NewGuid()),
            new VehicleId(Guid.NewGuid()),
            new TrackId(Guid.NewGuid()),
            studentId,
            now.AddDays(1), // This will be 12:00:00
            CnhCategory.B,
            _clock);

        _repository.GetSlotByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(slot);
        
        // Simulating another student calling the endpoint
        _currentUserService.UserId.Returns(otherStudentId);
        // Financial is not allowed to cancel slots
        _currentUserService.Role.Returns(RoleType.Financial); 

        var command = new CancelSlotCommand(slotId.Value, "No longer needed");

        // Act & Assert
        var action = async () => await _handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<ForbiddenException>();
    }
}
