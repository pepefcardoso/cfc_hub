using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Common.Security;

public interface ISecretsManagerService
{
    Task<string?> GetSecretAsync(string arn, CancellationToken ct = default);
}
