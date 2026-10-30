namespace ResourceHunterBonusScout.Scouting;

/// <param name="SiteName">The name the site uses, which is what we report.</param>
sealed record KnownZone(string SiteName) : Zone;
