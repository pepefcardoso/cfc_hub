using System.Text.Json;
using System.Text.Json.Serialization;
using CFCHub.Domain.Shared.Exceptions;

namespace CFCHub.Domain.Shared.Pagination;

public record Cursor
{
    public Guid Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public Cursor(Guid id, DateTimeOffset timestamp)
    {
        Id = id;
        Timestamp = timestamp;
    }

    public static Cursor Parse(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
            throw new ValidationException("Cursor cannot be empty.", "INVALID_CURSOR");

        try
        {
            var bytes = Convert.FromBase64String(encoded);
            var utf8String = System.Text.Encoding.UTF8.GetString(bytes);
            var cursor = JsonSerializer.Deserialize<Cursor>(utf8String);
            
            if (cursor is null || cursor.Id == Guid.Empty)
                throw new ValidationException("Malformed cursor data.", "INVALID_CURSOR");
                
            return cursor;
        }
        catch (Exception ex) when (ex is FormatException || ex is JsonException)
        {
            throw new ValidationException("Invalid cursor format.", "INVALID_CURSOR");
        }
    }

    public string Encode()
    {
        var utf8String = JsonSerializer.Serialize(this);
        var bytes = System.Text.Encoding.UTF8.GetBytes(utf8String);
        return Convert.ToBase64String(bytes);
    }
}
