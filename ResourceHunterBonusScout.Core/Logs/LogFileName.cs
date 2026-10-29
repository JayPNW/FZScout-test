using System.Text.RegularExpressions;

namespace ResourceHunterBonusScout.Logs;

/// <summary>What an <c>eqlog_{CharacterName}_{server}.txt</c> file name says.</summary>
public static partial class LogFileName
{
    public const string SearchPattern = "eqlog_*.txt";

    /// <returns>Null when the name doesn't match, or the server isn't one the app reports for.</returns>
    public static (string Character, Server Server)? Parse(string path)
    {
        var match = Pattern().Match(Path.GetFileName(path));
        if (!match.Success)
        {
            return null;
        }

        var server = Server.FromLogName(match.Groups["server"].Value);
        return server is null ? null : (match.Groups["character"].Value, server);
    }

    [GeneratedRegex(@"^eqlog_(?<character>[^_]+)_(?<server>[^_]+)\.txt$", RegexOptions.IgnoreCase)]
    private static partial Regex Pattern();
}
