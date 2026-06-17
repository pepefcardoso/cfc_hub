using System;
using CFCHub.Domain.Finance;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Finance;

public class InvoiceTests
{
    [Fact]
    public void Invoice_Constructor_SetsInstallmentId()
    {
        var id = new InvoiceId(Guid.NewGuid());
        var instId = new InstallmentId(Guid.NewGuid());
        
        var invoice = new Invoice(id, instId);
        
        invoice.Id.Should().Be(id);
        invoice.InstallmentId.Should().Be(instId);
    }

    [Fact]
    public void Invoice_EfCoreConstructor_Exists()
    {
        var instance = Activator.CreateInstance(typeof(Invoice), true);
        instance.Should().NotBeNull();
    }
}
