namespace ResourceHunterBonusScout.Reporting;

/// <param name="Zone">The site's spelling.</param>
public record BonusReport(string Character, Server Server, string Zone, string BonusKey)
{
    public const string NoBonus = "none";

    /// <summary>The site's bonus for a zone nobody has reported today.</summary>
    public const string Unconfirmed = "unconfirmed";
}
