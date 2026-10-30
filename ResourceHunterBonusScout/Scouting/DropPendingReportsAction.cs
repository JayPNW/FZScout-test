namespace ResourceHunterBonusScout.Scouting;

/// <summary>Forget any of this character's reports still waiting to retry; they're stale after the daily reset.</summary>
sealed record DropPendingReportsAction(Character Character) : ScoutAction;
