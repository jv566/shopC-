using System.Globalization;

namespace Shop.Maui.Converters;

public sealed class BannerDotConverter : IValueConverter
{
    public Color ActiveColor { get; set; } = Color.FromRgb(0xc8, 0xa9, 0x6e);
    public Color InactiveColor { get; set; } = Color.FromRgb(0x55, 0x55, 0x55);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentIndex && parameter is string paramStr && int.TryParse(paramStr, out int dotIndex))
        {
            return currentIndex == dotIndex ? ActiveColor : InactiveColor;
        }
        return InactiveColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
