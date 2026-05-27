using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace InkPlay.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            boolValue = parameter?.ToString() == "Invert" ? !boolValue : boolValue;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            var result = visibility == Visibility.Visible;
            return parameter?.ToString() == "Invert" ? !result : result;
        }
        return false;
    }
}
