namespace ResourceHunterBonusScout.Logs;

/// <summary>EverQuest announced the current zone's bonus.</summary>
sealed record BonusReportLogEvent(BonusKey BonusKey) : LogEvent
{
    const string Prefix = "This land is saturated with unstable magic, ";

    static readonly (string Ending, BonusKey Key)[] Endings =
    [
        ("improving the experiences of all who venture here.", new("experience")),
        ("improving the experience tied to abilities for all who venture here.", new("aa")),
        ("multiplying the loot that may be found here.", new("loot")),
        ("multiplying the coin that may be found here.", new("coin")),
        ("which has drawn many creatures of exceptional rarity here.", new("rare")),
        ("magnifying the recognition one may receive from their adventures here.", new("faction")),
        ("which has drawn numerous mundane creatures here.", new("respawn")),
        ("enhancing the benefits of offerings for all who venture here.", new("tribute")),
        ("enhancing the potency of any offerings made here.", new("tribute")),
        ("improving the rate at which proficiency is gained while adventuring here.", new("tradeskill")),
    ];

    public static LogEvent? TryParse(string message)
    {
        if (!message.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        foreach (var (ending, key) in Endings)
        {
            if (message.EndsWith(ending, StringComparison.Ordinal))
            {
                return new BonusReportLogEvent(key);
            }
        }

        return null;
    }
}
