using System.Globalization;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ATAG.Costing.WinUI;

public sealed class HexColourBrushConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        string language)
    {
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        }

        var hex = text.Trim().TrimStart('#');
        if (hex.Length == 6 &&
            byte.TryParse(
                hex[..2],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var red) &&
            byte.TryParse(
                hex.Substring(2, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var green) &&
            byte.TryParse(
                hex.Substring(4, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var blue))
        {
            return new SolidColorBrush(Color.FromArgb(255, red, green, blue));
        }

        return new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        string language) =>
        throw new NotSupportedException();
}
