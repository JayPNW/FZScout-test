namespace ResourceHunterBonusScout.Scouting;

/// <summary>Turns the zone name in the log into the site's zone name. See InternalDocumentation/Zones.md.</summary>
static class ZoneNames
{
    static readonly HashSet<string> NeverHaveABonus =
    [
        "The Plane of Tranquility",
        "The Plane of Knowledge",
        "The Guild Lobby",
        "Guild Hall",
    ];

    static readonly Dictionary<string, string> LogToSite = new()
    {
        ["Cabilis East"] = "Cabilis",
        ["Chardok: The Halls of Betrayal"] = "Halls of Betrayal",
        ["Clan Crushbone"] = "Crushbone",
        ["Kael Drakkel"] = "Kael Drakkal",
        ["Nagafen's Lair"] = "Nagafen's Lair (SolB)",
        ["North Desert of Ro"] = "North Ro",
        ["South Desert of Ro"] = "South Ro",
        ["The Akheva Ruins"] = "Akheva Ruins",
        ["The Burning Woods"] = "Burning Woods",
        ["The Castle of Mistmoore"] = "Castle Mistmoore",
        ["The Caverns of Exile"] = "Caverns of Exile",
        ["The City of Guk"] = "Upper Guk",
        ["The City of Mist"] = "City of Mist",
        ["The City of Thurgadin"] = "Thurgadin",
        ["The Commonlands"] = "Commonlands",
        ["The Crypt of Dalnir"] = "Dalnir",
        ["The Dreadlands"] = "Dreadlands",
        ["The Eastern Plains of Karana"] = "East Karana",
        ["The Emerald Jungle"] = "Emerald Jungle",
        ["The Estate of Unrest"] = "Estate of Unrest",
        ["The Field of Bone"] = "Field of Bone",
        ["The Fungus Grove"] = "Fungus Grove",
        ["The Gorge of King Xorbb"] = "Gorge of King Xorbb",
        ["The Great Divide"] = "Great Divide",
        ["The Greater Faydark"] = "Greater Faydark",
        ["The Iceclad Ocean"] = "Iceclad Ocean",
        ["The Lavastorm Mountains"] = "Lavastorm Mountains",
        ["The Lesser Faydark"] = "Lesser Faydark",
        ["The Liberated Citadel of Runnyeye"] = "Clan Runnyeye",
        ["The Maiden's Eye"] = "Maiden's Eye",
        ["The Mines of Nurga"] = "Mines of Nurga",
        ["The Northern Plains of Karana"] = "North Karana",
        ["The Ocean of Tears"] = "Ocean of Tears",
        ["The Plane of Fear"] = "Plane of Fear",
        ["The Plane of Growth"] = "Plane of Growth",
        ["The Plane of Hate"] = "Plane of Hate",
        ["The Plane of Sky"] = "Plane of Sky",
        ["The Rathe Mountains"] = "Rathe Mountains",
        ["The Ruins of Old Guk"] = "Lower Guk",
        ["The Ruins of Sebilis"] = "Old Sebilis",
        ["The Scarlet Desert"] = "Scarlet Desert",
        ["The Skyfire Mountains"] = "Skyfire Mountains",
        ["The Southern Plains of Karana"] = "South Karana",
        ["The Steamfont Mountains"] = "Steamfont Mountains",
        ["The Stonebrunt Mountains"] = "Stonebrunt Mountains",
        ["The Swamp of No Hope"] = "Swamp of No Hope",
        ["The Temple of Droga"] = "Temple of Droga",
        ["The Temple of Solusek Ro"] = "Temple of Solusek Ro",
        ["The Temple of Veeshan"] = "Temple of Veeshan",
        ["The Tenebrous Mountains"] = "Tenebrous Mountains",
        ["The Tower of Frozen Shadow"] = "Tower of Frozen Shadow",
        ["The Twilight Sea"] = "Twilight Sea",
        ["The Umbral Plains"] = "Umbral Plains",
        ["The Wakening Land"] = "Wakening Land",
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
            return new UnknownZone(logZone);
        }

        var siteName = SiteName(logZone, siteZones);
        if (siteName is null)
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
        if (siteZones.Contains(candidate))
        {
            return candidate;
        }

        return null;
    }
}
