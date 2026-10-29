namespace ResourceHunterBonusScout;

/// <param name="Zone">The site's spelling, or null when the current zone is unknown.</param>
/// <param name="NoBonusAt">When no bonus will be reported, or null when nothing is waiting.</param>
public record CharacterStatus(string Name, string ServerName, string? Zone, DateTimeOffset? NoBonusAt = null);
