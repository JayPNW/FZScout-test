namespace ResourceHunterBonusScout;

public enum TrayIconState { Idle, Active, Error }

/// <param name="Zone">The site's spelling, or null when the current zone is unknown.</param>
public record CharacterStatus(string Name, string ServerName, string? Zone);

/// <param name="Characters">The characters whose logs are being tailed.</param>
/// <param name="LastOperationFailed">Whether the last report or data load failed. Cleared by the next success.</param>
public record AppStatus(IReadOnlyList<CharacterStatus> Characters, bool HasApiKey, bool LastOperationFailed)
{
    public TrayIconState IconState
        => LastOperationFailed || !HasApiKey ? TrayIconState.Error
        : Characters.Count > 0 ? TrayIconState.Active
        : TrayIconState.Idle;
}
