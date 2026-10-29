namespace ResourceHunterBonusScout;

/// <param name="Characters">The characters whose logs are being tailed.</param>
/// <param name="LastOperationFailed">Whether the last report, data load, or zone lookup failed. Cleared by the next success.</param>
/// <param name="EqRunning">Whether any EverQuest process is running.</param>
public record AppStatus(IReadOnlyList<CharacterStatus> Characters, bool HasApiKey, bool LastOperationFailed, bool EqRunning)
{
    // Windows cuts tray tooltips off at 127 characters.
    const int MaxToolTipLength = 127;

    public TrayIconState IconState
    {
        get
        {
            if (LastOperationFailed || !HasApiKey)
            {
                return TrayIconState.Error;
            }

            if (Characters.Any(c => c.NoBonusAt is not null))
            {
                return TrayIconState.Waiting;
            }

            if (Characters.Count > 0)
            {
                return TrayIconState.Active;
            }

            return TrayIconState.Idle;
        }
    }

    public string ToolTip
    {
        get
        {
            var text = string.Join("\n", ["Resource Hunter Bonus Scout", .. ToolTipLines()]);
            return text.Length <= MaxToolTipLength ? text : text[..(MaxToolTipLength - 1)] + "…";
        }
    }

    IEnumerable<string> ToolTipLines()
    {
        if (!EqRunning)
        {
            return ["EverQuest is not running."];
        }

        if (Characters.Count == 0)
        {
            return ["No EverQuest log is being watched."];
        }

        return Characters.Select(ToolTipLine);
    }

    static string ToolTipLine(CharacterStatus c)
    {
        var line = c.Zone ?? "unknown zone";
        return c.NoBonusAt is { } at ? $"{line}, waiting for bonus until {at.ToLocalTime():T}" : line;
    }
}
