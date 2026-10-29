namespace ResourceHunterBonusScout.Tests;

public class TrayIconHintTests
{
    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(1, null, true)]
    [InlineData(null, null, true)]
    [InlineData(1, 1, false)]
    [InlineData(0, 0, false)]
    [InlineData(0, null, false)]
    public void ShownWhenTheOverflowMenuIsOnAndTheIconIsNotPinned(int? chevronVisibility, int? isPromoted, bool expected)
    {
        Assert.Equal(expected, TrayIconHint.ShouldShow(chevronVisibility, isPromoted));
    }
}
