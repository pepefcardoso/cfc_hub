using System;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;

namespace CFCHub.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    public Guid UserId { get; set; }
    public RoleType Role { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string TraceId { get; set; } = string.Empty;
}
