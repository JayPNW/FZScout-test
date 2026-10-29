namespace ResourceHunterBonusScout.Tests;

public class AppStatusTests
{
    static readonly CharacterStatus Bowslinger = new("Bowslinger", "Fangbreaker", "The Halls of Honor");

    [Fact]
    public void IdleWhenNoLogIsTailed()
    {
        Assert.Equal(TrayIconState.Idle, new AppStatus([], HasApiKey: true, LastOperationFailed: false, EqRunning: true).IconState);
    }

    [Fact]
    public void ActiveWhenALogIsTailed()
    {
        Assert.Equal(TrayIconState.Active, new AppStatus([Bowslinger], HasApiKey: true, LastOperationFailed: false, EqRunning: true).IconState);
    }

    [Fact]
    public void WaitingWhenACharacterIsWaitingForBonus()
    {
        var waiting = Bowslinger with { NoBonusAt = DateTimeOffset.UnixEpoch };

        Assert.Equal(TrayIconState.Waiting, new AppStatus([Bowslinger, waiting], HasApiKey: true, LastOperationFailed: false, EqRunning: true).IconState);
    }

    [Fact]
    public void ErrorWinsOverWaiting()
    {
        var waiting = Bowslinger with { NoBonusAt = DateTimeOffset.UnixEpoch };

        Assert.Equal(TrayIconState.Error, new AppStatus([waiting], HasApiKey: true, LastOperationFailed: true, EqRunning: true).IconState);
    }

    [Fact]
    public void IdleWhenEveryCharacterIsInAZoneThatNeverHasABonus()
    {
        var inPoK = Bowslinger with { Zone = "The Plane of Knowledge", NeverHasBonus = true };

        Assert.Equal(TrayIconState.Idle, new AppStatus([inPoK], HasApiKey: true, LastOperationFailed: false, EqRunning: true).IconState);
        Assert.Equal(TrayIconState.Active, new AppStatus([inPoK, Bowslinger], HasApiKey: true, LastOperationFailed: false, EqRunning: true).IconState);
    }

    [Fact]
    public void ErrorWhenNoKey()
    {
        Assert.Equal(TrayIconState.Error, new AppStatus([Bowslinger], HasApiKey: false, LastOperationFailed: false, EqRunning: true).IconState);
    }

    [Fact]
    public void ErrorWhenLastOperationFailed()
    {
        Assert.Equal(TrayIconState.Error, new AppStatus([Bowslinger], HasApiKey: true, LastOperationFailed: true, EqRunning: true).IconState);
    }

    [Fact]
    public void ToolTipSaysWhenEqIsNotRunning()
    {
        Assert.Equal("Resource Hunter Bonus Scout\nEverQuest is not running.", new AppStatus([Bowslinger], HasApiKey: true, LastOperationFailed: false, EqRunning: false).ToolTip);
    }

    [Fact]
    public void ToolTipSaysWhenNoLogIsTailed()
    {
        Assert.Equal("Resource Hunter Bonus Scout\nNo EverQuest log is being watched.", new AppStatus([], HasApiKey: true, LastOperationFailed: false, EqRunning: true).ToolTip);
    }

    [Fact]
    public void ToolTipListsEachCharactersZone()
    {
        CharacterStatus lost = new("Lostling", "Fangbreaker", Zone: null);

        Assert.Equal("Resource Hunter Bonus Scout\nThe Halls of Honor\nunknown zone", new AppStatus([Bowslinger, lost], HasApiKey: true, LastOperationFailed: false, EqRunning: true).ToolTip);
    }

    [Fact]
    public void ToolTipSaysWhenWaitingForBonus()
    {
        var at = new DateTimeOffset(2026, 10, 29, 22, 42, 10, TimeSpan.Zero);
        var waiting = Bowslinger with { Zone = "Crushbone", NoBonusAt = at };

        Assert.Equal($"Resource Hunter Bonus Scout\nCrushbone, waiting for bonus until {at.ToLocalTime():T}", new AppStatus([waiting], HasApiKey: true, LastOperationFailed: false, EqRunning: true).ToolTip);
    }

    [Fact]
    public void ToolTipIsTruncatedTo127Characters()
    {
        var characters = Enumerable.Repeat(Bowslinger, 10).ToList();

        var toolTip = new AppStatus(characters, HasApiKey: true, LastOperationFailed: false, EqRunning: true).ToolTip;

        Assert.Equal(127, toolTip.Length);
        Assert.EndsWith("…", toolTip);
    }
}
