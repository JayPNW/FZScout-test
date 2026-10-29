using System.Globalization;
using System.Windows.Data;

namespace ResourceHunterBonusScout.Views;

public class StatusToToolTipConverter : IValueConverter
{
    // Windows cuts tray tooltips off at 127 characters.
    const int MaxLength = 127;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = (AppStatus)value;
        IEnumerable<string> lines = status.Characters.Count == 0
            ? ["No EverQuest log is being watched."]
            : status.Characters.Select(c => $"{c.Name} ({c.ServerName}): {c.Zone ?? "unknown zone"}");
        var text = string.Join("\n", ["Resource Hunter Bonus Scout", .. lines]);
        return text.Length <= MaxLength ? text : text[..(MaxLength - 1)] + "…";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
