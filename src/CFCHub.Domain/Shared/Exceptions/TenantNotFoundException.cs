namespace CFCHub.Domain.Shared.Exceptions;

public class TenantNotFoundException : NotFoundException
{
    public TenantNotFoundException(string message, string errorCode = "TENANT_NOT_FOUND") 
        : base(message, errorCode)
    {
    }
}
