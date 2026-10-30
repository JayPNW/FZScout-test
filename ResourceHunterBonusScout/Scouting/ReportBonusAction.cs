using ResourceHunterBonusScout.Site;

namespace ResourceHunterBonusScout.Scouting;

/// <summary>Report a bonus that EverQuest announced.</summary>
sealed record ReportBonusAction(Character Character, BonusReport Report) : ScoutAction;
