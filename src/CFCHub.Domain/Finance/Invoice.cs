using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Finance;

public sealed class Invoice : Entity<InvoiceId>
{
    public InstallmentId InstallmentId { get; private set; }

    public Invoice(InvoiceId id, InstallmentId installmentId) : base(id)
    {
        InstallmentId = installmentId;
    }

#pragma warning disable CS8618
    private Invoice() { }
#pragma warning restore CS8618
}
