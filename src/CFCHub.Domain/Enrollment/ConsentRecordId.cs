using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment;

public sealed record ConsentRecordId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static ConsentRecordId New() => new(Guid.NewGuid());
}
