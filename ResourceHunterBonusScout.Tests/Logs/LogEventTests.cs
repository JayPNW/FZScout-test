using ResourceHunterBonusScout.Logs;

namespace ResourceHunterBonusScout.Tests.Logs;

public class LogEventTests
{
    [Fact]
    public void ZoneEntered()
    {
        Assert.Equal(
            new ZoneEnteredLogEvent("The Halls of Honor"),
            LogEvent.Parse("[Mon Sep 14 16:10:29 2026] You have entered The Halls of Honor."));
    }

    [Fact]
    public void ZoneWithColonAndApostrophe()
    {
        Assert.Equal(
            new ZoneEnteredLogEvent("Lavaspinner's Lair: The Drake Menace"),
            LogEvent.Parse("[Mon Sep 14 16:10:29 2026] You have entered Lavaspinner's Lair: The Drake Menace."));
    }

    [Theory]
    [InlineData("You have entered an area where levitation effects do not function.")]
    [InlineData("You have entered an area where Bind Affinity is allowed.")]
    public void AreaInsideAZoneIsNotAZone(string message)
    {
        Assert.Null(LogEvent.Parse($"[Mon Sep 14 16:10:29 2026] {message}"));
    }

    [Fact]
    public void PlayerKill()
    {
        Assert.Equal(new KillLogEvent(), LogEvent.Parse("[Mon Sep 14 16:10:29 2026] You have slain a Stillmoon leopard!"));
    }

    [Theory]
    [InlineData("A stormrider was slain by Bowslinger!")]
    [InlineData("Kekab was slain by A jord militis!")]
    [InlineData("Kixu has been slain by an elite kyv hunter!")]
    public void OtherKillsDontCount(string message)
    {
        Assert.Null(LogEvent.Parse($"[Mon Sep 14 16:10:29 2026] {message}"));
    }

    [Theory]
    [InlineData("enhancing the benefits of offerings for all who venture here.", "tribute")]
    [InlineData("enhancing the potency of any offerings made here.", "tribute")]
    [InlineData("improving the experience tied to abilities for all who venture here.", "aa")]
    [InlineData("improving the experiences of all who venture here.", "experience")]
    [InlineData("improving the rate at which proficiency is gained while adventuring here.", "tradeskill")]
    [InlineData("magnifying the recognition one may receive from their adventures here.", "faction")]
    [InlineData("multiplying the coin that may be found here.", "coin")]
    [InlineData("multiplying the loot that may be found here.", "loot")]
    [InlineData("which has drawn many creatures of exceptional rarity here.", "rare")]
    [InlineData("which has drawn numerous mundane creatures here.", "respawn")]
    public void Bonus(string ending, string key)
    {
        Assert.Equal(
            new BonusReportLogEvent(new BonusKey(key)),
            LogEvent.Parse($"[Mon Sep 14 16:10:29 2026] This land is saturated with unstable magic, {ending}"));
    }

    [Fact]
    public void UnrecognizedBonusIsIgnored()
    {
        Assert.Null(LogEvent.Parse("[Mon Sep 14 16:10:29 2026] This land is saturated with unstable magic, doing something new."));
    }

    [Fact]
    public void DailyReset()
    {
        Assert.Equal(
            new DailyResetLogEvent(),
            LogEvent.Parse("[Mon Sep 14 16:10:29 2026] Fonts of unstable magic are erupting and receding in response to some nameless power. The magic is cascading throughout Norrath and into the Planes beyond. Hunt for the lands that have been most recently saturated with this magic to reap their many and varied resources."));
    }

    [Theory]
    [InlineData("It will take you about 30 seconds to prepare your camp.")]
    [InlineData("It will take about 25 more seconds to prepare your camp.")]
    public void Camping(string message)
    {
        Assert.Equal(new CampingLogEvent(), LogEvent.Parse($"[Mon Sep 14 16:10:29 2026] {message}"));
    }

    [Fact]
    public void CampAbandoned()
    {
        Assert.Equal(new CampAbandonedLogEvent(), LogEvent.Parse("[Mon Sep 14 16:10:29 2026] You abandon your preparations to camp."));
    }

    [Fact]
    public void OtherLinesAreIgnored()
    {
        Assert.Null(LogEvent.Parse("[Mon Sep 14 16:10:29 2026] Welcome to EverQuest!"));
    }

    [Fact]
    public void StripTimestamp()
    {
        Assert.Equal("You have slain a rat!", LogLine.StripTimestamp("[Mon Sep 14 16:10:29 2026] You have slain a rat!"));
    }

    [Fact]
    public void LineWithoutTimestampIsUnchanged()
    {
        Assert.Equal("You have slain a rat!", LogLine.StripTimestamp("You have slain a rat!"));
    }
}
