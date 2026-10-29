namespace ResourceHunterBonusScout;

/// <summary>Whether to explain how to pin the tray icon, from two undocumented Windows 11 registry values.</summary>
public static class TrayIconHint
{
    /// <param name="chevronVisibility"><c>SystemTrayChevronVisibility</c>: 1 if the overflow menu is on. Null if missing.</param>
    /// <param name="isPromoted">This app's <c>IsPromoted</c>: 1 if the icon is pinned. Null if missing, like on first run.</param>
    public static bool ShouldShow(int? chevronVisibility, int? isPromoted)
        => chevronVisibility != 0 && isPromoted != 1;
}
