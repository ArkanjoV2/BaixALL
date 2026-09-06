using System;

namespace BaixALL.App.Helpers;

public static class ByteSizeFormatter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    /// <summary>
    /// Formata bytes numéricos em representação legível (ex: 15.4 MB, 1.2 GB).
    /// </summary>
    public static string Format(double bytes)
    {
        if (bytes <= 0)
            return "0 B";

        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < Units.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} {Units[order]}";
    }

    /// <summary>
    /// Formata velocidade em bytes/segundo (ex: 4.5 MB/s).
    /// </summary>
    public static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond <= 0)
            return "--";

        return $"{Format(bytesPerSecond)}/s";
    }
}
