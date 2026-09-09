using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BaixALL.App.Helpers;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class FormatSelectionService : IFormatSelectionService
{
    private readonly IPlatformService _platformService;

    public FormatSelectionService(IPlatformService? platformService = null)
    {
        _platformService = platformService ?? new PlatformService();
    }

    public VideoInfo ParseVideoInfo(JsonDocument json, string originalUrl)
    {
        var root = json.RootElement;

        var platform = _platformService.DetectPlatform(originalUrl);
        var id = root.GetStringSafe("id");
        var title = root.GetStringSafe("title", "Vídeo sem título");
        var channel = root.GetStringNullable("uploader")
            ?? root.GetStringNullable("channel")
            ?? root.GetStringSafe("uploader_id", "");

        title = title.Replace("\r\n", " ").Replace("\n", " ").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = platform switch
            {
                PlatformType.Instagram => $"Vídeo do Instagram ({id})",
                PlatformType.Twitter => $"Vídeo do X/Twitter ({id})",
                _ => "Vídeo sem título"
            };
        }

        if (string.IsNullOrWhiteSpace(channel))
        {
            channel = platform switch
            {
                PlatformType.Instagram => "Instagram",
                PlatformType.Twitter => "X / Twitter",
                _ => string.Empty
            };
        }

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
        int? maxWidth = null;

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

                if (formatRaw.Width.HasValue && (!maxWidth.HasValue || formatRaw.Width.Value > maxWidth.Value))
                {
                    maxWidth = formatRaw.Width.Value;
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

        var maxResName = maxHeight > 0 ? GetResolutionLabel(maxHeight, maxWidth) : "Automática";
        var canonicalKey = _platformService.GetCanonicalKey(originalUrl, platform, id);

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
            Platform = platform,
            CanonicalKey = canonicalKey,
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
            var fpsList = formatsForHeight
                .Where(f => f.Fps.HasValue && f.Fps.Value > 0)
                .Select(f => (int)Math.Round(f.Fps!.Value))
                .Distinct()
                .OrderByDescending(fps => fps)
                .ToList();

            var resName = GetResolutionLabel(height);

            if (fpsList.Count > 1)
            {
                // Múltiplas taxas de quadros disponíveis para esta resolução (ex: 60 FPS e 30 FPS)
                foreach (var fps in fpsList)
                {
                    var label = $"{resName} • {fps} FPS";
                    var selector = fps > 30
                        ? $"bestvideo[height<={height}][fps>{30}]+bestaudio/bestvideo[height<={height}]+bestaudio/best"
                        : $"bestvideo[height<={height}][fps<={fps}]+bestaudio/bestvideo[height<={height}]+bestaudio/best";

                    options.Add(new FormatOption
                    {
                        Label = label,
                        Height = height,
                        Fps = fps,
                        FormatSelector = selector,
                        IsBestQuality = false,
                        IsAudioOnly = false,
                        Description = $"Vídeo {resName} a {fps} FPS com áudio original."
                    });
                }
            }
            else
            {
                var fps = fpsList.FirstOrDefault();
                var label = fps > 30
                    ? $"{resName} • {fps} FPS"
                    : resName;

                options.Add(new FormatOption
                {
                    Label = label,
                    Height = height,
                    Fps = fps > 0 ? fps : null,
                    FormatSelector = $"bestvideo[height<={height}]+bestaudio/best[height<={height}]/best",
                    IsBestQuality = false,
                    IsAudioOnly = false,
                    Description = $"Vídeo até {resName} com áudio original."
                });
            }
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

    public PlaylistInfo ParsePlaylistInfo(JsonDocument json, string originalUrl)
    {
        var root = json.RootElement;
        var platform = _platformService.DetectPlatform(originalUrl);
        var isCarousel = (platform == PlatformType.Instagram || platform == PlatformType.Twitter);

        var id = root.GetStringSafe("id");
        var title = root.GetStringSafe("title", isCarousel ? "Publicação sem título" : "Playlist sem título");
        var channel = root.GetStringNullable("uploader")
            ?? root.GetStringNullable("channel")
            ?? root.GetStringSafe("uploader_id", platform == PlatformType.YouTube ? "YouTube" : platform.ToString());

        if (isCarousel)
        {
            if (title.StartsWith("Post by", StringComparison.OrdinalIgnoreCase))
            {
                title = platform == PlatformType.Instagram
                    ? $"Carrossel do Instagram ({channel})"
                    : $"Publicação do X/Twitter ({channel})";
            }
        }

        var thumbnail = root.GetStringSafe("thumbnail");
        if (string.IsNullOrEmpty(thumbnail) && root.TryGetProperty("thumbnails", out var rootThumbs) && rootThumbs.ValueKind == JsonValueKind.Array)
        {
            foreach (var th in rootThumbs.EnumerateArray())
            {
                var url = th.GetStringSafe("url");
                if (!string.IsNullOrEmpty(url)) thumbnail = url;
            }
        }

        var items = new List<PlaylistItemInfo>();
        int index = 1;

        if (root.TryGetProperty("entries", out var entriesProp) && entriesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in entriesProp.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                    continue;

                var entryId = entry.GetStringSafe("id");
                var entryTitle = entry.GetStringSafe("title", $"Vídeo #{index}");
                var duration = entry.GetDoubleNullable("duration");
                var entryChannel = entry.GetStringNullable("uploader")
                    ?? entry.GetStringNullable("channel")
                    ?? channel;

                var entryExt = entry.GetStringSafe("ext").ToLowerInvariant();
                var formatNote = entry.GetStringSafe("format_note").ToLowerInvariant();
                var vcodec = entry.GetStringSafe("vcodec").ToLowerInvariant();
                var acodec = entry.GetStringSafe("acodec").ToLowerInvariant();

                bool isPhoto = (entryExt == "jpg" || entryExt == "jpeg" || entryExt == "png" || entryExt == "webp" || entryExt == "heic") ||
                               formatNote.Contains("photo") ||
                               (vcodec == "none" && acodec == "none" && !entry.TryGetProperty("formats", out _));

                var entryUrl = entry.GetStringSafe("url");
                if (string.IsNullOrWhiteSpace(entryUrl) || !entryUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(entryId))
                    {
                        entryUrl = platform switch
                        {
                            PlatformType.Instagram => $"https://www.instagram.com/p/{entryId}/",
                            PlatformType.Twitter => $"https://x.com/i/status/{entryId}",
                            _ => $"https://www.youtube.com/watch?v={entryId}"
                        };
                    }
                    else
                    {
                        entryUrl = string.Empty;
                    }
                }

                var entryThumb = entry.GetStringSafe("thumbnail");
                if (string.IsNullOrEmpty(entryThumb) && entry.TryGetProperty("thumbnails", out var entryThumbs) && entryThumbs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var th in entryThumbs.EnumerateArray())
                    {
                        var u = th.GetStringSafe("url");
                        if (!string.IsNullOrEmpty(u)) entryThumb = u;
                    }
                }

                if (string.IsNullOrEmpty(entryThumb) && !string.IsNullOrEmpty(entryId) && platform == PlatformType.YouTube)
                {
                    entryThumb = $"https://i.ytimg.com/vi/{entryId}/hqdefault.jpg";
                }

                // Se a playlist ainda não tiver miniatura definida, aproveita a do primeiro vídeo
                if (string.IsNullOrEmpty(thumbnail) && !string.IsNullOrEmpty(entryThumb))
                {
                    thumbnail = entryThumb;
                }

                bool isUnavailable = string.IsNullOrEmpty(entryId) ||
                                     entryTitle.Contains("[Private video]", StringComparison.OrdinalIgnoreCase) ||
                                     entryTitle.Contains("[Deleted video]", StringComparison.OrdinalIgnoreCase);

                string notice = string.Empty;
                bool isItemAvailable = !isUnavailable;
                bool isItemSelected = !isUnavailable;

                if (isPhoto)
                {
                    isItemAvailable = false;
                    isItemSelected = false;
                    notice = "Foto (download de imagens em carrossel planejado para versão futura)";
                }
                else if (isUnavailable)
                {
                    notice = "Vídeo indisponível ou privado";
                }

                var item = new PlaylistItemInfo
                {
                    Id = entryId,
                    Title = entryTitle,
                    VideoUrl = entryUrl,
                    Channel = entryChannel,
                    DurationSeconds = duration,
                    ThumbnailUrl = entryThumb,
                    PlaylistIndex = index,
                    Platform = platform,
                    CanonicalKey = _platformService.GetCanonicalKey(
                        !string.IsNullOrEmpty(entryUrl) ? entryUrl : originalUrl,
                        platform,
                        entryId,
                        index),
                    IsAvailable = isItemAvailable,
                    IsSelected = isItemSelected,
                    AvailabilityNotice = notice
                };

                items.Add(item);
                index++;
            }
        }

        var playlistInfo = new PlaylistInfo
        {
            Id = id,
            Title = title,
            Channel = channel,
            ThumbnailUrl = thumbnail,
            OriginalUrl = originalUrl,
            Platform = platform,
            IsCarousel = isCarousel,
            TotalVideosCount = items.Count,
            Items = items
        };
        playlistInfo.UpdateCounts();
        return playlistInfo;
    }

    public List<FormatOption> BuildBatchFormatOptions()
    {
        return new List<FormatOption>
        {
            new()
            {
                Label = "Melhor qualidade disponível (Recomendado)",
                FormatSelector = "bestvideo+bestaudio/best",
                IsBestQuality = true,
                IsAudioOnly = false,
                Description = "Baixa a melhor resolução e áudio disponíveis de cada vídeo."
            },
            new()
            {
                Label = "Até 1080p (Full HD)",
                Height = 1080,
                FormatSelector = "bestvideo[height<=1080]+bestaudio/bestvideo[height<=1080]+bestaudio/best",
                IsBestQuality = false,
                IsAudioOnly = false,
                Description = "Limita a resolução a no máximo 1080p com fallback automático."
            },
            new()
            {
                Label = "Até 720p (HD)",
                Height = 720,
                FormatSelector = "bestvideo[height<=720]+bestaudio/bestvideo[height<=720]+bestaudio/best",
                IsBestQuality = false,
                IsAudioOnly = false,
                Description = "Limita a resolução a no máximo 720p com fallback automático."
            },
            new()
            {
                Label = "Até 480p",
                Height = 480,
                FormatSelector = "bestvideo[height<=480]+bestaudio/bestvideo[height<=480]+bestaudio/best",
                IsBestQuality = false,
                IsAudioOnly = false,
                Description = "Vídeo com tamanho reduzido até 480p."
            },
            new()
            {
                Label = "Até 360p",
                Height = 360,
                FormatSelector = "bestvideo[height<=360]+bestaudio/bestvideo[height<=360]+bestaudio/best",
                IsBestQuality = false,
                IsAudioOnly = false,
                Description = "Vídeo leve para conexões limitadas ou economia de espaço."
            },
            new()
            {
                Label = "🎵 Somente áudio",
                FormatSelector = "bestaudio/best",
                IsBestQuality = false,
                IsAudioOnly = true,
                Description = "Extrai apenas as faixas de áudio dos vídeos da playlist."
            }
        };
    }

    public static string GetResolutionLabel(int height, int? width = null)
    {
        if (width.HasValue && width.Value > 0 && width.Value < height)
        {
            var baseRes = GetResolutionLabel(width.Value);
            return $"{baseRes} Vertical ({width.Value}x{height})";
        }

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
