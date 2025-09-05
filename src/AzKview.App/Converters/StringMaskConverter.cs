using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AzKview.App.Converters
{
    public class StringMaskConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return new string('•', Math.Min(s.Length, 8));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // One-way converter used for display only
            throw new NotSupportedException();
        }
    }
}
