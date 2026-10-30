namespace ResourceHunterBonusScout.Scouting;

/// <summary>Turns the zone name in the log into the site's zone name. See InternalDocumentation/Zones.md.</summary>
static class ZoneNames
{

    static readonly HashSet<string> NeverHaveABonus =
    [
        "The Plane of Tranquility",
        "The Plane of Knowledge",
        "Guild Lobby",
        "Guild Hall",
    ];

    public static readonly Dictionary<string, string> LogToSite = new()
    {
        ["Cabilis East"] = "Cabilis",
        ["Cabilis West"] = "Cabilis",
        ["Chardok: The Halls of Betrayal"] = "Halls of Betrayal",
        ["Clan Crushbone"] = "Crushbone",
        ["Kael Drakkel"] = "Kael Drakkal",
        ["Neriak - Commons"] = "Neriak",
        ["Neriak - Foreign Quarter"] = "Neriak",
        ["Neriak - Third Gate"] = "Neriak",
        ["Nagafen's Lair"] = "Nagafen's Lair (SolB)",
        ["North Desert of Ro"] = "North Ro",
        ["North Kaladim"] = "Kaladim",
        ["Northern Felwithe"] = "Felwithe",
        ["South Desert of Ro"] = "South Ro",
        ["South Kaladim"] = "Kaladim",
        ["Southern Felwithe"] = "Felwithe",
        ["The Castle of Mistmoore"] = "Castle Mistmoore",
        ["The City of Guk"] = "Upper Guk",
        ["The City of Thurgadin"] = "Thurgadin",
        ["The Crypt of Dalnir"] = "Dalnir",
        ["The Eastern Plains of Karana"] = "East Karana",
        ["The Liberated Citadel of Runnyeye"] = "Clan Runnyeye",
        ["The Northern Plains of Karana"] = "North Karana",
        ["The Ruins of Old Guk"] = "Lower Guk",
        ["The Ruins of Old Paineel"] = "The Hole",
        ["The Ruins of Sebilis"] = "Old Sebilis",
        ["The Qeynos Aqueduct System"] = "Qeynos Catacombs",
        ["The Southern Plains of Karana"] = "South Karana",
        ["The Warsliks Woods"] = "Warsliks Wood",
        ["The Western Plains of Karana"] = "West Karana",
    };

    /// <param name="siteZones">The server's zones list, or null if it couldn't be loaded.</param>
    public static Zone Resolve(string logZone, IReadOnlySet<string>? siteZones)
    {
        if (NeverHaveABonus.Contains(logZone))
        {
            return new NoBonusZone(logZone);
        }

        if (siteZones is null)
        {
            return new UnresolvedZone(logZone);
        }

        var siteName = SiteName(logZone, siteZones);
        if (siteName is null && !LogToSite.ContainsKey(logZone))
        {
            var colon = logZone.IndexOf(':');
            if (colon > 0)
            {
                // An instance, like "Lavaspinner's Lair: The Drake Menace", reports as its parent zone.
                siteName = SiteName(logZone[..colon], siteZones);
            }
        }

        if (siteName is null)
        {
            return new UnknownZone(logZone);
        }

        return new KnownZone(siteName);
    }

    static string? SiteName(string logZone, IReadOnlySet<string> siteZones)
    {
        var candidate = LogToSite.GetValueOrDefault(logZone, logZone);
        return AddOrRemoveThePrefix(candidate).FirstOrDefault(siteZones.Contains);
    }

    /// <summary>A leading "The " is optional: "The Burning Woods" in the log is "Burning Woods" on the site, and the other way around.</summary>
    static IEnumerable<string> AddOrRemoveThePrefix(string name)
    {
        const string The = "The ";

        yield return name;
        if (name.StartsWith(The, StringComparison.Ordinal))
        {
            yield return name[The.Length..];
        }
        else
        {
            yield return $"{The}{name}";
        }
    }
}
