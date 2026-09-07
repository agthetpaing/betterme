namespace BetterMe.Shared.Validation;

public static class MeetingUrl
{
    public static bool IsAllowed(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps;
    }
}
