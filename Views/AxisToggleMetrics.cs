using System;
using Avalonia;

namespace SerialPlot.Views;

public static class AxisToggleMetrics
{
    public const double FluentCheckboxSize = 20;
    public const double BorderStrokeThickness = 1.4;
    public const double CheckStrokeThickness = 2.2;

    public static readonly Point CheckStart = new(4, 10);
    public static readonly Point CheckMiddle = new(8, 14);
    public static readonly Point CheckEnd = new(16, 6);

    public static AxisToggleScaledMetrics Scale(double dpiScale)
    {
        if (!double.IsFinite(dpiScale) || dpiScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiScale), "DPI scale must be positive and finite.");
        }

        var size = FluentCheckboxSize * dpiScale;
        var bounds = GetCheckBounds(dpiScale);
        return new AxisToggleScaledMetrics(
            size,
            new Point(size / 2, size / 2),
            bounds,
            new Point(bounds.Center.X, bounds.Center.Y),
            CheckStrokeThickness * dpiScale);
    }

    private static Rect GetCheckBounds(double dpiScale)
    {
        var minX = Math.Min(CheckStart.X, Math.Min(CheckMiddle.X, CheckEnd.X)) * dpiScale;
        var maxX = Math.Max(CheckStart.X, Math.Max(CheckMiddle.X, CheckEnd.X)) * dpiScale;
        var minY = Math.Min(CheckStart.Y, Math.Min(CheckMiddle.Y, CheckEnd.Y)) * dpiScale;
        var maxY = Math.Max(CheckStart.Y, Math.Max(CheckMiddle.Y, CheckEnd.Y)) * dpiScale;
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}

public sealed record AxisToggleScaledMetrics(
    double BoxSize,
    Point BoxCenter,
    Rect CheckBounds,
    Point CheckCenter,
    double CheckStrokeThickness);
