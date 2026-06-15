using CFCHub.Domain.Shared.Pagination;

namespace CFCHub.Application.Common.Pagination;

public static class CursorEncoder
{
    public static string Encode(Guid id, DateTimeOffset ts)
    {
        var cursor = new Cursor(id, ts);
        return cursor.Encode();
    }

    public static Cursor Decode(string encoded)
    {
        return Cursor.Parse(encoded);
    }
}
