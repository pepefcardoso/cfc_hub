using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Enrollment.Commands.RequestDataErasure;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Shared;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Enrollment;

public class RequestDataErasureCommandHandlerTests
{
    [Fact]
    public async Task RequestErasure_WithActiveContract_BlocksRequest()
    {
        // Arrange
        var studentRepository = Substitute.For<IStudentRepository>();
        var enrollmentRepository = Substitute.For<IEnrollmentRepository>();
        var contractRepository = Substitute.For<IContractRepository>();
        var installmentRepository = Substitute.For<IInstallmentRepository>();
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var dataErasureRequestRepository = Substitute.For<IDataErasureRequestRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var clock = Substitute.For<ISystemClock>();
        var outboxService = Substitute.For<IOutboxService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new RequestDataErasureCommandHandler(
            studentRepository, enrollmentRepository, contractRepository, installmentRepository,
            paymentRepository, dataErasureRequestRepository, idGenerator, clock, outboxService, unitOfWork);

        var student = new StudentBuilder().Build();
        studentRepository.GetByIdAsync(student.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Student?>(student));

        // Active contract means not Signed and not Cancelled
        var activeContract = new ContractBuilder().WithStatus(ContractStatus.Generated).Build();
        contractRepository.GetByStudentIdAsync(student.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Contract?>(activeContract));

        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        idGenerator.NewId<DataErasureRequestId>().Returns(new DataErasureRequestId(Guid.NewGuid()));

        var command = new RequestDataErasureCommand(student.Id.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(DataErasureRequestStatus.Blocked);
        result.BlockReason.Should().Be("ACTIVE_CONTRACT");
        
        await dataErasureRequestRepository.Received(1).AddAsync(Arg.Any<DataErasureRequest>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
