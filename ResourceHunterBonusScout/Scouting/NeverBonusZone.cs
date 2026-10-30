namespace ResourceHunterBonusScout.Scouting;

/// <summary>A zone that never gets a bonus, like The Plane of Knowledge.</summary>
sealed record NeverBonusZone(string Name) : Zone;
