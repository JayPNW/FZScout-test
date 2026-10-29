using ResourceHunterBonusScout.Logs;

namespace ResourceHunterBonusScout.Tests;

public class LogEventTests
{
    [Fact]
    public void ZoneEntry()
    {
        Assert.Equal(new LogEvent.ZoneEntered("The Halls of Honor"), LogEvent.Parse("[Mon Sep 14 16:10:29 2026] You have entered The Halls of Honor."));
    }

    [Theory]
    [InlineData("You have entered an area where levitation effects do not function.")]
    [InlineData("You have entered an area where Bind Affinity is allowed.")]
    public void AreaInsideAZoneIsNotAZoneEntry(string line)
    {
        Assert.Null(LogEvent.Parse(line));
    }

    [Theory]
    [InlineData("improving the experiences of all who venture here.", "experience")]
    [InlineData("improving the experience tied to abilities for all who venture here.", "aa")]
    [InlineData("multiplying the loot that may be found here.", "loot")]
    [InlineData("multiplying the coin that may be found here.", "coin")]
    [InlineData("which has drawn many creatures of exceptional rarity here.", "rare")]
    [InlineData("magnifying the recognition one may receive from their adventures here.", "faction")]
    [InlineData("which has drawn numerous mundane creatures here.", "respawn")]
    [InlineData("enhancing the benefits of offerings for all who venture here.", "tribute")]
    [InlineData("enhancing the potency of any offerings made here.", "tribute")]
    [InlineData("improving the rate at which proficiency is gained while adventuring here.", "tradeskill")]
    public void BonusMessage(string ending, string bonusKey)
    {
        Assert.Equal(new LogEvent.BonusMessage(bonusKey), LogEvent.Parse($"[Mon Sep 14 16:10:29 2026] This land is saturated with unstable magic, {ending}"));
    }

    [Fact]
    public void YourKill()
    {
        Assert.IsType<LogEvent.Kill>(LogEvent.Parse("[Mon Sep 14 16:10:29 2026] You have slain a Stillmoon leopard!"));
    }

    [Theory]
    [InlineData("A stormrider was slain by Bowslinger!")]
    [InlineData("Kekab was slain by A jord militis!")]
    [InlineData("  was slain by Gibarn!")]
    [InlineData("Kixu has been slain by an elite kyv hunter!")]
    public void OtherDeathsAreNotKills(string line)
    {
        Assert.Null(LogEvent.Parse(line));
    }

    [Fact]
    public void Camping()
    {
        Assert.IsType<LogEvent.CampStarted>(LogEvent.Parse("It will take you about 30 seconds to prepare your camp."));
        Assert.Null(LogEvent.Parse("It will take about 25 more seconds to prepare your camp."));
        Assert.IsType<LogEvent.CampAbandoned>(LogEvent.Parse("You abandon your preparations to camp."));
    }

    [Fact]
    public void DailyReset()
    {
        Assert.IsType<LogEvent.DailyReset>(LogEvent.Parse(
            "[Tue Sep 15 00:00:03 2026] Fonts of unstable magic are erupting and receding in response to some nameless power. The magic is cascading throughout Norrath and into the Planes beyond. Hunt for the lands that have been most recently saturated with this magic to reap their many and varied resources."));
    }

    [Theory]
    [InlineData("[Mon Sep 14 16:10:29 2026] Welcome to EverQuest!")]
    [InlineData("Bowslinger tells the guild, 'You have entered The Halls of Honor'")]
    [InlineData("")]
    public void OtherLinesDontMatter(string line)
    {
        Assert.Null(LogEvent.Parse(line));
    }
}
