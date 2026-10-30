using System.Text.RegularExpressions;

namespace ResourceHunterBonusScout.Logs;

internal static partial class LogLine
{
    /// <summary>Removes the leading <c>[Mon Sep 14 16:10:29 2026] </c>.</summary>
    public static string StripTimestamp(string line) => Timestamp().Replace(line, string.Empty);

    [GeneratedRegex(@"^\[[^\]]*\] ")]
    private static partial Regex Timestamp();
}
