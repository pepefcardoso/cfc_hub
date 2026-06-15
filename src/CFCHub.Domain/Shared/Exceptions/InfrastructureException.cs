namespace CFCHub.Domain.Shared.Exceptions;

public class InfrastructureException : CfcHubException
{
    public override int StatusCode => 500;

    public InfrastructureException(string message, string errorCode = "INTERNAL_ERROR") 
        : base(message, errorCode)
    {
    }
}
