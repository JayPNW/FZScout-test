using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ResourceHunterBonusScout.Views;

public class IconStateToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => BitmapFrame.Create(new Uri($"pack://application:,,,/Assets/{(TrayIconState)value}.ico"));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
