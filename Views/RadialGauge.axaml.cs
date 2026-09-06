using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace modshell_cs.Views;

public partial class RadialGauge : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<RadialGauge, double>(nameof(Value));

    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<RadialGauge, string>(nameof(Unit), "%");

    public static readonly StyledProperty<string?> CaptionProperty =
        AvaloniaProperty.Register<RadialGauge, string?>(nameof(Caption));

    /// <summary>Value at which the reading starts warning (arc turns amber).</summary>
    public static readonly StyledProperty<double> WarnThresholdProperty =
        AvaloniaProperty.Register<RadialGauge, double>(nameof(WarnThreshold), 60);

    /// <summary>Value at which the reading is critical (arc turns red).</summary>
    public static readonly StyledProperty<double> DangerThresholdProperty =
        AvaloniaProperty.Register<RadialGauge, double>(nameof(DangerThreshold), 85);

    /// <summary>Paints the danger span onto the track so the limit is visible at rest.</summary>
    public static readonly StyledProperty<bool> ShowDangerZoneProperty =
        AvaloniaProperty.Register<RadialGauge, bool>(nameof(ShowDangerZone));

    /// <summary>Full-scale reading, so the gauge can show units other than percent.
    /// Watts, for example, need their own ceiling rather than a 0-100 sweep.</summary>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RadialGauge, double>(nameof(Maximum), 100);

    private const double StartDeg = 135;
    private const double SweepDeg = 270;
    private const double Radius = 45;
    private const double Center = 54;

    private static readonly Color ColorNormal = Color.Parse("#8B5CF6");
    private static readonly Color ColorWarn = Color.Parse("#FBBF24");
    private static readonly Color ColorDanger = Color.Parse("#F87171");

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public string? Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public double WarnThreshold
    {
        get => GetValue(WarnThresholdProperty);
        set => SetValue(WarnThresholdProperty, value);
    }

    public double DangerThreshold
    {
        get => GetValue(DangerThresholdProperty);
        set => SetValue(DangerThresholdProperty, value);
    }

    public bool ShowDangerZone
    {
        get => GetValue(ShowDangerZoneProperty);
        set => SetValue(ShowDangerZoneProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    private double Scale => Maximum > 0 ? Maximum : 100;

    public RadialGauge()
    {
        InitializeComponent();
        TrackPath.Data = GaugeArc.Build(Center, Center, Radius, StartDeg, SweepDeg);
        UnitLabel.Text = Unit;
        CaptionLabel.Text = Caption;
        CaptionLabel.IsVisible = !string.IsNullOrEmpty(Caption);
        UpdateDangerZone();
        UpdateVisual(Value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            UpdateVisual(change.GetNewValue<double>());
        }
        else if (change.Property == UnitProperty && UnitLabel is not null)
        {
            UnitLabel.Text = change.GetNewValue<string>();
        }
        else if (change.Property == CaptionProperty && CaptionLabel is not null)
        {
            var caption = change.GetNewValue<string?>();
            CaptionLabel.Text = caption;
            CaptionLabel.IsVisible = !string.IsNullOrEmpty(caption);
        }
        else if (change.Property == ShowDangerZoneProperty || change.Property == DangerThresholdProperty
                 || change.Property == MaximumProperty)
        {
            UpdateDangerZone();
            UpdateVisual(Value);
        }
        else if (change.Property == WarnThresholdProperty)
        {
            UpdateVisual(Value);
        }
    }

    private void UpdateDangerZone()
    {
        if (DangerPath is null)
        {
            return;
        }

        var max = Scale;
        if (!ShowDangerZone || DangerThreshold <= 0 || DangerThreshold >= max)
        {
            DangerPath.Data = null;
            return;
        }

        var startDeg = StartDeg + SweepDeg * (DangerThreshold / max);
        var sweep = SweepDeg * ((max - DangerThreshold) / max);
        DangerPath.Data = GaugeArc.Build(Center, Center, Radius, startDeg, sweep);
    }

    private void UpdateVisual(double value)
    {
        if (ValuePath is null || ValueText is null)
        {
            return;
        }

        var max = Scale;
        var clamped = Math.Clamp(value, 0, max);
        var sweep = SweepDeg * (clamped / max);
        ValuePath.Data = GaugeArc.Build(Center, Center, Radius, StartDeg, sweep);
        ValuePath.Stroke = new SolidColorBrush(ColorForValue(clamped));
        ValueText.Text = Formatting.Integer(clamped);
    }

    // A DangerThreshold at or below the warn point disables threshold colouring
    // entirely, which is what readings with no universal limit (watts) want.
    private Color ColorForValue(double value)
    {
        var warn = WarnThreshold;
        var danger = DangerThreshold;

        if (value < warn || danger <= warn)
        {
            return ColorNormal;
        }

        if (value < danger)
        {
            return Lerp(ColorNormal, ColorWarn, (value - warn) / (danger - warn));
        }

        var span = Math.Max(1, Scale - danger);
        return Lerp(ColorWarn, ColorDanger, Math.Min(1.0, (value - danger) / span));
    }

    private static Color Lerp(Color a, Color b, double t) => Color.FromArgb(
        255,
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t));
}
