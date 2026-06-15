using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Pagination;

namespace CFCHub.Domain.Identity;

public interface IStaffUserRepository
{
    Task<StaffUser?> GetByIdAsync(StaffUserId id, CancellationToken cancellationToken = default);
    Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(StaffUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(StaffUser user, CancellationToken cancellationToken = default);
    Task<PagedResult<StaffUser>> ListAsync(Cursor? cursor, int limit, CancellationToken cancellationToken = default);
}
