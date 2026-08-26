using System;
using Avalonia;
using Avalonia.Media;

namespace modshell_hwtest.Views;

public static class GaugeArc
{
    public static StreamGeometry? Build(double centerX, double centerY, double radius, double startDeg, double sweepDeg)
    {
        if (sweepDeg <= 0)
        {
            return null;
        }

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();

        var startRad = startDeg * Math.PI / 180.0;
        var endRad = (startDeg + sweepDeg) * Math.PI / 180.0;
        var start = new Point(centerX + radius * Math.Cos(startRad), centerY + radius * Math.Sin(startRad));
        var end = new Point(centerX + radius * Math.Cos(endRad), centerY + radius * Math.Sin(endRad));
        var isLargeArc = sweepDeg > 180;

        ctx.BeginFigure(start, false);
        ctx.ArcTo(end, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise);
        ctx.EndFigure(false);

        return geometry;
    }
}
