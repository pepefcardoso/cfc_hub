namespace CFCHub.Domain.Shared.Exceptions;

public record ValidationFailureDetail(string PropertyName, string ErrorMessage, string ErrorCode);

public class ValidationException : CfcHubException
{
    public override int StatusCode => 400;

    public IReadOnlyCollection<ValidationFailureDetail> Errors { get; }

    public ValidationException(string message, string errorCode = "VALIDATION_ERROR") 
        : base(message, errorCode)
    {
        Errors = Array.Empty<ValidationFailureDetail>();
    }

    public ValidationException(IReadOnlyCollection<ValidationFailureDetail> errors) 
        : base("One or more validation failures have occurred.", "VALIDATION_ERROR")
    {
        Errors = errors;
    }
}
