namespace CFCHub.Application.Common.Security;

public interface IDataProtectionService
{
    string Encrypt(string plaintext, string tenantId);
    string Decrypt(string ciphertext, string tenantId);
}
