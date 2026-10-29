namespace ResourceHunterBonusScout.Tests;

public class ServerListsTests
{
    [Fact]
    public void FindsParentZone()
    {
        var subject = new ServerLists([], ["Deepest Guk"]);
        var result = subject.FindZone("Deepest Guk: The Rescue");
        Assert.Equal("Deepest Guk", result);
    }
}
