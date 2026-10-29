namespace ResourceHunterBonusScout.Tests;

public class ServerListsTests
{
    readonly ServerLists lists = new(["experience"],
    [
        "Halls of Honor", "The Feerrott", "Thurgadin", "The Ruins of Illsalin", "Kael Drakkal",
        "Deepest Guk", "Deepest Guk: The Rescue", "Tirranun's Delve",
    ]);

    [Theory]
    [InlineData("The Feerrott", "The Feerrott")]
    [InlineData("the feerrott", "The Feerrott")]
    [InlineData("The Halls of Honor", "Halls of Honor")]
    [InlineData("Ruins of Illsalin", "The Ruins of Illsalin")]
    [InlineData("The City of Thurgadin", "Thurgadin")]
    [InlineData("Kael Drakkel", "Kael Drakkal")]
    [InlineData("Deepest Guk: The Rescue", "Deepest Guk: The Rescue")]
    [InlineData("Deepest Guk: Ritualist of Hate", "Deepest Guk")]
    [InlineData("Tirranun's Delve: Grounding the Drakes", "Tirranun's Delve")]
    public void FindsTheSiteSpelling(string logZone, string siteZone)
    {
        Assert.Equal(siteZone, lists.FindZone(logZone));
    }

    [Theory]
    [InlineData("The Bazaar")]
    [InlineData("Cabilis East")] // An alias, but Cabilis isn't on this list.
    [InlineData("Guild Hall: Bowslinger's Guild")]
    public void UnknownZone(string logZone)
    {
        Assert.Null(lists.FindZone(logZone));
    }
}
