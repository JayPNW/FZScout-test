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
    public void ErrorWhenNoKey()
    {
        Assert.Equal(TrayIconState.Error, new AppStatus([Bowslinger], HasApiKey: false, LastOperationFailed: false, EqRunning: true).IconState);
    }

    [Fact]
    public void ErrorWhenLastOperationFailed()
    {
        Assert.Equal(TrayIconState.Error, new AppStatus([Bowslinger], HasApiKey: true, LastOperationFailed: true, EqRunning: true).IconState);
    }
}
