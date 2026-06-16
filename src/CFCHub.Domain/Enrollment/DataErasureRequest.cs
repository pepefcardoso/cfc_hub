using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment;

public class DataErasureRequest : AggregateRoot<DataErasureRequestId>, IAuditable
{
    public StudentId StudentId { get; private set; }
    public DataErasureRequestStatus Status { get; private set; }
    public string? BlockReason { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

#pragma warning disable CS8618 // EF Core
    private DataErasureRequest() { }
#pragma warning restore CS8618

    private DataErasureRequest(DataErasureRequestId id, StudentId studentId, DataErasureRequestStatus status) : base(id)
    {
        StudentId = studentId;
        Status = status;
    }

    public static DataErasureRequest Create(DataErasureRequestId id, StudentId studentId, ISystemClock clock)
    {
        return new DataErasureRequest(id, studentId, DataErasureRequestStatus.Pending);
    }

    public void Block(string reason)
    {
        Status = DataErasureRequestStatus.Blocked;
        BlockReason = reason;
    }

    public void Complete(ISystemClock clock)
    {
        Status = DataErasureRequestStatus.Completed;
        CompletedAt = clock.UtcNow;
    }
}
