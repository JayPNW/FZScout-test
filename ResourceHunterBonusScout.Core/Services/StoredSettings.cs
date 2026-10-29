namespace ResourceHunterBonusScout.Services;

/// <param name="IdleTimer">Report <see cref="Reporting.BonusReport.NoBonus"/> after a few minutes in a zone with no bonus message.</param>
public record StoredSettings(bool IdleTimer = true);
