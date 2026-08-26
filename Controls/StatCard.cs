using System;
using Avalonia;
using Avalonia.Controls;

namespace modshell_hwtest.Controls;

/// <summary>
/// The single card primitive for the headline vitals row, so CPU, GPU and
/// Memory cannot drift into different internal layout logic from each other.
/// Title sits above the content; Caption is an optional line underneath.
/// </summary>
public class StatCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> CaptionProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(Caption));

    /// <summary>Device name shown next to the title, e.g. the CPU or GPU model.</summary>
    public static readonly StyledProperty<string?> DetailProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(Detail));

    public string? Detail
    {
        get => GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(StatCard);
}
