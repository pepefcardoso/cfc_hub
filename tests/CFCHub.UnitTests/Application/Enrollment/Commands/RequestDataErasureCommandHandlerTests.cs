using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Enrollment.Commands.RequestDataErasure;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Enrollment.Commands;

public class RequestDataErasureCommandHandlerTests
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IInstallmentRepository _installmentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IDataErasureRequestRepository _dataErasureRequestRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly ISystemClock _clock;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RequestDataErasureCommandHandler _handler;

    public RequestDataErasureCommandHandlerTests()
    {
        _studentRepository = Substitute.For<IStudentRepository>();
        _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
        _contractRepository = Substitute.For<IContractRepository>();
        _installmentRepository = Substitute.For<IInstallmentRepository>();
        _paymentRepository = Substitute.For<IPaymentRepository>();
        _dataErasureRequestRepository = Substitute.For<IDataErasureRequestRepository>();
        _idGenerator = Substitute.For<IIdGenerator>();
        _clock = Substitute.For<ISystemClock>();
        _outboxService = Substitute.For<IOutboxService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _idGenerator.NewId<DataErasureRequestId>().Returns(new DataErasureRequestId(Guid.NewGuid()));
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _handler = new RequestDataErasureCommandHandler(
            _studentRepository,
            _enrollmentRepository,
            _contractRepository,
            _installmentRepository,
            _paymentRepository,
            _dataErasureRequestRepository,
            _idGenerator,
            _clock,
            _outboxService,
            _unitOfWork);
    }

    [Fact]
    public async Task RequestErasure_WithActiveContract_BlocksRequest()
    {
        // Arrange
        var studentIdGuid = Guid.NewGuid();
        var studentId = new StudentId(studentIdGuid);
        
        var student = Student.Create(studentId, "John", "12345678901", null, "john@test.com", "11999999999", new DateOnly(1990, 1, 1), Address.Empty, _clock, _idGenerator);
        _studentRepository.GetByIdAsync(studentId, Arg.Any<CancellationToken>()).Returns(student);

        var contract = Contract.Create(new ContractId(Guid.NewGuid()), studentId, new EnrollmentId(Guid.NewGuid()), null, _clock);
        // Contract is created as Pending
        _contractRepository.GetByStudentIdAsync(studentId, Arg.Any<CancellationToken>()).Returns(contract);

        var command = new RequestDataErasureCommand(studentIdGuid);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(DataErasureRequestStatus.Blocked);
        result.BlockReason.Should().Be("ACTIVE_CONTRACT");
        
        await _dataErasureRequestRepository.Received(1).AddAsync(
            Arg.Is<DataErasureRequest>(r => r.Status == DataErasureRequestStatus.Blocked && r.BlockReason == "ACTIVE_CONTRACT"), 
            Arg.Any<CancellationToken>());
            
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _outboxService.DidNotReceiveWithAnyArgs().InsertAsync(default!, default!);
    }
    
    [Fact]
    public async Task RequestErasure_WithUnpaidDebt_BlocksRequest()
    {
        // Arrange
        var studentIdGuid = Guid.NewGuid();
        var studentId = new StudentId(studentIdGuid);
        
        var student = Student.Create(studentId, "John", "12345678901", null, "john@test.com", "11999999999", new DateOnly(1990, 1, 1), Address.Empty, _clock, _idGenerator);
        _studentRepository.GetByIdAsync(studentId, Arg.Any<CancellationToken>()).Returns(student);

        _contractRepository.GetByStudentIdAsync(studentId, Arg.Any<CancellationToken>()).Returns((Contract?)null);

        var enrollmentId = new EnrollmentId(Guid.NewGuid());
        var enrollment = CFCHub.Domain.Enrollment.Enrollment.Enroll(enrollmentId, studentId, CnhCategory.B, _clock);
        _enrollmentRepository.GetByStudentIdAsync(studentId, Arg.Any<CancellationToken>()).Returns(new List<CFCHub.Domain.Enrollment.Enrollment> { enrollment });

        _installmentRepository.HasOverdueInstallmentsAsync(Arg.Any<IEnumerable<EnrollmentId>>(), Arg.Any<CancellationToken>()).Returns(true);

        var command = new RequestDataErasureCommand(studentIdGuid);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(DataErasureRequestStatus.Blocked);
        result.BlockReason.Should().Be("UNPAID_DEBT");
        
        await _dataErasureRequestRepository.Received(1).AddAsync(
            Arg.Is<DataErasureRequest>(r => r.Status == DataErasureRequestStatus.Blocked && r.BlockReason == "UNPAID_DEBT"), 
            Arg.Any<CancellationToken>());
            
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestErasure_WithNoHolds_EnqueuesWorker()
    {
        // Arrange
        var studentIdGuid = Guid.NewGuid();
        var studentId = new StudentId(studentIdGuid);
        
        var student = Student.Create(studentId, "John", "12345678901", null, "john@test.com", "11999999999", new DateOnly(1990, 1, 1), Address.Empty, _clock, _idGenerator);
        _studentRepository.GetByIdAsync(studentId, Arg.Any<CancellationToken>()).Returns(student);

        _contractRepository.GetByStudentIdAsync(studentId, Arg.Any<CancellationToken>()).Returns((Contract?)null);
        _enrollmentRepository.GetByStudentIdAsync(studentId, Arg.Any<CancellationToken>()).Returns(new List<CFCHub.Domain.Enrollment.Enrollment>());
        _paymentRepository.GetByStudentIdAsync(studentId, Arg.Any<CancellationToken>()).Returns(new List<Payment>());

        var command = new RequestDataErasureCommand(studentIdGuid);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(DataErasureRequestStatus.Pending);
        result.BlockReason.Should().BeNull();
        
        student.Status.Should().Be(StudentStatus.PendingErasure);
        
        await _dataErasureRequestRepository.Received(1).AddAsync(
            Arg.Is<DataErasureRequest>(r => r.Status == DataErasureRequestStatus.Pending), 
            Arg.Any<CancellationToken>());
            
        await _studentRepository.Received(1).UpdateAsync(student, Arg.Any<CancellationToken>());
            
        await _outboxService.Received(1).InsertAsync(
            "DataErasureRequested", 
            Arg.Is<string>(s => s.Contains(studentIdGuid.ToString())), 
            Arg.Any<CancellationToken>());
            
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
