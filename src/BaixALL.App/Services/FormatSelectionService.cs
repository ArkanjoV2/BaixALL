using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BaixALL.App.Helpers;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class FormatSelectionService : IFormatSelectionService
{
    public VideoInfo ParseVideoInfo(JsonDocument json, string originalUrl)
    {
        var root = json.RootElement;

        var id = root.GetStringSafe("id");
        var title = root.GetStringSafe("title", "Vídeo sem título");
        var channel = root.GetStringNullable("uploader")
            ?? root.GetStringNullable("channel")
            ?? root.GetStringSafe("uploader_id", "");

        var duration = root.GetDoubleNullable("duration");
        var thumbnail = root.GetStringSafe("thumbnail");
        var uploadDate = root.GetStringSafe("upload_date");

        // Formata data YYYYMMDD para DD/MM/YYYY se possível
        if (uploadDate.Length == 8 && int.TryParse(uploadDate, out _))
        {
            uploadDate = $"{uploadDate.Substring(6, 2)}/{uploadDate.Substring(4, 2)}/{uploadDate.Substring(0, 4)}";
        }

        var rawFormats = new List<VideoFormatRaw>();
        int? maxFps = null;
        int maxHeight = 0;

        if (root.TryGetProperty("formats", out var formatsProp) && formatsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in formatsProp.EnumerateArray())
            {
                if (f.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var formatRaw = new VideoFormatRaw
                {
                    FormatId = f.GetStringSafe("format_id"),
                    FormatNote = f.GetStringSafe("format_note"),
                    Ext = f.GetStringSafe("ext"),
                    Width = f.GetInt32Nullable("width"),
                    Height = f.GetInt32Nullable("height"),
                    Fps = f.GetDoubleNullable("fps"),
                    VCodec = f.GetStringNullable("vcodec"),
                    ACodec = f.GetStringNullable("acodec"),
                    FileSize = f.GetInt64Nullable("filesize"),
                    FileSizeApprox = f.GetInt64Nullable("filesize_approx"),
                    Tbr = f.GetDoubleNullable("tbr"),
                    Vbr = f.GetDoubleNullable("vbr"),
                    Abr = f.GetDoubleNullable("abr"),
                    Asr = f.GetInt32Nullable("asr"),
                    AudioChannels = f.GetInt32Nullable("audio_channels")
                };

                if (formatRaw.Height.HasValue && formatRaw.Height.Value > maxHeight)
                {
                    maxHeight = formatRaw.Height.Value;
                }

                if (formatRaw.Fps.HasValue)
                {
                    var roundedFps = (int)Math.Round(formatRaw.Fps.Value);
                    if (!maxFps.HasValue || roundedFps > maxFps.Value)
                    {
                        maxFps = roundedFps;
                    }
                }

                rawFormats.Add(formatRaw);
            }
        }

        var maxResName = maxHeight > 0 ? GetResolutionLabel(maxHeight) : "Automática";

        return new VideoInfo
        {
            Id = id,
            Title = title,
            Channel = channel,
            DurationSeconds = duration,
            ThumbnailUrl = thumbnail,
            UploadDate = uploadDate,
            MaxResolution = maxResName,
            MaxFps = maxFps,
            OriginalUrl = originalUrl,
            Formats = rawFormats
        };
    }

    public List<FormatOption> BuildFormatOptions(VideoInfo info)
    {
        var options = new List<FormatOption>();

        // Filtra os formatos que têm fluxo de vídeo
        var videoFormats = info.Formats
            .Where(f => f.HasVideo && f.Height.HasValue && f.Height.Value > 0)
            .ToList();

        // Identifica alturas únicas presentes no vídeo
        var uniqueHeights = videoFormats
            .Select(f => f.Height!.Value)
            .Distinct()
            .OrderByDescending(h => h)
            .ToList();

        // 1. Opção Padrão: "Melhor qualidade disponível"
        var bestHeight = uniqueHeights.FirstOrDefault();
        var bestFps = videoFormats
            .Where(f => f.Height == bestHeight && f.Fps.HasValue)
            .Select(f => (int)Math.Round(f.Fps!.Value))
            .DefaultIfEmpty(0)
            .Max();

        var bestLabel = "Melhor qualidade disponível";
        if (bestHeight > 0)
        {
            var resName = GetResolutionLabel(bestHeight);
            bestLabel = bestFps > 30
                ? $"Melhor qualidade disponível ({resName} • {bestFps} FPS)"
                : $"Melhor qualidade disponível ({resName})";
        }

        options.Add(new FormatOption
        {
            Label = bestLabel,
            Height = bestHeight > 0 ? bestHeight : null,
            Fps = bestFps > 0 ? bestFps : null,
            FormatSelector = "bestvideo+bestaudio/best",
            IsBestQuality = true,
            IsAudioOnly = false,
            Description = "Combina melhor faixa de vídeo e melhor faixa de áudio originais com FFmpeg."
        });

        // 2. Opções específicas por resolução identificada
        foreach (var height in uniqueHeights)
        {
            var formatsForHeight = videoFormats.Where(f => f.Height == height).ToList();
            var maxFpsForHeight = formatsForHeight
                .Where(f => f.Fps.HasValue)
                .Select(f => (int)Math.Round(f.Fps!.Value))
                .DefaultIfEmpty(0)
                .Max();

            var resName = GetResolutionLabel(height);
            var label = maxFpsForHeight > 30
                ? $"{resName} • {maxFpsForHeight} FPS"
                : resName;

            options.Add(new FormatOption
            {
                Label = label,
                Height = height,
                Fps = maxFpsForHeight > 0 ? maxFpsForHeight : null,
                FormatSelector = $"bestvideo[height<={height}]+bestaudio/best[height<={height}]/best",
                IsBestQuality = false,
                IsAudioOnly = false,
                Description = $"Vídeo até {resName} com áudio original."
            });
        }

        // 3. Opção: Somente Áudio
        options.Add(new FormatOption
        {
            Label = "🎵 Somente áudio",
            FormatSelector = "bestaudio/best",
            IsBestQuality = false,
            IsAudioOnly = true,
            Description = "Extrai apenas a melhor trilha de áudio disponível."
        });

        return options;
    }

    public static string GetResolutionLabel(int height)
    {
        return height switch
        {
            >= 4320 => "8K (4320p)",
            >= 2160 => "4K (2160p)",
            >= 1440 => "1440p (2K)",
            >= 1080 => "1080p (Full HD)",
            >= 720 => "720p (HD)",
            >= 480 => "480p",
            >= 360 => "360p",
            >= 240 => "240p",
            >= 144 => "144p",
            _ => height > 0 ? $"{height}p" : "Desconhecida"
        };
    }
}
