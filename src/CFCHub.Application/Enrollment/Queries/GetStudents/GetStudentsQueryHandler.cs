using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Enrollment.Queries.GetStudents;

public class GetStudentsQueryHandler : IRequestHandler<GetStudentsQuery, PagedResult<GetStudent.StudentResult>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IFieldAccessPolicyService _fieldAccessPolicyService;
    private readonly ICurrentUserService _currentUserService;

    public GetStudentsQueryHandler(
        IStudentRepository studentRepository,
        IFieldAccessPolicyService fieldAccessPolicyService,
        ICurrentUserService currentUserService)
    {
        _studentRepository = studentRepository;
        _fieldAccessPolicyService = fieldAccessPolicyService;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<GetStudent.StudentResult>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var pagedStudents = await _studentRepository.ListAsync(request.PageSize, request.Cursor, cancellationToken);
        
        var role = _currentUserService.Role;

        var results = pagedStudents.Items.Select(student => new GetStudent.StudentResult
        {
            Id = student.Id.Value,
            Status = student.Status.ToString(),
            Name = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Name)) == FieldAccess.Allowed ? student.Name : null,
            Cpf = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Cpf)) == FieldAccess.Allowed ? student.Cpf : null,
            Rg = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Rg)) == FieldAccess.Allowed ? student.Rg : null,
            Email = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Email)) == FieldAccess.Allowed ? student.Email : null,
            Phone = _fieldAccessPolicyService.CheckAccess(role, nameof(student.Phone)) == FieldAccess.Allowed ? student.Phone : null
        }).ToList();

        return new PagedResult<GetStudent.StudentResult>(results, pagedStudents.NextCursor, pagedStudents.HasMore, pagedStudents.Count);
    }
}
