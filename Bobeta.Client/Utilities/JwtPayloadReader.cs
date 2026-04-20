using System.Text.Json;

namespace Bobeta.Client.Utilities;

/// <summary>Reads standard JWT claims from an unparsed token string (no signature verification — UI hints only).</summary>
public static class JwtPayloadReader
{
    /// <summary>Returns UTC expiry from the <c>exp</c> claim, or null if missing or malformed.</summary>
    public static DateTimeOffset? TryGetExpiryUtc(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return null;
        var parts = jwt.Split('.');
        if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
            return null;
        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(payloadBytes);
            if (!doc.RootElement.TryGetProperty("exp", out var expEl))
                return null;
            long unix = expEl.ValueKind == JsonValueKind.Number && expEl.TryGetInt64(out var n)
                ? n
                : long.TryParse(expEl.GetString(), out var parsed) ? parsed : 0;
            if (unix <= 0)
                return null;
            return DateTimeOffset.FromUnixTimeSeconds(unix);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>True when <paramref name="jwt"/> has a parsable <c>exp</c> and <paramref name="utcNow"/> is at or past that instant.</summary>
    public static bool IsExpired(string? jwt, DateTimeOffset utcNow) =>
        TryGetExpiryUtc(jwt) is { } exp && utcNow >= exp;

    private static byte[] Base64UrlDecode(string segment)
    {
        var padded = segment.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}
