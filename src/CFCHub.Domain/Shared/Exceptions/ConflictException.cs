namespace CFCHub.Domain.Shared.Exceptions;

public class ConflictException : CfcHubException
{
    public override int StatusCode => 409;

    public ConflictException(string message, string errorCode = "CONFLICT") 
        : base(message, errorCode)
    {
    }
}
