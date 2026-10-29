namespace ResourceHunterBonusScout;

/// <param name="Characters">The characters whose logs are being tailed.</param>
/// <param name="LastOperationFailed">Whether the last report, data load, or zone lookup failed. Cleared by the next success.</param>
/// <param name="EqRunning">Whether any EverQuest process is running.</param>
public record AppStatus(IReadOnlyList<CharacterStatus> Characters, bool HasApiKey, bool LastOperationFailed, bool EqRunning)
{
    public TrayIconState IconState
        => LastOperationFailed || !HasApiKey ? TrayIconState.Error
        : Characters.Count > 0 ? TrayIconState.Active
        : TrayIconState.Idle;
}
