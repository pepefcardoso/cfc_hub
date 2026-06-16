using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Enrollment.Queries.GetStudent;

public class GetStudentQueryHandler : IRequestHandler<GetStudentQuery, StudentResult>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IFieldAccessPolicyService _fieldAccessPolicyService;
    private readonly ICurrentUserService _currentUserService;

    public GetStudentQueryHandler(
        IStudentRepository studentRepository,
        IFieldAccessPolicyService fieldAccessPolicyService,
        ICurrentUserService currentUserService)
    {
        _studentRepository = studentRepository;
        _fieldAccessPolicyService = fieldAccessPolicyService;
        _currentUserService = currentUserService;
    }

    public async Task<StudentResult> Handle(GetStudentQuery request, CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByIdAsync(new StudentId(request.StudentId), cancellationToken);
        if (student == null)
        {
            throw new NotFoundException($"Student with id {request.StudentId} not found.");
        }

        var role = _currentUserService.Role;

        return new StudentResult
        {
            Id = student.Id.Value,
            Status = student.Status.ToString(),
            Name = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Name)) == FieldAccess.Allowed ? student.Name : null,
            Cpf = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Cpf)) == FieldAccess.Allowed ? student.Cpf : null,
            Rg = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Rg)) == FieldAccess.Allowed ? student.Rg : null,
            Email = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Email)) == FieldAccess.Allowed ? student.Email : null,
            Phone = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Phone)) == FieldAccess.Allowed ? student.Phone : null
        };
    }
}
