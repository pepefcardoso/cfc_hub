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

namespace CFCHub.UnitTests.Application.Scheduling;

public class CancelSlotCommandHandlerTests
{
    private readonly ISchedulingRepository _repositoryMock;
    private readonly ICurrentUserService _currentUserServiceMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ISystemClock _clockMock;
    private readonly CancelSlotCommandHandler _handler;

    public CancelSlotCommandHandlerTests()
    {
        _repositoryMock = Substitute.For<ISchedulingRepository>();
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _clockMock = Substitute.For<ISystemClock>();

        _clockMock.UtcNow.Returns(new DateTimeOffset(2023, 1, 1, 10, 0, 0, TimeSpan.Zero));

        _handler = new CancelSlotCommandHandler(
            _repositoryMock,
            _currentUserServiceMock,
            _unitOfWorkMock,
            _clockMock);
    }

    [Fact]
    public async Task CancelSlot_ByOtherStudent_ReturnsForbidden()
    {
        // Arrange
        var slotId = new SchedulingSlotId(Guid.NewGuid());
        var slotOwnerId = Guid.NewGuid();
        var anotherStudentId = Guid.NewGuid();

        var command = new CancelSlotCommand(slotId.Value, "No reason");

        var slot = SchedulingSlot.Book(
            slotId,
            new InstructorId(Guid.NewGuid()),
            new VehicleId(Guid.NewGuid()),
            new TrackId(Guid.NewGuid()),
            new StudentId(slotOwnerId),
            _clockMock.UtcNow.AddDays(1),
            CnhCategory.B,
            _clockMock);

        _repositoryMock.GetSlotByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(slot);

        _currentUserServiceMock.UserId.Returns(anotherStudentId);
        _currentUserServiceMock.Role.Returns((RoleType)999);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ForbiddenException>();
        exception.Which.Message.Should().Be("Você não tem permissão para cancelar este agendamento.");
    }
}
