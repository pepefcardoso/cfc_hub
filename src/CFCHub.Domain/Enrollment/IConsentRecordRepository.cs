using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Domain.Enrollment;

public interface IConsentRecordRepository
{
    Task AddAsync(ConsentRecord consentRecord, CancellationToken cancellationToken = default);
}
