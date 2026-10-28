using System.Text.RegularExpressions;

namespace ResourceHunterBonusScout.Logs;

partial record ZoneEnteredLogEvent(string Zone) : LogEvent
{
    [GeneratedRegex(@"^You have entered (?!an area)(?<zone>.*)\.$", RegexOptions.CultureInvariant)]
    private static partial Regex ZoneEnteredPattern();

    public static LogEvent? TryParse(string message)
    {
        var match = ZoneEnteredPattern().Match(message);
        if (!match.Success)
        {
            return null;
        }

        return new ZoneEnteredLogEvent(match.Groups["zone"].Value);
    }
}
