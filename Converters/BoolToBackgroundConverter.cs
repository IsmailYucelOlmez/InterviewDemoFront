using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CommunicationApp.Converters;

public class BoolToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isFromMe)
        {
            return isFromMe ? new SolidColorBrush(Color.FromRgb(200, 230, 255)) : Brushes.White;
        }
        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

