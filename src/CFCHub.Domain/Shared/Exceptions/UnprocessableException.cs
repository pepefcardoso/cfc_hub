namespace CFCHub.Domain.Shared.Exceptions;

public class UnprocessableException : CfcHubException
{
    public override int StatusCode => 422;

    public UnprocessableException(string message, string errorCode = "UNPROCESSABLE") 
        : base(message, errorCode)
    {
    }
}
