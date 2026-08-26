using System;
using System.Globalization;

namespace modshell_hwtest;

/// <summary>
/// The single source of truth for how every number in the app is rendered.
/// No view, card or control should format a numeric value on its own.
/// </summary>
public static class Formatting
{
    public const string Unavailable = "--";

    public static string Integer(object? value) =>
        TryNumber(value, out var n) ? n.ToString("0", CultureInfo.InvariantCulture) : Unavailable;

    public static string Percent(object? value) =>
        TryNumber(value, out var n) ? n.ToString("0", CultureInfo.InvariantCulture) + "%" : Unavailable;

    public static string Temperature(object? value) =>
        TryNumber(value, out var n) ? n.ToString("0", CultureInfo.InvariantCulture) + "°C" : Unavailable;

    public static string Watts(object? value) =>
        TryNumber(value, out var n) ? n.ToString("0", CultureInfo.InvariantCulture) + " W" : Unavailable;

    public static string Milliseconds(object? value) =>
        TryNumber(value, out var n) ? n.ToString("0", CultureInfo.InvariantCulture) + " ms" : Unavailable;

    public static string BytesPerSecond(object? value)
    {
        if (!TryNumber(value, out var bytes))
        {
            return Unavailable;
        }

        return bytes switch
        {
            >= 1024 * 1024 => (bytes / (1024 * 1024)).ToString("0.00", CultureInfo.InvariantCulture) + " MB/s",
            >= 1024 => (bytes / 1024).ToString("0.0", CultureInfo.InvariantCulture) + " KB/s",
            _ => bytes.ToString("0", CultureInfo.InvariantCulture) + " B/s",
        };
    }

    public static string Gigabytes(object? used, object? total)
    {
        if (!TryNumber(used, out var u) || !TryNumber(total, out var t))
        {
            return Unavailable;
        }

        return string.Format(
            CultureInfo.InvariantCulture, "{0:0.0} / {1:0.0} GB", u, t);
    }

    public static string Megabytes(object? used, object? total)
    {
        if (!TryNumber(used, out var u) || !TryNumber(total, out var t))
        {
            return Unavailable;
        }

        return string.Format(
            CultureInfo.InvariantCulture, "{0:0} / {1:0} MB", u, t);
    }

    private static bool TryNumber(object? value, out double result)
    {
        result = 0;

        if (value is null)
        {
            return false;
        }

        try
        {
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return false;
        }

        return !double.IsNaN(result) && !double.IsInfinity(result);
    }
}
