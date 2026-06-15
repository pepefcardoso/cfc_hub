using System;

namespace CFCHub.Domain.Shared.Exceptions;

public abstract class CfcHubException : Exception
{
    public string ErrorCode { get; }
    public abstract int StatusCode { get; }

    protected CfcHubException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}
