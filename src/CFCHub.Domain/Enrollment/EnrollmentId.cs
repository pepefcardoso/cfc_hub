using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment;

public sealed record EnrollmentId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static EnrollmentId New() => new(Guid.NewGuid());
}
