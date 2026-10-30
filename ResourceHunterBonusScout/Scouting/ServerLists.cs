namespace ResourceHunterBonusScout.Scouting;

sealed record ServerLists(IReadOnlySet<BonusKey> Types, IReadOnlySet<string> Zones);
