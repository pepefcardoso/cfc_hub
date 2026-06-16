using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Common.Email;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
