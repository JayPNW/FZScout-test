namespace ResourceHunterBonusScout.Scouting;

/// <summary>A server's <c>types</c> and <c>zones</c> lists from the site.</summary>
sealed record ServerLists(IReadOnlySet<BonusKey> Types, IReadOnlySet<string> Zones);
