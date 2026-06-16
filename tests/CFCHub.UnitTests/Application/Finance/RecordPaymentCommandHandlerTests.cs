using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Finance.Commands.RecordPayment;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Finance;

public class RecordPaymentCommandHandlerTests
{
    private readonly IInstallmentRepository _installmentRepository = Substitute.For<IInstallmentRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IOutboxService _outboxService = Substitute.For<IOutboxService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISystemClock _clock = Substitute.For<ISystemClock>();
    private readonly IIdGenerator _idGenerator = Substitute.For<IIdGenerator>();
    
    private readonly RecordPaymentCommandHandler _handler;

    public RecordPaymentCommandHandlerTests()
    {
        _handler = new RecordPaymentCommandHandler(
            _installmentRepository,
            _paymentRepository,
            _outboxService,
            _unitOfWork,
            _clock,
            _idGenerator);
            
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task RecordPayment_Confirm_EnqueuesReceipt()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var installmentIdValue = Guid.NewGuid();
        var paymentIdValue = Guid.NewGuid();
        
        var command = new RecordPaymentCommand(
            studentId,
            enrollmentId,
            installmentIdValue,
            100m,
            "BRL",
            "Pix");

        var installmentId = new InstallmentId(installmentIdValue);
        var installment = Installment.Create(
            installmentId,
            new EnrollmentId(enrollmentId),
            new Money(100m, "BRL"),
            DateOnly.FromDateTime(DateTime.UtcNow));

        _installmentRepository.GetByIdAsync(installmentId, Arg.Any<CancellationToken>())
            .Returns(installment);

        _idGenerator.NewId<PaymentId>().Returns(new PaymentId(paymentIdValue));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(paymentIdValue);
        
        await _paymentRepository.Received(1).AddAsync(Arg.Is<Payment>(p => 
            p.Id.Value == paymentIdValue && 
            p.Status == PaymentStatus.Confirmed), Arg.Any<CancellationToken>());
            
        installment.Status.Should().Be(InstallmentStatus.Paid);

        await _outboxService.Received(1).InsertAsync(
            "PaymentReceiptRequested",
            Arg.Is<string>(s => s.Contains(paymentIdValue.ToString())),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
    }
}
