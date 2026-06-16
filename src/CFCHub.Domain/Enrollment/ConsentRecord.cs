using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment;

public class ConsentRecord : Entity<ConsentRecordId>
{
    public StudentId StudentId { get; private init; }
    public string PolicyVersion { get; private init; }
    public string PolicyContentHash { get; private init; }
    public DateTimeOffset ConsentedAt { get; private init; }
    public string IpAddress { get; private init; }
    public string UserAgent { get; private init; }
    public ConsentChannel Channel { get; private init; }

#pragma warning disable CS8618 // EF Core
    private ConsentRecord() { }
#pragma warning restore CS8618

    private ConsentRecord(
        ConsentRecordId id,
        StudentId studentId,
        string policyVersion,
        string policyContentHash,
        DateTimeOffset consentedAt,
        string ipAddress,
        string userAgent,
        ConsentChannel channel) : base(id)
    {
        StudentId = studentId;
        PolicyVersion = policyVersion;
        PolicyContentHash = policyContentHash;
        ConsentedAt = consentedAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Channel = channel;
    }

    public static ConsentRecord Capture(
        ConsentRecordId id,
        StudentId studentId,
        string policyVersion,
        string policyContentHash,
        DateTimeOffset consentedAt,
        string ipAddress,
        string userAgent,
        ConsentChannel channel)
    {
        return new ConsentRecord(
            id, 
            studentId, 
            policyVersion, 
            policyContentHash, 
            consentedAt, 
            ipAddress, 
            userAgent, 
            channel);
    }
}
