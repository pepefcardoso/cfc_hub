using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Pagination;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Identity.Queries.GetStaffUsers;

public class GetStaffUsersQueryHandler : IRequestHandler<GetStaffUsersQuery, PagedResult<StaffUserResult>>
{
    private readonly IStaffUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFieldAccessPolicyService _fieldAccessPolicyService;

    public GetStaffUsersQueryHandler(
        IStaffUserRepository userRepository,
        ICurrentUserService currentUserService,
        IFieldAccessPolicyService fieldAccessPolicyService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _fieldAccessPolicyService = fieldAccessPolicyService;
    }

    public async Task<PagedResult<StaffUserResult>> Handle(GetStaffUsersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != RoleType.Admin)
        {
            throw new ForbiddenException("Apenas administradores podem visualizar a lista de usuários.", "FORBIDDEN");
        }

        var cursor = request.Cursor != null ? CursorEncoder.Decode(request.Cursor) : null;
        var pagedUsers = await _userRepository.ListAsync(cursor, request.Limit, cancellationToken);

        var canReadEmail = _fieldAccessPolicyService.CheckAccess(_currentUserService.Role, "StaffUser.Email") == FieldAccess.Allowed;

        var resultItems = pagedUsers.Items.Select(u => new StaffUserResult(
            u.Id.Value,
            u.Name,
            canReadEmail ? u.Email : null,
            u.Role,
            u.Status,
            u.LastLoginAt)).ToList();

        return new PagedResult<StaffUserResult>(
            resultItems,
            pagedUsers.NextCursor,
            pagedUsers.HasMore,
            pagedUsers.Count);
    }
}
