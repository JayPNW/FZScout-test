namespace ResourceHunterBonusScout.Logs;

/// <param name="NewLines">Lines added to each active log since the last poll.</param>
/// <param name="LoggingHintNeeded">Whether an EQ process has run long enough without any of its logs growing.</param>
public record LogWatcherPoll(IReadOnlyList<(ActiveLog Log, IReadOnlyList<string> Lines)> NewLines, bool LoggingHintNeeded);
