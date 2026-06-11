using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace SerialPlot.Views;

public sealed class TraceBrushBindingConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value ?? AvaloniaProperty.UnsetValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => AvaloniaProperty.UnsetValue;
}
