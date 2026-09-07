using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace modshell_cs.ViewModels;

/// <summary>
/// One logical processor in the thread panel. Unlike the other readings this is
/// created once per thread and updated in place: the sparkline needs its history
/// to survive every tick, which a value replaced each second cannot do.
/// </summary>
public partial class CoreUsageItem : ObservableObject
{
    public CoreUsageItem(
        int index,
        ObservableCollection<double> history,
        ISeries[] series,
        Axis[] xAxes,
        Axis[] yAxes)
    {
        Index = index;
        History = history;
        Series = series;
        XAxes = xAxes;
        YAxes = yAxes;
    }

    public int Index { get; }

    /// <summary>Backing values for <see cref="Series"/>, appended to by the owner.</summary>
    public ObservableCollection<double> History { get; }

    public ISeries[] Series { get; }
    public Axis[] XAxes { get; }
    public Axis[] YAxes { get; }

    [ObservableProperty]
    private double _value;
}
