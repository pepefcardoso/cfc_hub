namespace CFCHub.Domain.Shared.Exceptions;

public class NotFoundException : CfcHubException
{
    public override int StatusCode => 404;

    public NotFoundException(string message, string errorCode = "NOT_FOUND") 
        : base(message, errorCode)
    {
    }
}
