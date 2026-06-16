using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Contracts;

public class SignatureRecord : Entity<SignatureRecordId>
{
    public ContractId ContractId { get; private set; }
    public string SignatureHash { get; private set; }
    public string IpAddress { get; private set; }
    public DateTimeOffset SignedAt { get; private set; }

    public SignatureRecord(SignatureRecordId id, ContractId contractId, string signatureHash, string ipAddress, DateTimeOffset signedAt) : base(id)
    {
        ContractId = contractId;
        SignatureHash = signatureHash;
        IpAddress = ipAddress;
        SignedAt = signedAt;
    }

    private SignatureRecord() : base()
    {
        ContractId = null!;
        SignatureHash = null!;
        IpAddress = null!;
    } // EF Core
}
