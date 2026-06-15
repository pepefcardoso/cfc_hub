namespace CFCHub.Domain.Shared.Exceptions;

public class SchedulingConflictException : ConflictException
{
    public SchedulingConflictException(string message, string errorCode = "SCHEDULING_CONFLICT") 
        : base(message, errorCode)
    {
    }
}
