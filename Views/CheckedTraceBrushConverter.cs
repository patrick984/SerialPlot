using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace SerialPlot.Views;

public sealed class CheckedTraceBrushConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is bool isChecked && isChecked && values[1] is { } brush)
        {
            return brush;
        }

        return AvaloniaProperty.UnsetValue;
    }
}
