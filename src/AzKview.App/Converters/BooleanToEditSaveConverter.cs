using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AzKview.App.Converters;

public sealed class BooleanToEditSaveConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "Save" : "Edit";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}
