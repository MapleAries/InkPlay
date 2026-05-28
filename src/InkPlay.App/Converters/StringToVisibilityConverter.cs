using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace InkPlay.App.Converters;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var str = value as string;
        var hasValue = !string.IsNullOrEmpty(str);

        if (parameter?.ToString() == "Invert")
        {
            return hasValue ? Visibility.Collapsed : Visibility.Visible;
        }

        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
