namespace ResourceHunterBonusScout;

/// <summary>A server's <c>types</c> and <c>zones</c> lists from its site.</summary>
public class ServerLists
{
    /// <summary>Log spellings of zones whose site spelling is different. Used only if the site name is on the server's list.</summary>
    static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ruins of Illsalin"] = "The Ruins of Illsalin",
        ["Riftseekers' Sanctum"] = "Riftseeker's Sanctum",
        ["The City of Thurgadin"] = "Thurgadin",
        ["Kael Drakkel"] = "Kael Drakkal",
        ["Cabilis East"] = "Cabilis",
        ["Torden, the Bastion of Thunder"] = "Bastion of Thunder",
        ["Drunder, the Fortress of Zek"] = "Plane of Tactics",
        ["Ruins of Lxanvom"] = "Crypt of Decay",
    };

    readonly Dictionary<string, string> zones;

    public ServerLists(IEnumerable<string> bonusKeys, IEnumerable<string> zones)
    {
        BonusKeys = bonusKeys.ToHashSet();
        this.zones = zones.DistinctBy(z => z, StringComparer.OrdinalIgnoreCase).ToDictionary(z => z, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlySet<string> BonusKeys { get; }

    /// <param name="logZone">The zone name as the log spells it.</param>
    /// <returns>The site's spelling, or null when the zone isn't on the list.</returns>
    public string? FindZone(string logZone)
    {
        if (Find(logZone) is { } zone)
            return zone;

        // Instances follow their parent zone.
        var separator = logZone.IndexOf(": ", StringComparison.Ordinal);
        return separator < 0 ? null : Find(logZone[..separator]);
    }

    string? Find(string logZone)
    {
        if (zones.TryGetValue(logZone, out var zone))
            return zone;
        if (logZone.StartsWith("The ", StringComparison.OrdinalIgnoreCase) && zones.TryGetValue(logZone[4..], out zone))
            return zone;
        if (Aliases.TryGetValue(logZone, out var alias) && zones.TryGetValue(alias, out zone))
            return zone;
        return null;
    }
}
