using System.Net;
using ResourceHunterBonusScout.Tests.Site;

namespace ResourceHunterBonusScout.Tests;

public class UpdateCheckerTests
{
    [Theory]
    [InlineData("2", "3", true)]
    [InlineData("3", "3", false)]
    [InlineData("4", "3", false)]
    [InlineData("3-ci.99", "3", true)]
    [InlineData("3-ci.99+abc123", "3", true)]
    [InlineData("3-ci.99", "2", false)]
    public async Task AnnouncesNewerRelease(string current, string latest, bool announced)
    {
        var output = new StringWriter();
        var handler = new StubHttpHandler(HttpStatusCode.OK, $$"""{"tag_name":"v{{latest}}","html_url":"https://example.test/r"}""");
        var checker = new UpdateChecker(new HttpClient(handler), new Log(output, TimeProvider.System));

        await checker.CheckAsync(current, TestContext.Current.CancellationToken);

        Assert.Equal(announced, output.ToString().Contains("https://example.test/r"));
    }

    [Fact]
    public async Task StaysQuietWhenGitHubIsUnreachable()
    {
        var output = new StringWriter();
        var checker = new UpdateChecker(new HttpClient(new StubHttpHandler(new HttpRequestException())), new Log(output, TimeProvider.System));

        await checker.CheckAsync("1", TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, output.ToString());
    }
}
