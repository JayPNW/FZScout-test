namespace ResourceHunterBonusScout.Logs;

/// <param name="Zone">The zone name as EverQuest writes it, which may differ from the site's name.</param>
record ZoneEnteredLogEvent(string Zone) : LogEvent
{
    const string Prefix = "You have entered ";

    public static LogEvent? TryParse(string message)
    {
        if (!message.StartsWith(Prefix, StringComparison.Ordinal) || !message.EndsWith('.'))
        {
            return null;
        }

        if (message.StartsWith($"{Prefix}an area", StringComparison.Ordinal))
        {
            return null;
        }

        return new ZoneEnteredLogEvent(message[Prefix.Length..^1]);
    }
}
