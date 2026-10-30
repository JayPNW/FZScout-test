namespace ResourceHunterBonusScout.Logs;

static class LogLine
{
    /// <summary>Removes the leading <c>[Mon Sep 14 16:10:29 2026] </c>.</summary>
    public static string StripTimestamp(string line)
    {
        if (!line.StartsWith('['))
        {
            return line;
        }

        var end = line.IndexOf("] ", StringComparison.Ordinal);
        if (end < 0)
        {
            return line;
        }

        return line[(end + 2)..];
    }
}
