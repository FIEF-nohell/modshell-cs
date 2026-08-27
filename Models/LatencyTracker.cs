using System;
using System.Collections.Generic;
using System.Linq;

namespace modshell_hwtest.Models;

/// <summary>
/// Rolling window of ping attempts, and the single place latency statistics
/// are derived. Failed attempts are kept in the window as nulls so the loss
/// rate stays honest: dropping them would make a flaky link look merely slow.
/// </summary>
public sealed class LatencyTracker
{
    private readonly Queue<double?> _attempts = new();
    private readonly int _capacity;

    /// <param name="capacity">
    /// Attempts to retain. The collection loop samples once a second, so this
    /// doubles as the window length in seconds.
    /// </param>
    public LatencyTracker(int capacity) => _capacity = capacity;

    public LatencyStats Add(double? replyMs)
    {
        _attempts.Enqueue(replyMs);
        while (_attempts.Count > _capacity)
        {
            _attempts.Dequeue();
        }

        return Compute();
    }

    private LatencyStats Compute()
    {
        var total = _attempts.Count;
        if (total == 0)
        {
            return LatencyStats.Empty;
        }

        // Replies in arrival order: jitter needs the ordering, the percentiles
        // need a sorted copy. One materialization feeds both.
        var replies = _attempts.Where(a => a.HasValue).Select(a => a!.Value).ToArray();
        var lossPercent = (total - replies.Length) / (double)total * 100;

        if (replies.Length == 0)
        {
            return LatencyStats.Empty with { LossPercent = lossPercent, SampleCount = total };
        }

        var sorted = replies.Order().ToArray();

        return new LatencyStats(
            replies.Average(),
            sorted[0],
            sorted[^1],
            Percentile(sorted, 50),
            Percentile(sorted, 95),
            Percentile(sorted, 99),
            Jitter(replies),
            lossPercent,
            total);
    }

    /// <summary>Nearest-rank percentile over an ascending array.</summary>
    private static double Percentile(double[] sorted, double percent)
    {
        var rank = (int)Math.Ceiling(percent / 100 * sorted.Length);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Length - 1)];
    }

    /// <summary>
    /// Mean absolute difference between consecutive replies. Dropped attempts
    /// are skipped rather than treated as a gap, so a timeout does not register
    /// as a latency swing on either side of it.
    /// </summary>
    private static double? Jitter(double[] replies)
    {
        if (replies.Length < 2)
        {
            return null;
        }

        var sum = 0d;
        for (var i = 1; i < replies.Length; i++)
        {
            sum += Math.Abs(replies[i] - replies[i - 1]);
        }

        return sum / (replies.Length - 1);
    }
}
