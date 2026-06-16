using CFCHub.Application.Common.Pagination;
using CFCHub.Domain.Shared;

namespace CFCHub.Application.Identity.Queries.GetStaffUsers;

public record GetStaffUsersQuery(
    string? Cursor = null,
    int Limit = 20) : PaginatedQuery<PagedResult<StaffUserResult>>(Cursor, Limit);
