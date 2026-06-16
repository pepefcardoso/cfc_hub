using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment;

public record StudentId(Guid Value) : StronglyTypedId<Guid>(Value);
