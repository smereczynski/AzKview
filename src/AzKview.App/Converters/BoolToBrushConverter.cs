using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AzKview.App.Converters;

public class BoolToBrushConverter : IValueConverter
{
    // ConverterParameter: "onColorHex,offColorHex" e.g. "#0078D4,#808080"
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isOn = value is bool b && b;
        var (onColor, offColor) = ParseParameter(parameter as string);
        return isOn ? new SolidColorBrush(Color.Parse(onColor)) : new SolidColorBrush(Color.Parse(offColor));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static (string onColor, string offColor) ParseParameter(string? param)
    {
        if (string.IsNullOrWhiteSpace(param))
            return ("#0078D4", "#808080");
        var parts = param.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
            return (parts[0], parts[1]);
        if (parts.Length == 1)
            return (parts[0], "#808080");
        return ("#0078D4", "#808080");
    }
}
