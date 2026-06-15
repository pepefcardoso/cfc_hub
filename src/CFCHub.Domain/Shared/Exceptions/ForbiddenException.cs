namespace CFCHub.Domain.Shared.Exceptions;

public class ForbiddenException : CfcHubException
{
    public override int StatusCode => 403;

    public ForbiddenException(string message, string errorCode = "FORBIDDEN") 
        : base(message, errorCode)
    {
    }
}
