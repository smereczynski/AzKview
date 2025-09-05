using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AzKview.App.Converters;

public class BoolFalseToBlueGrayBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isTrue = value is bool b && b;
        return isTrue ? new SolidColorBrush(Color.Parse("#808080")) : new SolidColorBrush(Color.Parse("#0078D4"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
