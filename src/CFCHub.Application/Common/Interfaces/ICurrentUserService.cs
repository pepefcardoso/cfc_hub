using System;
using CFCHub.Domain.Identity;

namespace CFCHub.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    RoleType Role { get; }
    string IpAddress { get; }
    string? UserAgent { get; }
    string TraceId { get; }
}
