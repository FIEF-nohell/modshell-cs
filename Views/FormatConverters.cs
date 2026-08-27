using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace modshell_hwtest.Views;

/// <summary>
/// Routes a single bound value through <see cref="Formatting"/>.
/// ConverterParameter picks the format: Percent, Temperature, Watts, Milliseconds, BytesPerSecond, Integer.
/// </summary>
public class MetricFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (parameter as string) switch
        {
            "Percent" => Formatting.Percent(value),
            "Temperature" => Formatting.Temperature(value),
            "Watts" => Formatting.Watts(value),
            "Milliseconds" => Formatting.Milliseconds(value),
            "BytesPerSecond" => Formatting.BytesPerSecond(value),
            _ => Formatting.Integer(value),
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Routes a used/total pair through <see cref="Formatting"/>.
/// ConverterParameter picks the unit: Gigabytes or Megabytes.
/// </summary>
public class MetricPairFormatConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var used = values.Count > 0 ? values[0] : null;
        var total = values.Count > 1 ? values[1] : null;

        return (parameter as string) switch
        {
            "Megabytes" => Formatting.Megabytes(used, total),
            _ => Formatting.Gigabytes(used, total),
        };
    }
}

/// <summary>
/// Picks a column count for the per-core list so it stays legible whether
/// the CPU has 4 cores or 32: few cores get 2 columns, more get up to 4.
/// </summary>
public class CoreColumnsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value is int i ? i : 0;
        return count switch
        {
            <= 8 => 2,
            <= 20 => 3,
            _ => 4,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a 0-100 load value to a green-to-red heat color, used by the
/// high-core-count tile view where a bar/label per core no longer fits.
/// </summary>
public class LoadHeatColorConverter : IValueConverter
{
    private static readonly Color Cold = Color.Parse("#22C55E");
    private static readonly Color Warm = Color.Parse("#EAB308");
    private static readonly Color Hot = Color.Parse("#EF4444");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var percent = value switch
        {
            double d => d,
            float f => f,
            _ => 0d,
        };
        percent = Math.Clamp(percent, 0, 100);

        var color = percent <= 50
            ? Lerp(Cold, Warm, percent / 50)
            : Lerp(Warm, Hot, (percent - 50) / 50);

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Color Lerp(Color from, Color to, double t) => new(
        255,
        (byte)(from.R + (to.R - from.R) * t),
        (byte)(from.G + (to.G - from.G) * t),
        (byte)(from.B + (to.B - from.B) * t));
}
