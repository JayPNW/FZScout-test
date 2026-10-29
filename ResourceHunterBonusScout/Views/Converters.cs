using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ResourceHunterBonusScout.Views;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IconStateToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => BitmapFrame.Create(new Uri($"pack://application:,,,/Assets/{(TrayIconState)value}.ico"));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

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
