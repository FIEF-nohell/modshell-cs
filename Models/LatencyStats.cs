namespace modshell_hwtest.Models;

/// <summary>
/// Aggregate view of the ping window: a single reading says almost nothing
/// about a connection, so the panel reports distribution and loss instead.
/// Every latency figure is null while no successful reply is in the window.
/// </summary>
/// <param name="Average">Mean of the successful replies in the window.</param>
/// <param name="P50">Median. Half the replies were at least this fast.</param>
/// <param name="P95">Nearest-rank 95th percentile: the bad-but-not-rare case.</param>
/// <param name="P99">Nearest-rank 99th percentile: the worst case that still repeats.</param>
/// <param name="Jitter">
/// Mean absolute difference between consecutive successful replies. Low average
/// with high jitter is what actually breaks calls and games, so it gets its own line.
/// </param>
/// <param name="LossPercent">Share of attempts in the window that got no reply.</param>
/// <param name="SampleCount">Attempts currently in the window, successful or not.</param>
public record LatencyStats(
    double? Average,
    double? Min,
    double? Max,
    double? P50,
    double? P95,
    double? P99,
    double? Jitter,
    double LossPercent,
    int SampleCount)
{
    public static readonly LatencyStats Empty = new(null, null, null, null, null, null, null, 0, 0);
}
