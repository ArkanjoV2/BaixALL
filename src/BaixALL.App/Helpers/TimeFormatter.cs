using System;

namespace BaixALL.App.Helpers;

public static class TimeFormatter
{
    /// <summary>
    /// Formata segundos restantes de ETA (ex: "01:25", "1h 10m", "< 1s").
    /// </summary>
    public static string FormatEta(double? etaSeconds)
    {
        if (!etaSeconds.HasValue || double.IsInfinity(etaSeconds.Value) || double.IsNaN(etaSeconds.Value) || etaSeconds.Value <= 0)
            return "--";

        var ts = TimeSpan.FromSeconds(etaSeconds.Value);

        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";

        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}s";

        return $"{ts.Seconds}s";
    }

    /// <summary>
    /// Formata duração do vídeo (ex: "03:45", "1:15:30").
    /// </summary>
    public static string FormatDuration(double seconds)
    {
        if (seconds <= 0)
            return "00:00";

        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
    }
}
