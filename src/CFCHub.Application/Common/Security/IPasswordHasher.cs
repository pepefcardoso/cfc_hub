namespace CFCHub.Application.Common.Security;

public interface IPasswordHasher
{
    bool Verify(string password, string passwordHash);
}
