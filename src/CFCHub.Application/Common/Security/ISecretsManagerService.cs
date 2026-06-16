using System;

namespace CFCHub.Application.Common.Security;

public interface ISecretsManagerService
{
    string? GetSecret(string secretName);
}
