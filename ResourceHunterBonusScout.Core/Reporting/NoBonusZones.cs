namespace ResourceHunterBonusScout.Reporting;

/// <summary>Zones that never have a bonus, so they aren't on any server's zones list.</summary>
public static class NoBonusZones
{
    static readonly HashSet<string> Zones = new(StringComparer.OrdinalIgnoreCase)
    {
        "The Plane of Tranquility",
        "The Plane of Knowledge",
        "The Guild Lobby",
        "Guild Hall",
    };

    /// <param name="logZone">The zone name as the log spells it.</param>
    public static bool Contains(string logZone) => Zones.Contains(logZone);
}
