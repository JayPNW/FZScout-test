namespace ResourceHunterBonusScout.Scouting;

/// <summary>A zone we can't match to the site's zones list.</summary>
sealed record UnknownZone(string LogName) : Zone;
