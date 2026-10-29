using System.Text.RegularExpressions;

namespace ResourceHunterBonusScout.Logs;

/// <summary>A log line that matters for reporting.</summary>
public abstract partial record LogEvent
{
    static readonly (string Ending, string BonusKey)[] BonusMessages =
    [
        ("improving the experiences of all who venture here.", "experience"),
        ("improving the experience tied to abilities for all who venture here.", "aa"),
        ("multiplying the loot that may be found here.", "loot"),
        ("multiplying the coin that may be found here.", "coin"),
        ("which has drawn many creatures of exceptional rarity here.", "rare"),
        ("magnifying the recognition one may receive from their adventures here.", "faction"),
        ("which has drawn numerous mundane creatures here.", "respawn"),
        ("enhancing the benefits of offerings for all who venture here.", "tribute"),
        ("enhancing the potency of any offerings made here.", "tribute"),
        ("improving the rate at which proficiency is gained while adventuring here.", "tradeskill"),
    ];

    /// <returns>Null for lines that don't matter.</returns>
    public static LogEvent? Parse(string line)
    {
        var text = StripTimestamp(line);

        if (ZoneEnteredRegex().Match(text) is { Success: true } zoneEntered)
        {
            return new ZoneEntered(zoneEntered.Groups["zone"].Value);
        }

        foreach (var (ending, bonusKey) in BonusMessages)
        {
            if (text.EndsWith(ending, StringComparison.Ordinal))
            {
                return new BonusMessage(bonusKey);
            }
        }

        if (text.StartsWith("You have slain ", StringComparison.Ordinal) && text.EndsWith('!'))
        {
            return new Kill();
        }

        if (text == "It will take you about 30 seconds to prepare your camp.")
        {
            return new CampStarted();
        }

        if (text == "You abandon your preparations to camp.")
        {
            return new CampAbandoned();
        }

        if (text.StartsWith("Fonts of unstable magic are erupting", StringComparison.Ordinal))
        {
            return new DailyReset();
        }

        // The game doesn't announce a persona change, but these lines come with one.
        if (text == "Autojoining channels..." || EquipmentSetLoadedRegex().IsMatch(text))
        {
            return new PersonaChanged();
        }

        return null;
    }

    [GeneratedRegex(@"^You have entered (?!an area)(?<zone>.+)\.$")]
    private static partial Regex ZoneEnteredRegex();

    [GeneratedRegex(@"^You successfully loaded your .+ equipment set\.$")]
    private static partial Regex EquipmentSetLoadedRegex();

    /// <summary>Removes a leading <c>[Mon Sep 14 16:10:29 2026] </c>.</summary>
    static string StripTimestamp(string line)
    {
        if (!line.StartsWith('['))
        {
            return line;
        }

        var end = line.IndexOf("] ", StringComparison.Ordinal);
        return end < 0 ? line : line[(end + 2)..];
    }

    /// <summary>The zone name as the log spells it.</summary>
    public record ZoneEntered(string Zone) : LogEvent;

    public record BonusMessage(string BonusKey) : LogEvent;

    /// <summary><c>You have slain {mob}!</c></summary>
    public record Kill : LogEvent;

    public record CampStarted : LogEvent;

    public record CampAbandoned : LogEvent;

    public record DailyReset : LogEvent;

    /// <summary>The character switched to another class's persona, without zoning.</summary>
    public record PersonaChanged : LogEvent;
}
