using System;
using System.Security.Cryptography;
using System.Text;

namespace CFCHub.Infrastructure.Caching;

public static class RedisKeys
{
    // Scheduling locks
    public static string SchedulingLockInstructor(string env, string tenant, Guid instructorId)
        => $"{env}:{tenant}:sched:lock:instructor:{instructorId}";

    public static string SchedulingLockVehicle(string env, string tenant, Guid vehicleId)
        => $"{env}:{tenant}:sched:lock:vehicle:{vehicleId}";

    public static string SchedulingLockTrack(string env, string tenant, Guid trackId)
        => $"{env}:{tenant}:sched:lock:track:{trackId}";

    // Availability cache
    public static string InstructorAvailability(string env, string tenant, Guid instructorId, DateOnly date)
        => $"{env}:{tenant}:sched:avail:instructor:{instructorId}:{date:yyyy-MM-dd}";

    // DETRAN cache
    public static string DetranCnhStatus(string env, string tenant, string cpfHash)
        => $"{env}:{tenant}:detran:cnh:{cpfHash}";

    // Rate limiting
    public static string RateLimit(string env, string tenant, string endpointHash, string userId)
        => $"{env}:{tenant}:rl:{endpointHash}:{userId}";

    // Staff session
    public static string StaffSession(string env, string tenant, string jti)
        => $"{env}:{tenant}:session:{jti}";

    // Outbox worker lease
    public static string OutboxWorkerLease(string env, string tenant)
        => $"{env}:{tenant}:outbox:lease";

    // Tenant resolution cache
    public static string TenantResolution(string env, string slug)
        => $"{env}:global:tenant:{slug}";

    // Document Expiry Lease
    public static string DocExpiryLease(string env, string date)
        => $"{env}:global:docexpiry:lease:{date}";

    // Slot Reminder Lease
    public static string SlotReminderLease(string env, string tenant)
        => $"{env}:{tenant}:slotreminder:lease";

    public static string CpfHash(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            throw new ArgumentException("CPF cannot be null or whitespace.", nameof(cpf));
        }

        var bytes = Encoding.UTF8.GetBytes(cpf);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
