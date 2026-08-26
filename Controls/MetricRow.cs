using System;
using Avalonia;
using Avalonia.Controls;

namespace modshell_hwtest.Controls;

/// <summary>
/// The single row primitive for every label + bar/graph + value line in the app.
/// Label and value columns are Auto with a reserved MinWidth so they are never
/// squeezed; the middle column is star sized and absorbs all resizing. Both text
/// columns trim with an ellipsis as a hard backstop against overlap.
/// </summary>
public class MetricRow : ContentControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MetricRow, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<MetricRow, string?>(nameof(Value));

    public static readonly StyledProperty<double> LabelMinWidthProperty =
        AvaloniaProperty.Register<MetricRow, double>(nameof(LabelMinWidth), 56);

    public static readonly StyledProperty<double> ValueMinWidthProperty =
        AvaloniaProperty.Register<MetricRow, double>(nameof(ValueMinWidth), 74);

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double LabelMinWidth
    {
        get => GetValue(LabelMinWidthProperty);
        set => SetValue(LabelMinWidthProperty, value);
    }

    public double ValueMinWidth
    {
        get => GetValue(ValueMinWidthProperty);
        set => SetValue(ValueMinWidthProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(MetricRow);
}
