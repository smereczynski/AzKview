using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AzKview.App.Converters
{
    public class BooleanToRevealHideConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b ? "Hide" : "Reveal";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
