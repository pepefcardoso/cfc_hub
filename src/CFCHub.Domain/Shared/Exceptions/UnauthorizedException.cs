namespace CFCHub.Domain.Shared.Exceptions;

public class UnauthorizedException : CfcHubException
{
    public override int StatusCode => 401;

    public UnauthorizedException(string message, string errorCode = "UNAUTHORIZED") 
        : base(message, errorCode)
    {
    }
}
