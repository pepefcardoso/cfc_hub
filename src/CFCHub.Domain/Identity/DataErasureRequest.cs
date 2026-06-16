using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Identity;

public class DataErasureRequest : IAuditable
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
