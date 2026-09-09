using System;
using System.Collections.Generic;

namespace BaixALL.App.Models;

public class VideoInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public double? DurationSeconds { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string UploadDate { get; set; } = string.Empty;
    public string MaxResolution { get; set; } = string.Empty;
    public int? MaxFps { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public PlatformType Platform { get; set; } = PlatformType.YouTube;
    public string Author => Channel;
    public string CanonicalKey { get; set; } = string.Empty;
    public List<VideoFormatRaw> Formats { get; set; } = new();

    public string FormattedDuration
    {
        get
        {
            if (!DurationSeconds.HasValue || DurationSeconds.Value <= 0)
            {
                return "—";
            }

            var ts = TimeSpan.FromSeconds(DurationSeconds.Value);
            return ts.TotalHours >= 1
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"m\:ss");
        }
    }

    public string FormattedFps => MaxFps.HasValue && MaxFps.Value > 0 ? $"{MaxFps.Value} FPS" : string.Empty;
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
    public int? Asr { get; set; }
    public int? AudioChannels { get; set; }

    public bool HasVideo => (!string.IsNullOrEmpty(VCodec) && VCodec != "none") ||
                            (string.IsNullOrEmpty(VCodec) && ((Height.HasValue && Height.Value > 0) || (Width.HasValue && Width.Value > 0)));
    public bool HasAudio => (!string.IsNullOrEmpty(ACodec) && ACodec != "none") ||
                            (string.IsNullOrEmpty(ACodec) && ((AudioChannels.HasValue && AudioChannels.Value > 0) || (Abr.HasValue && Abr.Value > 0) || (Asr.HasValue && Asr.Value > 0)));
}
