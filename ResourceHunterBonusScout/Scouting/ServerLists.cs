namespace ResourceHunterBonusScout.Scouting;

record ServerLists(IReadOnlySet<BonusKey> Types, IReadOnlySet<string> Zones);
