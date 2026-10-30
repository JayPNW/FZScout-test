namespace ResourceHunterBonusScout.Logs;

/// <summary>The player is camping out. Repeats every 5 seconds during the countdown.</summary>
record CampingLogEvent : LogEvent
{
    public static LogEvent? TryParse(string message)
    {
        if (message.StartsWith("It will take ", StringComparison.Ordinal)
            && message.EndsWith(" to prepare your camp.", StringComparison.Ordinal))
        {
            return new CampingLogEvent();
        }

        return null;
    }
}
