namespace CFCHub.Domain.Shared;

public enum ErrorType
{
    NotFound,
    Conflict,
    Validation,
    Unauthorized,
    Forbidden,
    Unexpected
}

public record Error(string Code, string Description, ErrorType Type)
{
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
    public static Error Unexpected(string code, string description) => new(code, description, ErrorType.Unexpected);
}
