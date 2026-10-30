using System.Runtime.CompilerServices;
using ResourceHunterBonusScout.Scouting;

namespace ResourceHunterBonusScout.Tests.Scouting;

public class ZoneNamesTests
{
    /// <summary>Zones on the sites whose name in the EverQuest log isn't known yet.</summary>
    static readonly HashSet<string> UnknownLogNames =
    [
        "Felwithe",
        "Kaladim",
        "Neriak",
        "Plane of Mischief",
        "Qeynos Catacombs",
        "Sleeper's Tomb",
        "The Hole",
        "Veeshan's Peak",
    ];

    /// <summary>Generates the table in InternalDocumentation/Zones.md from the sites' zone lists.</summary>
    [Fact]
    public Task ZoneNamesMapping()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Site", "SiteZonesIntegrationTests.KnownZones.verified.txt");
        var siteZones = File.ReadAllLines(path).Where(line => line.Length > 0).ToHashSet();
        var logNames = ZoneNames.LogToSite.ToDictionary(pair => pair.Value, pair => pair.Key);

        // Unknown log names first, then zones with a different log name, then zones whose names match.
        static (int Group, string LogName, string Site) ToRow(
            string zone, Dictionary<string, string> logNames, HashSet<string> siteZones)
        {
            if (UnknownLogNames.Contains(zone))
            {
                return (0, "<unknown>", zone);
            }

            if (logNames.TryGetValue(zone, out var logName))
            {
                return (1, logName, Describe(ZoneNames.Resolve(logName, siteZones)));
            }

            return (2, "<same>", Describe(ZoneNames.Resolve(zone, siteZones)));
        }

        var rows = from zone in siteZones
                   let r = ToRow(zone, logNames, siteZones)
                   orderby r.Group, r.Group == 1 ? r.LogName : r.Site
                   select (r.LogName, r.Site);

        return Verify(FormatAsMarkdownTable(("EQ log message name", "Site zone name"), rows));
    }

    static string FormatAsMarkdownTable<TTuple>(TTuple header, IEnumerable<TTuple> rows)
        where TTuple : ITuple
    {
        var all = rows.Prepend(header).Select(row => ToCells(row)).ToList();
        var widths = Enumerable.Range(0, header.Length)
            .Select(column => all.Max(cells => cells[column].Length))
            .ToList();
        var lines = all
            .Select(cells => ToLine(cells.Select((cell, column) => cell.PadRight(widths[column])), " "))
            .ToList();
        lines.Insert(1, ToLine(widths.Select(width => new string('-', width)), "-"));

        return string.Join(Environment.NewLine, lines);
    }

    static string[] ToCells(ITuple row)
    {
        return Enumerable.Range(0, row.Length).Select(i => row[i]?.ToString() ?? "").ToArray();
    }

    static string ToLine(IEnumerable<string> cells, string padding)
    {
        return $"|{padding}{string.Join($"{padding}|{padding}", cells)}{padding}|";
    }

    static string Describe(Zone zone)
    {
        return zone switch
        {
            KnownZone known => known.SiteName,
            NoBonusZone => "<no bonus>",
            UnknownZone => "<unknown>",
            UnresolvedZone => "<unresolved>",
            _ => throw new ArgumentOutOfRangeException(nameof(zone)),
        };
    }
}
