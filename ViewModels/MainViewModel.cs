using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LibreHardwareMonitor.Hardware;
using modshell_cs.Models;
using SkiaSharp;

namespace modshell_cs.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private const int HistoryLength = 60;
    private const string PingHost = "google.com";

    // The sparkline shows the last 60 samples, but a one minute window is far
    // too short for tail percentiles or a meaningful loss rate: a single dropped
    // reply would read as 1.7% loss. Ten minutes of one-per-second samples gives
    // p99 six-hundred readings to sit on and keeps the arrays small enough to
    // sort every tick without noticing.
    private const int LatencyWindowSeconds = 600;

    // Above this many physical cores the per-core list (label + bar + %) no
    // longer fits legibly, so the view switches to a compact heat-tile grid.
    // Chosen so mainstream desktop/laptop CPUs (up to 32 cores) keep the
    // detailed list, while HEDT/workstation chips (Threadripper, Xeon) get tiles.
    private const int HighCoreCountThreshold = 32;

    private static readonly SKColor LabelColor = new(0x6B, 0x67, 0x7E);
    private static readonly SKColor GridColor = new(0x1D, 0x1A, 0x25);

    public static readonly SKColor CpuTraceColor = new(0x8B, 0x5C, 0xF6);
    public static readonly SKColor GpuTraceColor = new(0x22, 0xD3, 0xEE);
    public static readonly SKColor MemTraceColor = new(0x34, 0xD3, 0x99);

    private readonly Computer _computer;
    private readonly Ping _ping = new();
    private readonly CancellationTokenSource _cts = new();

    private readonly ObservableCollection<double> _cpuUsageHistory = new();
    private readonly ObservableCollection<double> _gpuUsageHistory = new();
    private readonly ObservableCollection<double> _cpuTempHistory = new();
    private readonly ObservableCollection<double> _gpuTempHistory = new();
    private readonly ObservableCollection<double> _memUsedHistory = new();
    private readonly ObservableCollection<double> _netUpHistory = new();
    private readonly ObservableCollection<double> _netDownHistory = new();
    private readonly ObservableCollection<double> _pingHistory = new();
    private readonly ObservableCollection<double> _gpuVramUsedHistory = new();
    private readonly LatencyTracker _latency = new(LatencyWindowSeconds);

    private readonly Axis _memYAxis = YAxis(32);
    private readonly Axis _vramYAxis = YAxis(8);

    [ObservableProperty]
    private HardwareSnapshot _currentSnapshot = new("...", 0, null, [], "...", 0, null, null, null, null, 0, 0, 0, 0, null);

    [ObservableProperty]
    private LatencyStats _latencyStats = LatencyStats.Empty;

    /// <summary>Window coverage shown next to the ping header, so a warming-up
    /// window is never mistaken for a full ten minutes of history.</summary>
    [ObservableProperty]
    private string _latencyWindowLabel = "warming up";

    public string PingHeader { get; } = $"PING · {PingHost.ToUpperInvariant()}";

    public string AppTitle { get; } = $"modshell hardware monitor - v{ResolveVersion()}";

    public ObservableCollection<CoreUsageItem> PerCoreItems { get; } = new();

    [ObservableProperty]
    private int _coreCount;

    [ObservableProperty]
    private bool _isHighCoreCount;

    public SolidColorPaint TooltipBackground { get; } = new(new SKColor(0x1C, 0x19, 0x26));
    public SolidColorPaint TooltipText { get; } = new(new SKColor(0xF2, 0xF0, 0xF8));

    public ISeries[] CpuUtilizationSeries { get; }
    public ISeries[] CpuThermalSeries { get; }
    public ISeries[] GpuUtilizationSeries { get; }
    public ISeries[] GpuThermalSeries { get; }
    public ISeries[] MemorySeries { get; }
    public ISeries[] NetUpSeries { get; }
    public ISeries[] NetDownSeries { get; }
    public ISeries[] PingSeries { get; }
    public ISeries[] GpuVramSeries { get; }

    public Axis[] CpuUtilizationXAxes { get; } = [XAxis()];
    public Axis[] CpuUtilizationYAxes { get; } = [YAxis(100)];
    public Axis[] CpuThermalXAxes { get; } = [XAxis()];
    public Axis[] CpuThermalYAxes { get; } = [YAxis(100)];
    public Axis[] GpuUtilizationXAxes { get; } = [XAxis()];
    public Axis[] GpuUtilizationYAxes { get; } = [YAxis(100)];
    public Axis[] GpuThermalXAxes { get; } = [XAxis()];
    public Axis[] GpuThermalYAxes { get; } = [YAxis(100)];
    public Axis[] MemoryXAxes { get; } = [XAxis()];
    public Axis[] MemoryYAxes { get; }
    public Axis[] NetUpXAxes { get; } = [XAxis()];
    public Axis[] NetUpYAxes { get; } = [HiddenAxis()];
    public Axis[] NetDownXAxes { get; } = [XAxis()];
    public Axis[] NetDownYAxes { get; } = [HiddenAxis()];
    public Axis[] PingXAxes { get; } = [XAxis()];
    public Axis[] PingYAxes { get; } = [HiddenAxis()];
    public Axis[] GpuVramXAxes { get; } = [XAxis()];
    public Axis[] GpuVramYAxes { get; }

    public MainViewModel()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsNetworkEnabled = true,
        };
        _computer.Open();

        MemoryYAxes = [_memYAxis];
        GpuVramYAxes = [_vramYAxis];
        _vramYAxis.Labeler = value => value.ToString("0.0", CultureInfo.InvariantCulture);

        CpuUtilizationSeries = [Trace(_cpuUsageHistory, CpuTraceColor, "cpu", "%")];
        CpuThermalSeries = [Trace(_cpuTempHistory, CpuTraceColor, "cpu", "°C")];
        GpuUtilizationSeries = [Trace(_gpuUsageHistory, GpuTraceColor, "gpu", "%")];
        GpuThermalSeries = [Trace(_gpuTempHistory, GpuTraceColor, "gpu", "°C")];
        MemorySeries = [Trace(_memUsedHistory, MemTraceColor, "mem", " GB")];
        GpuVramSeries = [Trace(_gpuVramUsedHistory, GpuTraceColor, "vram", " GB", "0.0")];
        NetUpSeries = [Spark(_netUpHistory, CpuTraceColor, "up", v => Formatting.BytesPerSecond(v))];
        NetDownSeries = [Spark(_netDownHistory, CpuTraceColor, "down", v => Formatting.BytesPerSecond(v))];
        PingSeries = [Spark(_pingHistory, CpuTraceColor, "ping", v => Formatting.Milliseconds(v))];

        _ = Task.Run(() => RunLoopAsync(_cts.Token));
    }

    private static LineSeries<double> Trace(
        ObservableCollection<double> values, SKColor color, string name, string unit, string numberFormat = "0.#") => new()
    {
        Name = name,
        Values = values,
        Fill = new SolidColorPaint(color.WithAlpha(38)),
        GeometrySize = 0,
        GeometryFill = null,
        GeometryStroke = null,
        LineSmoothness = 0.3,
        Stroke = new SolidColorPaint(color, 2),
        // Hovering the chart reports the reading under the cursor. The tooltip
        // already labels each row with the series name, so only the value here.
        YToolTipLabelFormatter = point =>
            point.Coordinate.PrimaryValue.ToString(numberFormat, CultureInfo.InvariantCulture) + unit,
    };

    private static LineSeries<double> Spark(
        ObservableCollection<double> values, SKColor color, string name, Func<double, string> format) => new()
    {
        Name = name,
        Values = values,
        Fill = new SolidColorPaint(color.WithAlpha(30)),
        GeometrySize = 0,
        GeometryFill = null,
        GeometryStroke = null,
        LineSmoothness = 0.3,
        Stroke = new SolidColorPaint(color, 1.5f),
        YToolTipLabelFormatter = point => format(point.Coordinate.PrimaryValue),
    };

    private static Axis YAxis(double max) => new()
    {
        MinLimit = 0,
        MaxLimit = max,
        TextSize = 10,
        LabelsPaint = new SolidColorPaint(LabelColor),
        SeparatorsPaint = new SolidColorPaint(GridColor) { StrokeThickness = 1 },
    };

    private static Axis XAxis() => new()
    {
        IsVisible = false,
        MinLimit = 0,
        MaxLimit = HistoryLength,
    };

    /// <summary>Reads the version baked into the assembly at build time
    /// (Version in the csproj, overridden at publish for real releases),
    /// so the title bar always shows what actually shipped.</summary>
    private static string ResolveVersion() =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "dev";

    private static Axis HiddenAxis() => new() { IsVisible = false };

    /// <summary>Invisible axis pinned to a fixed range, so sibling sparklines
    /// share one scale instead of each autoscaling to its own peak.</summary>
    private static Axis HiddenAxis(double max) => new() { IsVisible = false, MinLimit = 0, MaxLimit = max };

    /// <summary>Builds a physical core row and its sparkline. Cores are created once and
    /// then mutated in place, so this only runs when the core count changes.</summary>
    private static CoreUsageItem CreateCoreItem(int index)
    {
        var history = new ObservableCollection<double>();
        return new CoreUsageItem(
            index,
            history,
            [Spark(history, CpuTraceColor, $"c{index}", v => Formatting.Percent(v))],
            [XAxis()],
            [HiddenAxis(100)]);
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var snapshot = await CollectAsync(token).ConfigureAwait(false);

            Dispatcher.UIThread.Post(() =>
            {
                CurrentSnapshot = snapshot;

                var perCore = snapshot.CpuPerCoreUsage;

                // Rebuilding the list every tick would throw away each core's sparkline
                // history, so items are only recreated when the core count actually changes.
                if (PerCoreItems.Count != perCore.Length)
                {
                    PerCoreItems.Clear();
                    for (var i = 0; i < perCore.Length; i++)
                    {
                        PerCoreItems.Add(CreateCoreItem(i));
                    }
                }

                for (var i = 0; i < perCore.Length; i++)
                {
                    PerCoreItems[i].Value = perCore[i];
                    AppendHistory(PerCoreItems[i].History, perCore[i]);
                }

                CoreCount = perCore.Length;
                IsHighCoreCount = CoreCount > HighCoreCountThreshold;

                if (snapshot.MemTotalGb > 0)
                {
                    _memYAxis.MaxLimit = snapshot.MemTotalGb;
                }

                if (snapshot.GpuVramTotalMb is > 0)
                {
                    _vramYAxis.MaxLimit = snapshot.GpuVramTotalMb.Value / 1024.0;
                }

                AppendHistory(_cpuUsageHistory, snapshot.CpuUsage);
                AppendHistory(_gpuUsageHistory, snapshot.GpuUsage);
                AppendHistory(_cpuTempHistory, snapshot.CpuTempC ?? 0);
                AppendHistory(_gpuTempHistory, snapshot.GpuTempC ?? 0);
                AppendHistory(_memUsedHistory, snapshot.MemUsedGb);
                AppendHistory(_gpuVramUsedHistory, (snapshot.GpuVramUsedMb ?? 0) / 1024.0);
                AppendHistory(_netUpHistory, snapshot.NetUpBytesPerSec);
                AppendHistory(_netDownHistory, snapshot.NetDownBytesPerSec);
                AppendHistory(_pingHistory, snapshot.PingMs ?? 0);

                LatencyStats = _latency.Add(snapshot.PingMs);
                LatencyWindowLabel = LatencyStats.SampleCount >= LatencyWindowSeconds
                    ? $"last {LatencyWindowSeconds / 60} min"
                    : $"last {LatencyStats.SampleCount}s";
            });

            try
            {
                await Task.Delay(1000, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private static void AppendHistory(ObservableCollection<double> history, double value)
    {
        history.Add(value);
        if (history.Count > HistoryLength)
        {
            history.RemoveAt(0);
        }
    }

    private async Task<HardwareSnapshot> CollectAsync(CancellationToken token)
    {
        var pingTask = SendPingAsync(token);

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
            {
                sub.Update();
            }
        }

        return BuildSnapshot(await pingTask.ConfigureAwait(false));
    }

    private async Task<double?> SendPingAsync(CancellationToken token)
    {
        try
        {
            var reply = await _ping.SendPingAsync(PingHost, 700).WaitAsync(token).ConfigureAwait(false);
            return reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
        }
        catch
        {
            return null;
        }
    }

    private HardwareSnapshot BuildSnapshot(double? pingMs)
    {
        var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        var gpu = _computer.Hardware.FirstOrDefault(h =>
            h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel);
        // LibreHardwareMonitor exposes two Memory entries: "Virtual Memory"
        // (RAM + pagefile commit) and "Total Memory" (actual physical RAM).
        // Taking the first match lands on Virtual Memory and overstates the total.
        var memoryDevices = _computer.Hardware.Where(h => h.HardwareType == HardwareType.Memory).ToList();
        var memory = memoryDevices.FirstOrDefault(h => !h.Name.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                     ?? memoryDevices.FirstOrDefault();

        // Every adapter is its own Network device, so throughput has to be
        // summed across all of them rather than read off whichever comes first.
        var networks = _computer.Hardware.Where(h => h.HardwareType == HardwareType.Network).ToList();

        var cpuName = cpu?.Name ?? "Unknown CPU";
        var cpuUsage = cpu?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total")?.Value ?? 0f;
        var cpuTemp = cpu?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Package"))?.Value;
        var perCore = BuildPhysicalCoreUsage(cpu);

        var gpuName = gpu?.Name ?? "No GPU detected";
        var gpuUsage = gpu?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Core"))?.Value ?? 0f;
        var gpuTemp = gpu?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core"))?.Value;
        var gpuPower = gpu?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power)?.Value;
        var vramUsed = gpu?.Sensors.FirstOrDefault(s => s.Name.Contains("Memory Used", StringComparison.OrdinalIgnoreCase))?.Value;
        var vramTotal = gpu?.Sensors.FirstOrDefault(s => s.Name.Contains("Memory Total", StringComparison.OrdinalIgnoreCase))?.Value;

        var memUsed = memory?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Used")?.Value ?? 0f;
        var memAvailable = memory?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Available")?.Value ?? 0f;

        var upBytes = networks.Sum(n =>
            n.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Upload"))?.Value ?? 0f);
        var downBytes = networks.Sum(n =>
            n.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Download"))?.Value ?? 0f);

        return new HardwareSnapshot(
            cpuName,
            cpuUsage,
            cpuTemp,
            perCore,
            gpuName,
            gpuUsage,
            gpuTemp,
            gpuPower,
            vramUsed,
            vramTotal,
            memUsed,
            memUsed + memAvailable,
            upBytes,
            downBytes,
            pingMs);
    }

    private static double[] BuildPhysicalCoreUsage(IHardware? cpu)
    {
        var loadSensors = cpu?.Sensors
            .Where(s => s.SensorType == SensorType.Load
                        && s.Name.StartsWith("CPU Core", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var threadSensors = loadSensors
            .Where(s => s.Name.Contains("Thread", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var selectedSensors = threadSensors.Length > 0 ? threadSensors : loadSensors;

        var coreLoads = selectedSensors
            .Select(s => new
            {
                Sensor = s,
                CoreIndex = TryParseCoreIndex(s.Name),
                Value = (double)(s.Value ?? 0),
            })
            .ToArray();

        if (coreLoads.Length == 0)
        {
            return [];
        }

        if (coreLoads.All(s => s.CoreIndex is not null))
        {
            return coreLoads
                .GroupBy(s => s.CoreIndex!.Value)
                .OrderBy(g => g.Key)
                .Select(g => g.Average(s => s.Value))
                .ToArray();
        }

        return coreLoads
            .OrderBy(s => s.Sensor.Name, StringComparer.OrdinalIgnoreCase)
            .Select(s => s.Value)
            .ToArray();
    }

    private static int? TryParseCoreIndex(string sensorName)
    {
        var match = Regex.Match(sensorName, @"CPU Core\s*#?(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            ? index
            : null;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _ping.Dispose();
        _computer.Close();
    }
}
