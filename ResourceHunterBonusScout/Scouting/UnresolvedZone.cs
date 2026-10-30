namespace ResourceHunterBonusScout.Scouting;

/// <summary>A zone entered while the server's zones list wasn't loaded. It's resolved once the list loads.</summary>
sealed record UnresolvedZone(string LogName) : Zone;
