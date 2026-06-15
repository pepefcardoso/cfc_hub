namespace CFCHub.Domain.Shared.Exceptions;

public class StorageException : InfrastructureException
{
    public StorageException(string message, string errorCode = "STORAGE_ERROR") 
        : base(message, errorCode)
    {
    }
}
