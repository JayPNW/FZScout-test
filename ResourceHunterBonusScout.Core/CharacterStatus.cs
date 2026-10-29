namespace ResourceHunterBonusScout;

/// <param name="Zone">The site's spelling, or null when the current zone is unknown.</param>
public record CharacterStatus(string Name, string ServerName, string? Zone);
