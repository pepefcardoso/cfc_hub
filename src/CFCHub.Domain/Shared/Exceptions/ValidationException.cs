namespace CFCHub.Domain.Shared.Exceptions;

public class ValidationException : CfcHubException
{
    public override int StatusCode => 400;

    public ValidationException(string message, string errorCode = "VALIDATION_ERROR") 
        : base(message, errorCode)
    {
    }
}
