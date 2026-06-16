using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using StackExchange.Redis;

namespace CFCHub.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _clock;
    private readonly IConnectionMultiplexer _redis;

    public AuditInterceptor(
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        ISystemClock clock,
        IConnectionMultiplexer redis)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _clock = clock;
        _redis = redis;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AuditEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AuditEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AuditEntities(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        if (!entries.Any()) return;

        var now = _clock.UtcNow;
        var userId = _currentUserService.UserId;
        var userRole = _currentUserService.Role.ToString();
        var ipAddress = _currentUserService.IpAddress;
        var userAgent = _currentUserService.UserAgent;
        var traceId = _currentUserService.TraceId;

        foreach (var entry in entries)
        {
            if (IsAuditableEntity(entry.Entity))
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityType = entry.Entity.GetType().Name,
                    EntityId = GetPrimaryKey(entry),
                    Action = entry.State.ToString(),
                    OccurredAt = now,
                    ActorUserId = userId,
                    ActorRole = userRole,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    TraceId = traceId,
                    ChangedFields = GetChangedFields(entry)
                };

                context.Add(auditLog);
            }

            if (entry.Entity is SchedulingSlot slot && (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted))
            {
                InvalidateSchedulingSlotCache(slot);
            }
        }
    }

    private bool IsAuditableEntity(object entity)
    {
        return entity is Student || 
               entity is DocumentRecord || 
               entity is Contract || 
               entity is Payment || 
               entity is Enrollment;
    }

    private string GetPrimaryKey(EntityEntry entry)
    {
        var keyName = entry.Metadata.FindPrimaryKey()?.Properties.Select(x => x.Name).FirstOrDefault();
        if (keyName != null)
        {
            var value = entry.Property(keyName).CurrentValue;
            if (value != null)
            {
                // In DDD with ValueObjects as IDs, we might need to handle them carefully.
                // Assuming EF Core handles the conversion or ToString() works appropriately.
                return value.ToString() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    private string GetChangedFields(EntityEntry entry)
    {
        var changes = new Dictionary<string, object>();

        foreach (var property in entry.Properties)
        {
            if (property.IsTemporary) continue;
            
            if (entry.State == EntityState.Added)
            {
                changes[property.Metadata.Name] = new { to = MaskIfSensitive(property.Metadata, property.CurrentValue) };
            }
            else if (entry.State == EntityState.Modified && property.IsModified)
            {
                changes[property.Metadata.Name] = new 
                { 
                    from = MaskIfSensitive(property.Metadata, property.OriginalValue), 
                    to = MaskIfSensitive(property.Metadata, property.CurrentValue) 
                };
            }
            else if (entry.State == EntityState.Deleted)
            {
                changes[property.Metadata.Name] = new { from = MaskIfSensitive(property.Metadata, property.OriginalValue) };
            }
        }

        return JsonSerializer.Serialize(changes);
    }

    private object? MaskIfSensitive(IProperty property, object? value)
    {
        if (value == null) return null;

        var propInfo = property.PropertyInfo;
        if (propInfo != null && Attribute.IsDefined(propInfo, typeof(SensitiveAttribute)))
        {
            return "[encrypted]";
        }
        
        return value;
    }

    private void InvalidateSchedulingSlotCache(SchedulingSlot slot)
    {
        if (slot.InstructorId != null)
        {
            var date = slot.StartedAt.ToString("yyyy-MM-dd");
            var key = $"sched:avail:instructor:{slot.InstructorId.Value}:{date}";
            
            // Fire and forget invalidation
            _redis.GetDatabase().KeyDelete(key, CommandFlags.FireAndForget);
        }
    }
}
