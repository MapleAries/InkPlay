using Microsoft.UI.Xaml.Data;

namespace InkPlay.App.Converters;

public class BoolToTextConverter : IValueConverter
{
    public string TrueText { get; set; } = "新增";
    public string FalseText { get; set; } = "编辑";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return b ? TrueText : FalseText;
        return FalseText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
