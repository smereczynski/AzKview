using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AzKview.App.Converters;

public class BoolToBlueGrayBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isOn = value is bool b && b;
        return isOn ? new SolidColorBrush(Color.Parse("#0078D4")) : new SolidColorBrush(Color.Parse("#808080"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
