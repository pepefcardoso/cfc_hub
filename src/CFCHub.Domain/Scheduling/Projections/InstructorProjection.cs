using System;
using System.Collections.Generic;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling.Projections;

public record InstructorProjection(
    Guid Id,
    string Name,
    IReadOnlyList<CnhCategory> TeachableCategories);
