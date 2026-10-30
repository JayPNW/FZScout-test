namespace ResourceHunterBonusScout.Logs;

/// <summary>The server reset every zone's bonus for the new day.</summary>
sealed record DailyResetLogEvent : LogEvent
{
    public static LogEvent? TryParse(string message)
    {
        if (message.StartsWith("Fonts of unstable magic are erupting and receding", StringComparison.Ordinal))
        {
            return new DailyResetLogEvent();
        }

        return null;
    }
}
