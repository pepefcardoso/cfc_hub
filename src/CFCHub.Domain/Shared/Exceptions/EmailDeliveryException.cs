namespace CFCHub.Domain.Shared.Exceptions;

public class EmailDeliveryException : InfrastructureException
{
    public EmailDeliveryException(string message, string errorCode = "EMAIL_DELIVERY_ERROR") 
        : base(message, errorCode)
    {
    }
}
