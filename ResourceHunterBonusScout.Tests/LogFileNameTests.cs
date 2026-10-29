using ResourceHunterBonusScout.Logs;

namespace ResourceHunterBonusScout.Tests;

public class LogFileNameTests
{
    [Fact]
    public void CharacterAndServer()
    {
        Assert.Equal(("Bowslinger", Server.Fangbreaker), LogFileName.Parse(@"C:\EverQuest\Logs\eqlog_Bowslinger_fangbreaker.txt"));
        Assert.Equal(("Bowslinger", Server.Frostreaver), LogFileName.Parse(@"C:\EverQuest\Logs\eqlog_Bowslinger_FrostReaver.txt"));
    }

    [Theory]
    [InlineData(@"C:\EverQuest\Logs\eqlog_Bowslinger_vaniki.txt")]
    [InlineData(@"C:\EverQuest\Logs\eqlog_Bowslinger_fangbreaker.txt.bak")]
    [InlineData(@"C:\EverQuest\Logs\dbg.txt")]
    public void OtherFilesAreIgnored(string path)
    {
        Assert.Null(LogFileName.Parse(path));
    }
}
