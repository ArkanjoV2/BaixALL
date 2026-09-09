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

    private static readonly HashSet<string> StaticImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "webp", "bmp", "heic", "tiff", "mhtml"
    };

    public bool HasVideo
    {
        get
        {
            // Se possui codec de vídeo explícito (ex: avc1, vp9, gif animado), é um fluxo de vídeo
            if (!string.IsNullOrEmpty(VCodec))
            {
                return !VCodec.Equals("none", StringComparison.OrdinalIgnoreCase);
            }

            // Se vcodec for nulo e a extensão for comprovadamente imagem estática (foto), não é vídeo
            if (!string.IsNullOrEmpty(Ext) && StaticImageExtensions.Contains(Ext))
            {
                return false;
            }

            // Para .gif sem vcodec informado: só é vídeo se possuir FPS de animação ou indicação de vídeo/animação
            if (string.Equals(Ext, "gif", StringComparison.OrdinalIgnoreCase))
            {
                return (Fps.HasValue && Fps.Value > 1) ||
                       FormatNote.Contains("video", StringComparison.OrdinalIgnoreCase) ||
                       FormatNote.Contains("anim", StringComparison.OrdinalIgnoreCase);
            }

            // Sem vcodec explícito: requer dimensões espaciais válidas
            return (Height.HasValue && Height.Value > 0) || (Width.HasValue && Width.Value > 0);
        }
    }

    public bool HasAudio
    {
        get
        {
            if (!string.IsNullOrEmpty(Ext) && (StaticImageExtensions.Contains(Ext) || string.Equals(Ext, "gif", StringComparison.OrdinalIgnoreCase)))
                return false;

            if (!string.IsNullOrEmpty(ACodec))
                return !ACodec.Equals("none", StringComparison.OrdinalIgnoreCase);

            return (AudioChannels.HasValue && AudioChannels.Value > 0) ||
                   (Abr.HasValue && Abr.Value > 0) ||
                   (Asr.HasValue && Asr.Value > 0);
        }
    }

    private static bool IsKnownVideoExtension(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext)) return false;
        return ext.Equals("mp4", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("webm", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("mkv", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("mov", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("m4v", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("ts", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("m3u8", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKnownAudioExtension(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext)) return false;
        return ext.Equals("m4a", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("mp3", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("aac", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("opus", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("ogg", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("flac", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals("wav", StringComparison.OrdinalIgnoreCase);
    }
}
