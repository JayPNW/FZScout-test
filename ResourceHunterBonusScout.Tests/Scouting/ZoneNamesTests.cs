using ResourceHunterBonusScout.Scouting;

namespace ResourceHunterBonusScout.Tests.Scouting;

public class ZoneNamesTests
{
    static readonly HashSet<string> SiteZones =
    [
        "Befallen",
        "Chardok",
        "Halls of Betrayal",
        "Lavaspinner's Lair",
        "Crushbone",
        "Nagafen's Lair (SolB)",
    ];

    [Fact]
    public void SameName()
    {
        Assert.Equal(new KnownZone("Befallen"), ZoneNames.Resolve("Befallen", SiteZones));
    }

    [Fact]
    public void DifferentName()
    {
        Assert.Equal(new KnownZone("Crushbone"), ZoneNames.Resolve("Clan Crushbone", SiteZones));
        Assert.Equal(new KnownZone("Nagafen's Lair (SolB)"), ZoneNames.Resolve("Nagafen's Lair", SiteZones));
    }

    [Fact]
    public void LeadingTheInTheLogIsOptional()
    {
        Assert.Equal(new KnownZone("Halls of Honor"), ZoneNames.Resolve("The Halls of Honor", new HashSet<string> { "Halls of Honor" }));
    }

    [Fact]
    public void LeadingTheOnTheSiteIsOptional()
    {
        Assert.Equal(new KnownZone("The Nexus"), ZoneNames.Resolve("Nexus", new HashSet<string> { "The Nexus" }));
    }

    [Fact]
    public void ExactNameWinsOverTheOptionalThe()
    {
        Assert.Equal(new KnownZone("The Deep"), ZoneNames.Resolve("The Deep", new HashSet<string> { "Deep", "The Deep" }));
    }

    [Fact]
    public void MappedNameCanAlsoDropThe()
    {
        Assert.Equal(new KnownZone("Upper Guk"), ZoneNames.Resolve("The City of Guk", new HashSet<string> { "Upper Guk" }));
    }

    [Fact]
    public void ZoneWithAColonThatIsNotAnInstance()
    {
        Assert.Equal(new KnownZone("Halls of Betrayal"), ZoneNames.Resolve("Chardok: The Halls of Betrayal", SiteZones));
    }

    [Theory]
    [InlineData("Lavaspinner's Lair: Lavaspinner's Locals")]
    [InlineData("Lavaspinner's Lair: The Drake Menace")]
    public void InstanceReportsAsItsParent(string instance)
    {
        Assert.Equal(new KnownZone("Lavaspinner's Lair"), ZoneNames.Resolve(instance, SiteZones));
    }

    [Theory]
    [InlineData("The Plane of Tranquility")]
    [InlineData("The Plane of Knowledge")]
    [InlineData("The Guild Lobby")]
    [InlineData("Guild Hall")]
    public void ZonesThatNeverHaveABonus(string zone)
    {
        Assert.Equal(new NoBonusZone(zone), ZoneNames.Resolve(zone, SiteZones));
    }

    [Fact]
    public void NotOnTheSiteList()
    {
        Assert.Equal(new UnknownZone("The Halls of Honor"), ZoneNames.Resolve("The Halls of Honor", SiteZones));
    }

    [Fact]
    public void MappedButNotOnThisServersList()
    {
        Assert.Equal(new UnknownZone("The Burning Woods"), ZoneNames.Resolve("The Burning Woods", SiteZones));
    }

    [Fact]
    public void MappedZoneWithAColonIsNotTreatedAsAnInstance()
    {
        Assert.Equal(
            new UnknownZone("Chardok: The Halls of Betrayal"),
            ZoneNames.Resolve("Chardok: The Halls of Betrayal", new HashSet<string> { "Chardok" }));
    }

    [Fact]
    public void WithoutASiteListEveryZoneIsUnresolved()
    {
        Assert.Equal(new UnresolvedZone("Befallen"), ZoneNames.Resolve("Befallen", null));
    }

    [Fact]
    public void WithoutASiteListNoBonusZonesAreStillKnown()
    {
        Assert.Equal(new NoBonusZone("The Guild Lobby"), ZoneNames.Resolve("The Guild Lobby", null));
    }
}
