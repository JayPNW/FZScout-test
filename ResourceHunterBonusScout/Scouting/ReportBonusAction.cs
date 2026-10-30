using ResourceHunterBonusScout.Site;

namespace ResourceHunterBonusScout.Scouting;

sealed record ReportBonusAction(Character Character, BonusReport Report) : ScoutAction;
