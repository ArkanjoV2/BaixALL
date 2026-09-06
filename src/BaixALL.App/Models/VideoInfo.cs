using System;
using System.Collections.Generic;

namespace BaixALL.App.Models;

public class VideoInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string UploadDate { get; set; } = string.Empty;
    public string MaxResolution { get; set; } = string.Empty;
    public int MaxFps { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public List<VideoFormatRaw> Formats { get; set; } = new();

    public string FormattedDuration
    {
        get
        {
            var ts = TimeSpan.FromSeconds(DurationSeconds);
            return ts.TotalHours >= 1
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"m\:ss");
        }
    }

    public string FormattedFps => MaxFps > 0 ? $"{MaxFps} FPS" : string.Empty;
}

public class VideoFormatRaw
{
    public string FormatId { get; set; } = string.Empty;
    public string FormatNote { get; set; } = string.Empty;
    public string Ext { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? Fps { get; set; }
    public string? VCodec { get; set; }
    public string? ACodec { get; set; }
    public long? FileSize { get; set; }
    public long? FileSizeApprox { get; set; }
    public double? Tbr { get; set; }
    public double? Vbr { get; set; }
    public double? Abr { get; set; }

    public bool HasVideo => !string.IsNullOrEmpty(VCodec) && VCodec != "none";
    public bool HasAudio => !string.IsNullOrEmpty(ACodec) && ACodec != "none";
}
