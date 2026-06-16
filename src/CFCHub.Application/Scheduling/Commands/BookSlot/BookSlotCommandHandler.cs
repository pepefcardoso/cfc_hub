using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;
using MediatR;

namespace CFCHub.Application.Scheduling.Commands.BookSlot;

public class BookSlotCommandHandler : IRequestHandler<BookSlotCommand, Result<BookSlotResult>>
{
    private readonly ISchedulingLockService _lockService;
    private readonly ISchedulingRepository _schedulingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxService _outboxService;
    private readonly IIdGenerator _idGenerator;
    private readonly ISystemClock _clock;

    public BookSlotCommandHandler(
        ISchedulingLockService lockService,
        ISchedulingRepository schedulingRepository,
        IUnitOfWork unitOfWork,
        IOutboxService outboxService,
        IIdGenerator idGenerator,
        ISystemClock clock)
    {
        _lockService = lockService;
        _schedulingRepository = schedulingRepository;
        _unitOfWork = unitOfWork;
        _outboxService = outboxService;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<BookSlotResult>> Handle(BookSlotCommand request, CancellationToken cancellationToken)
    {
        var keys = new List<string>
        {
            $"instructor:{request.InstructorId}",
            $"vehicle:{request.VehicleId}",
            $"track:{request.TrackId}"
        };
        keys.Sort(); // Lexicographical sorting

        var acquired = await _lockService.AcquireAllAsync(keys, cancellationToken);
        if (!acquired)
        {
            return Result<BookSlotResult>.Failure(Error.Conflict("SLOT_LOCK_FAILED", "Não foi possível adquirir os bloqueios necessários para agendamento. Tente novamente."));
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var instructorId = new InstructorId(request.InstructorId);
                var vehicleId = new VehicleId(request.VehicleId);
                var trackId = new TrackId(request.TrackId);
                var studentId = new StudentId(request.StudentId);
                var end = request.StartedAt.AddMinutes(50);

                var overlappingInstructor = await _schedulingRepository.GetOverlappingInstructorSlotAsync(instructorId, request.StartedAt, end, cancellationToken);
                var overlappingVehicle = await _schedulingRepository.GetOverlappingVehicleSlotAsync(vehicleId, request.StartedAt, end, cancellationToken);
                var overlappingTrack = await _schedulingRepository.GetOverlappingTrackSlotAsync(trackId, request.StartedAt, end, cancellationToken);

                if (overlappingInstructor != null || overlappingVehicle != null || overlappingTrack != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<BookSlotResult>.Failure(Error.Conflict("SLOT_OVERLAP", "Já existe um agendamento conflitante para este horário."));
                }

                var slotId = _idGenerator.NewId<SchedulingSlotId>();
                
                var slot = SchedulingSlot.Book(
                    slotId,
                    instructorId,
                    vehicleId,
                    trackId,
                    studentId,
                    request.StartedAt,
                    request.Category,
                    _clock);

                await _schedulingRepository.AddAsync(slot, cancellationToken);

                var payload = JsonSerializer.Serialize(new
                {
                    SlotId = slot.Id.Value,
                    InstructorId = slot.InstructorId.Value,
                    VehicleId = slot.VehicleId.Value,
                    TrackId = slot.TrackId.Value,
                    StudentId = slot.StudentId.Value,
                    StartedAt = slot.StartedAt,
                    EndedAt = slot.EndedAt
                });

                await _outboxService.InsertAsync("SlotBooked", payload, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<BookSlotResult>.Success(new BookSlotResult(slot.Id.Value));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }
        finally
        {
            await _lockService.ReleaseAllAsync(keys, CancellationToken.None); // Release handles multiple keys. Wait, let's check lockservice
        }
    }
}
