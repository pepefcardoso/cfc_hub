using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Scheduling.Commands.BookSlot;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CFCHub.UnitTests.Application.Scheduling;

public class BookSlotCommandHandlerTests
{
    private readonly ISchedulingLockService _lockServiceMock;
    private readonly ISchedulingRepository _repositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IOutboxService _outboxServiceMock;
    private readonly IIdGenerator _idGeneratorMock;
    private readonly ISystemClock _clockMock;
    private readonly BookSlotCommandHandler _handler;

    public BookSlotCommandHandlerTests()
    {
        _lockServiceMock = Substitute.For<ISchedulingLockService>();
        _repositoryMock = Substitute.For<ISchedulingRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _outboxServiceMock = Substitute.For<IOutboxService>();
        _idGeneratorMock = Substitute.For<IIdGenerator>();
        _clockMock = Substitute.For<ISystemClock>();

        _clockMock.UtcNow.Returns(new DateTimeOffset(2023, 1, 1, 10, 0, 0, TimeSpan.Zero));

        _handler = new BookSlotCommandHandler(
            _lockServiceMock,
            _repositoryMock,
            _unitOfWorkMock,
            _outboxServiceMock,
            _idGeneratorMock,
            _clockMock);
    }

    [Fact]
    public async Task Handle_WhenLockFails_ReturnsConflictResult()
    {
        // Arrange
        var command = new BookSlotCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CnhCategory.B, _clockMock.UtcNow.AddDays(1));
        _lockServiceMock.AcquireAllAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("SLOT_LOCK_FAILED");
    }

    [Fact]
    public async Task Handle_WhenOverlapFoundAfterLock_ReleasesLocksAndReturnsConflict()
    {
        // Arrange
        var command = new BookSlotCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CnhCategory.B, _clockMock.UtcNow.AddDays(1));
        _lockServiceMock.AcquireAllAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(true);
        
        var slotId = new SchedulingSlotId(Guid.NewGuid());
        var slot = SchedulingSlot.Book(slotId, new InstructorId(command.InstructorId), new VehicleId(command.VehicleId), new TrackId(command.TrackId), new StudentId(command.StudentId), command.StartedAt, command.Category, _clockMock);
        
        _repositoryMock.GetOverlappingInstructorSlotAsync(Arg.Any<InstructorId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(slot);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("SLOT_OVERLAP");
        await _unitOfWorkMock.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        await _lockServiceMock.Received(1).ReleaseAllAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidData_InsertsSlotAndOutboxMessage()
    {
        // Arrange
        var command = new BookSlotCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CnhCategory.B, _clockMock.UtcNow.AddDays(1));
        _lockServiceMock.AcquireAllAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(true);
        _idGeneratorMock.NewId<SchedulingSlotId>().Returns(new SchedulingSlotId(Guid.NewGuid()));

        _repositoryMock.GetOverlappingInstructorSlotAsync(Arg.Any<InstructorId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((SchedulingSlot?)null);
        _repositoryMock.GetOverlappingVehicleSlotAsync(Arg.Any<VehicleId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((SchedulingSlot?)null);
        _repositoryMock.GetOverlappingTrackSlotAsync(Arg.Any<TrackId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((SchedulingSlot?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repositoryMock.Received(1).AddAsync(Arg.Any<SchedulingSlot>(), Arg.Any<CancellationToken>());
        await _outboxServiceMock.Received(1).InsertAsync("SlotBooked", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _unitOfWorkMock.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
        await _lockServiceMock.Received(1).ReleaseAllAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LockAlwaysReleasedInFinallyBlock()
    {
        // Arrange
        var command = new BookSlotCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CnhCategory.B, _clockMock.UtcNow.AddDays(1));
        _lockServiceMock.AcquireAllAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>()).Returns(true);
        
        _unitOfWorkMock.BeginTransactionAsync(Arg.Any<CancellationToken>()).Throws(new Exception("Database error"));

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database error");
        await _lockServiceMock.Received(1).ReleaseAllAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
    }
}
