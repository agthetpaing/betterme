using BetterMe.Shared.Validation;

namespace BetterMe.Tests;

public class MeetingUrlTests
{
    [Theory]
    [InlineData("https://meet.example/x", true)]
    [InlineData("https://teams.microsoft.com/l/meetup-join/abc", true)]
    [InlineData("http://meet.example/x", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("/relative/path", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsAllowed(string? url, bool expected)
    {
        Assert.Equal(expected, MeetingUrl.IsAllowed(url));
    }
}
