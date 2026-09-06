using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class FormatSelectionService : IFormatSelectionService
{
    public VideoInfo ParseVideoInfo(JsonDocument json, string originalUrl)
    {
        var root = json.RootElement;

        var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
        var title = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "Vídeo sem título" : "Vídeo sem título";
        var channel = root.TryGetProperty("uploader", out var uploaderProp)
            ? uploaderProp.GetString() ?? ""
            : (root.TryGetProperty("channel", out var chProp) ? chProp.GetString() ?? "" : "");

        var duration = root.TryGetProperty("duration", out var durProp) && durProp.TryGetDouble(out var d) ? d : 0;
        var thumbnail = root.TryGetProperty("thumbnail", out var thumbProp) ? thumbProp.GetString() ?? "" : "";
        var uploadDate = root.TryGetProperty("upload_date", out var dateProp) ? dateProp.GetString() ?? "" : "";

        // Formata data YYYYMMDD para DD/MM/YYYY se possível
        if (uploadDate.Length == 8)
        {
            uploadDate = $"{uploadDate.Substring(6, 2)}/{uploadDate.Substring(4, 2)}/{uploadDate.Substring(0, 4)}";
        }

        var rawFormats = new List<VideoFormatRaw>();
        int maxFps = 0;
        int maxHeight = 0;

        if (root.TryGetProperty("formats", out var formatsProp) && formatsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in formatsProp.EnumerateArray())
            {
                var formatRaw = new VideoFormatRaw
                {
                    FormatId = f.TryGetProperty("format_id", out var fid) ? fid.GetString() ?? "" : "",
                    FormatNote = f.TryGetProperty("format_note", out var fn) ? fn.GetString() ?? "" : "",
                    Ext = f.TryGetProperty("ext", out var ext) ? ext.GetString() ?? "" : "",
                    Width = f.TryGetProperty("width", out var w) && w.TryGetInt32(out var widthVal) ? widthVal : null,
                    Height = f.TryGetProperty("height", out var h) && h.TryGetInt32(out var heightVal) ? heightVal : null,
                    Fps = f.TryGetProperty("fps", out var fps) && fps.TryGetDouble(out var fpsVal) ? fpsVal : null,
                    VCodec = f.TryGetProperty("vcodec", out var vc) ? vc.GetString() : null,
                    ACodec = f.TryGetProperty("acodec", out var ac) ? ac.GetString() : null,
                    FileSize = f.TryGetProperty("filesize", out var fs) && fs.TryGetInt64(out var fsVal) ? fsVal : null,
                    FileSizeApprox = f.TryGetProperty("filesize_approx", out var fsa) && fsa.TryGetInt64(out var fsaVal) ? fsaVal : null
                };

                if (formatRaw.Height.HasValue && formatRaw.Height.Value > maxHeight)
                {
                    maxHeight = formatRaw.Height.Value;
                }

                if (formatRaw.Fps.HasValue && (int)Math.Round(formatRaw.Fps.Value) > maxFps)
                {
                    maxFps = (int)Math.Round(formatRaw.Fps.Value);
                }

                rawFormats.Add(formatRaw);
            }
        }

        var maxResName = GetResolutionLabel(maxHeight);

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
