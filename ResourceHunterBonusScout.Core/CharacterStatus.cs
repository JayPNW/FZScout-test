namespace ResourceHunterBonusScout;

/// <param name="Zone">The site's spelling, the log's spelling for a zone that never has a bonus, or null when the current zone is unknown.</param>
/// <param name="NoBonusAt">When no bonus will be reported, or null when nothing is waiting.</param>
/// <param name="NeverHasBonus">Whether the current zone is one of the <see cref="Reporting.NoBonusZones"/>.</param>
public record CharacterStatus(string Name, string ServerName, string? Zone, DateTimeOffset? NoBonusAt = null, bool NeverHasBonus = false);
