namespace ResourceHunterBonusScout;

/// <param name="Name">The <c>serverName</c> the API expects, like <c>Fangbreaker</c>.</param>
/// <param name="Site">The community site that tracks this server's bonuses.</param>
public record Server(string Name, Uri Site)
{
    public static readonly Server Fangbreaker = new("Fangbreaker", new("https://fangbreaker.zone/"));
    public static readonly Server Frostreaver = new("Frostreaver", new("https://frostreaver.zone/"));

    public static readonly IReadOnlyList<Server> All = [Fangbreaker, Frostreaver];

    /// <summary>The server for the <c>{server}</c> part of a log file name, ignoring case. Null for other servers.</summary>
    public static Server? FromLogName(string logName) =>
        All.FirstOrDefault(s => string.Equals(s.Name, logName, StringComparison.OrdinalIgnoreCase));

    public override string ToString() => Name;
}
