namespace ResourceHunterBonusScout.Logs;

/// <summary>The player killed something. Kills by anyone else don't trigger a bonus message.</summary>
sealed record KillLogEvent : LogEvent
{
    public static LogEvent? TryParse(string message)
    {
        if (message.StartsWith("You have slain ", StringComparison.Ordinal))
        {
            return new KillLogEvent();
        }

        return null;
    }
}
