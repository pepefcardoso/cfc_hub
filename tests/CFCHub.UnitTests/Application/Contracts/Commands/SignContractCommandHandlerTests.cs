using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Contracts.Commands.SignContract;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Contracts.Commands;

public class SignContractCommandHandlerTests
{
    private readonly IContractRepository _contractRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly SignContractCommandHandler _handler;

    public SignContractCommandHandlerTests()
    {
        _contractRepository = Substitute.For<IContractRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _clock = Substitute.For<ISystemClock>();
        _idGenerator = Substitute.For<IIdGenerator>();

        _handler = new SignContractCommandHandler(
            _contractRepository,
            _currentUserService,
            _clock,
            _idGenerator);
    }

    [Fact]
    public async Task SignContract_WhenPending_ThrowsUnprocessable()
    {
        // Arrange
        var contractId = new ContractId(Guid.NewGuid());
        var studentId = new CFCHub.Domain.Enrollment.StudentId(Guid.NewGuid());
        var enrollmentId = new CFCHub.Domain.Enrollment.EnrollmentId(Guid.NewGuid());

        var contract = Contract.Create(contractId, studentId, enrollmentId, null, _clock);

        _contractRepository.GetByIdAsync(contractId, Arg.Any<CancellationToken>())
            .Returns(contract);

        var command = new SignContractCommand(contractId.Value, "hash");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<UnprocessableException>();
        exception.WithMessage("*pending generation*");
        exception.Which.ErrorCode.Should().Be("CONTRACT_PENDING");

        await _contractRepository.DidNotReceive().UpdateAsync(Arg.Any<Contract>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignContract_WhenGenerated_SignsContract()
    {
        // Arrange
        var contractId = new ContractId(Guid.NewGuid());
        var studentId = new CFCHub.Domain.Enrollment.StudentId(Guid.NewGuid());
        var enrollmentId = new CFCHub.Domain.Enrollment.EnrollmentId(Guid.NewGuid());
        var utcNow = DateTimeOffset.UtcNow;
        var newSignatureId = new SignatureRecordId(Guid.NewGuid());

        _clock.UtcNow.Returns(utcNow);
        _idGenerator.NewId<SignatureRecordId>().Returns(newSignatureId);
        _currentUserService.IpAddress.Returns("127.0.0.1");

        var contract = Contract.Create(contractId, studentId, enrollmentId, null, _clock);
        contract.MarkGenerated("s3key");

        _contractRepository.GetByIdAsync(contractId, Arg.Any<CancellationToken>())
            .Returns(contract);

        var command = new SignContractCommand(contractId.Value, "hash");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        contract.Status.Should().Be(ContractStatus.Signed);
        contract.SignedAt.Should().Be(utcNow);
        contract.Signature.Should().NotBeNull();
        contract.Signature!.IpAddress.Should().Be("127.0.0.1");
        contract.Signature.SignatureHash.Should().Be("hash");

        await _contractRepository.Received(1).UpdateAsync(contract, Arg.Any<CancellationToken>());
    }
}
