using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

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
