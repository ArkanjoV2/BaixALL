using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BaixALL.App.Helpers;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class FormatSelectionService : IFormatSelectionService
{
    private readonly IPlatformService _platformService;

    private static readonly Regex TikTokPlayBitrateFormatIdRegex = new(
        @"^(?:h264|h265|bytevc1|hevc|avc1)_\d+p_\d+(?:-\d+)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TikTokPlayAddrFormatIdRegex = new(
        @"^play_?addr(?:_(?:h264|h265|bytevc1|hevc|avc1))?(?:-\d+)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public FormatSelectionService(IPlatformService? platformService = null)
    {
        _platformService = platformService ?? new PlatformService();
    }

    public static WatermarkStatus ClassifyTikTokFormatWatermark(VideoFormatRaw format)
    {
        if (format.IsUnplayable || !format.HasVideo)
        {
            return WatermarkStatus.NotApplicable;
        }

        var formatId = (format.FormatId ?? string.Empty).Trim();
        var formatNote = (format.FormatNote ?? string.Empty).Trim();

        bool hasExplicitNoWatermarkNote =
            formatNote.Contains("no watermark", StringComparison.OrdinalIgnoreCase) ||
            formatNote.Contains("without watermark", StringComparison.OrdinalIgnoreCase) ||
            formatNote.Contains("unwatermarked", StringComparison.OrdinalIgnoreCase) ||
            formatNote.Contains("nowm", StringComparison.OrdinalIgnoreCase);

        bool hasExplicitWatermarkIndicator =
            !hasExplicitNoWatermarkNote &&
            (formatNote.Contains("watermark", StringComparison.OrdinalIgnoreCase) ||
             formatId.Contains("watermark", StringComparison.OrdinalIgnoreCase));

        if (hasExplicitWatermarkIndicator)
        {
            return WatermarkStatus.Watermarked;
        }

        bool isDownloadEndpointFormat =
            formatId.Equals("download", StringComparison.OrdinalIgnoreCase) ||
            formatId.Equals("download_addr", StringComparison.OrdinalIgnoreCase) ||
            formatId.StartsWith("download_", StringComparison.OrdinalIgnoreCase) ||
            formatId.StartsWith("download-", StringComparison.OrdinalIgnoreCase) ||
            formatNote.Contains("Download video", StringComparison.OrdinalIgnoreCase);

        if (isDownloadEndpointFormat && !hasExplicitNoWatermarkNote)
        {
            // download / download_addr sem confirmação explícita de marca-d'água nem de ausência de marca
            return WatermarkStatus.Unknown;
        }

        bool hasExplicitDirectPlaybackNote =
            formatNote.Contains("Direct video", StringComparison.OrdinalIgnoreCase) ||
            formatNote.Contains("Playback video", StringComparison.OrdinalIgnoreCase);

        bool hasIdentifiedCleanPlaybackId =
            TikTokPlayBitrateFormatIdRegex.IsMatch(formatId) ||
            TikTokPlayAddrFormatIdRegex.IsMatch(formatId);

        if (hasExplicitNoWatermarkNote || hasExplicitDirectPlaybackNote || hasIdentifiedCleanPlaybackId)
        {
            return WatermarkStatus.NoWatermark;
        }

        return WatermarkStatus.Unknown;
    }

    public VideoInfo ParseVideoInfo(JsonDocument json, string originalUrl)
    {
        var root = json.RootElement;

        var platform = _platformService.DetectPlatform(originalUrl);
        var webpageUrl = root.GetStringNullable("webpage_url") ?? string.Empty;
        var extractorKey = root.GetStringNullable("extractor_key") ?? root.GetStringNullable("extractor") ?? string.Empty;

        if (platform == PlatformType.Unknown)
        {
            if (extractorKey.Contains("TikTok", StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(webpageUrl) && _platformService.DetectPlatform(webpageUrl) == PlatformType.TikTok))
            {
                platform = PlatformType.TikTok;
            }
            else if (!string.IsNullOrWhiteSpace(webpageUrl))
            {
                platform = _platformService.DetectPlatform(webpageUrl);
            }
        }

        var id = root.GetStringSafe("id");
        var rawTitle = root.GetStringSafe("title", "");
        var rawDescription = (root.GetStringNullable("description") ?? string.Empty)
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Trim();

        var uploader = root.GetStringNullable("uploader")?.Trim();
        var channelName = root.GetStringNullable("channel")?.Trim()
            ?? root.GetStringNullable("creator")?.Trim();
        var uploaderId = root.GetStringSafe("uploader_id", "").Trim();

        string channel;
        if (platform == PlatformType.TikTok)
        {
            var handle = !string.IsNullOrWhiteSpace(uploader) ? $"@{uploader.TrimStart('@')}" : string.Empty;
            if (!string.IsNullOrWhiteSpace(channelName) && !string.IsNullOrWhiteSpace(handle))
            {
                channel = string.Equals(channelName.TrimStart('@'), uploader!.TrimStart('@'), StringComparison.OrdinalIgnoreCase)
                    ? handle
                    : $"{channelName} ({handle})";
            }
            else if (!string.IsNullOrWhiteSpace(handle))
            {
                channel = handle;
            }
            else if (!string.IsNullOrWhiteSpace(channelName))
            {
                channel = channelName;
            }
            else
            {
                channel = !string.IsNullOrWhiteSpace(uploaderId) ? $"@{uploaderId.TrimStart('@')}" : "TikTok";
            }
        }
        else
        {
            channel = uploader
                ?? channelName
                ?? uploaderId;
        }

        var title = rawTitle.Replace("\r\n", " ").Replace("\n", " ").Trim();
        if (platform == PlatformType.TikTok)
        {
            bool isGenericTikTokTitle =
                string.IsNullOrWhiteSpace(title) ||
                title.StartsWith("TikTok video #", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(title, "Vídeo sem título", StringComparison.OrdinalIgnoreCase);

            if (isGenericTikTokTitle && !string.IsNullOrWhiteSpace(rawDescription))
            {
                title = rawDescription.Length > 100
                    ? rawDescription.Substring(0, 97) + "..."
                    : rawDescription;
            }
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = platform switch
            {
                PlatformType.Instagram => $"Vídeo do Instagram ({id})",
                PlatformType.Twitter => $"Vídeo do X/Twitter ({id})",
                PlatformType.TikTok => !string.IsNullOrWhiteSpace(id) ? $"Vídeo do TikTok ({id})" : "Vídeo do TikTok",
                _ => "Vídeo sem título"
            };
        }

        if (string.IsNullOrWhiteSpace(channel))
        {
            channel = platform switch
            {
                PlatformType.Instagram => "Instagram",
                PlatformType.Twitter => "X / Twitter",
                PlatformType.TikTok => "TikTok",
                _ => string.Empty
            };
        }

        var duration = root.GetDoubleNullable("duration");
        var thumbnail = root.GetStringSafe("thumbnail");
        if (string.IsNullOrWhiteSpace(thumbnail) &&
            root.TryGetProperty("thumbnails", out var thumbsProp) &&
            thumbsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var th in thumbsProp.EnumerateArray())
            {
                if (th.ValueKind != JsonValueKind.Object) continue;
                var thUrl = th.GetStringSafe("url");
                if (!string.IsNullOrWhiteSpace(thUrl))
                {
                    thumbnail = thUrl;
                }
            }
        }

        var uploadDate = root.GetStringSafe("upload_date");

        // Formata data YYYYMMDD para DD/MM/YYYY se possível
        if (uploadDate.Length == 8 && int.TryParse(uploadDate, out _))
        {
            uploadDate = $"{uploadDate.Substring(6, 2)}/{uploadDate.Substring(4, 2)}/{uploadDate.Substring(0, 4)}";
        }

        var rawFormats = new List<VideoFormatRaw>();

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
                    AudioChannels = f.GetInt32Nullable("audio_channels"),
                    Preference = f.GetInt32Nullable("preference"),
                    Quality = f.GetDoubleNullable("quality")
                };

                if (platform == PlatformType.TikTok)
                {
                    formatRaw.WatermarkStatus = ClassifyTikTokFormatWatermark(formatRaw);
                }

                rawFormats.Add(formatRaw);
            }
        }

        WatermarkStatus videoWatermarkStatus = WatermarkStatus.NotApplicable;
        List<VideoFormatRaw> formatsForMetrics;

        if (platform == PlatformType.TikTok)
        {
            var cleanVideoFormats = rawFormats.Where(f => f.HasVideo && f.WatermarkStatus == WatermarkStatus.NoWatermark).ToList();
            var unknownVideoFormats = rawFormats.Where(f => f.HasVideo && f.WatermarkStatus == WatermarkStatus.Unknown).ToList();
            var watermarkedVideoFormats = rawFormats.Where(f => f.HasVideo && f.WatermarkStatus == WatermarkStatus.Watermarked).ToList();

            if (cleanVideoFormats.Count > 0)
            {
                videoWatermarkStatus = WatermarkStatus.NoWatermark;
                formatsForMetrics = cleanVideoFormats;
            }
            else if (unknownVideoFormats.Count > 0)
            {
                videoWatermarkStatus = WatermarkStatus.Unknown;
                formatsForMetrics = unknownVideoFormats;
            }
            else if (watermarkedVideoFormats.Count > 0)
            {
                videoWatermarkStatus = WatermarkStatus.Watermarked;
                formatsForMetrics = watermarkedVideoFormats;
            }
            else
            {
                videoWatermarkStatus = WatermarkStatus.NotApplicable;
                formatsForMetrics = rawFormats;
            }
        }
        else
        {
            formatsForMetrics = rawFormats;
        }

        int? maxFps = null;
        int maxHeight = 0;
        int? maxWidth = null;

        foreach (var formatRaw in formatsForMetrics.Where(f => f.HasVideo))
        {
            if (formatRaw.Height.HasValue && formatRaw.Height.Value > maxHeight)
            {
                maxHeight = formatRaw.Height.Value;
                maxWidth = formatRaw.Width;
            }
            else if (formatRaw.Height.HasValue && formatRaw.Height.Value == maxHeight &&
                     formatRaw.Width.HasValue && (!maxWidth.HasValue || formatRaw.Width.Value > maxWidth.Value))
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
        }

        var maxResName = maxHeight > 0
            ? (platform == PlatformType.TikTok ? GetTikTokResolutionLabel(maxHeight, maxWidth) : GetResolutionLabel(maxHeight, maxWidth))
            : "Automática";

        var urlForCanonical = !string.IsNullOrWhiteSpace(webpageUrl) ? webpageUrl : originalUrl;
        var canonicalKey = _platformService.GetCanonicalKey(urlForCanonical, platform, id);

        return new VideoInfo
        {
            Id = id,
            Title = title,
            Channel = channel,
            Description = rawDescription,
            DurationSeconds = duration,
            ThumbnailUrl = thumbnail,
            UploadDate = uploadDate,
            MaxResolution = maxResName,
            MaxFps = maxFps,
            OriginalUrl = originalUrl,
            Platform = platform,
            CanonicalKey = canonicalKey,
            WatermarkStatus = videoWatermarkStatus,
            Formats = rawFormats
        };
    }

    public List<FormatOption> BuildFormatOptions(VideoInfo info)
    {
        if (info.Platform == PlatformType.TikTok)
        {
            return BuildTikTokFormatOptions(info);
        }

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
            var rawDesc = root.GetStringNullable("description");
            if (!string.IsNullOrWhiteSpace(rawDesc))
            {
                var firstLine = rawDesc.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));

                if (!string.IsNullOrWhiteSpace(firstLine))
                {
                    title = firstLine.Length > 80 ? firstLine.Substring(0, 77) + "..." : firstLine;
                }
            }

            if (title.StartsWith("Post by", StringComparison.OrdinalIgnoreCase) ||
                title.StartsWith("Video by", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(title, "Publicação sem título", StringComparison.OrdinalIgnoreCase))
            {
                if (platform == PlatformType.Instagram)
                {
                    title = string.Equals(channel, "Instagram", StringComparison.OrdinalIgnoreCase)
                        ? "Carrossel do Instagram"
                        : $"Carrossel de @{channel.TrimStart('@')}";
                }
                else
                {
                    title = $"Publicação de @{channel.TrimStart('@')}";
                }
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
                {
                    var nullMediaId = !string.IsNullOrEmpty(id) ? $"{id}_{index}" : $"item_{index}";
                    var nullMediaUrl = isCarousel && platform == PlatformType.Instagram
                        ? (!string.IsNullOrWhiteSpace(originalUrl) ? originalUrl : (!string.IsNullOrWhiteSpace(id) ? $"https://www.instagram.com/p/{id}/" : string.Empty))
                        : (!string.IsNullOrWhiteSpace(originalUrl) ? originalUrl : string.Empty);

                    var nullItem = new PlaylistItemInfo
                    {
                        Id = nullMediaId,
                        Title = $"Mídia #{index} (Indisponível)",
                        VideoUrl = nullMediaUrl,
                        Channel = channel,
                        DurationSeconds = null,
                        ThumbnailUrl = thumbnail,
                        PlaylistIndex = index,
                        Platform = platform,
                        CanonicalKey = _platformService.GetCanonicalKey(
                            nullMediaUrl,
                            platform,
                            nullMediaId,
                            index),
                        IsAvailable = false,
                        IsSelected = false,
                        AvailabilityNotice = "Mídia indisponível ou não suportada"
                    };

                    items.Add(nullItem);
                    index++;
                    continue;
                }

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

                bool isPhoto = false;
                bool isExplicitUnavailable = false;
                string explicitUnavailableNotice = "Vídeo indisponível ou privado";

                // Sinais de erro, exclusão ou autenticação requerida
                if (entryTitle.Contains("[Private video]", StringComparison.OrdinalIgnoreCase) ||
                    entryTitle.Contains("[Deleted video]", StringComparison.OrdinalIgnoreCase) ||
                    entryTitle.Contains("private", StringComparison.OrdinalIgnoreCase) ||
                    entryTitle.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                    entryTitle.Contains("auth", StringComparison.OrdinalIgnoreCase))
                {
                    isExplicitUnavailable = true;
                    explicitUnavailableNotice = "Vídeo indisponível ou privado";
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

                if (string.IsNullOrEmpty(entryThumb) && !string.IsNullOrEmpty(thumbnail))
                {
                    entryThumb = thumbnail;
                }

                if (isCarousel)
                {
                    var isImageExt = entryExt == "jpg" || entryExt == "jpeg" || entryExt == "png" || entryExt == "webp" || entryExt == "heic" || entryExt == "bmp";

                    bool hasVideoFormat = false;
                    if (entry.TryGetProperty("formats", out var formatsProp) && formatsProp.ValueKind == JsonValueKind.Array && formatsProp.GetArrayLength() > 0)
                    {
                        foreach (var fmt in formatsProp.EnumerateArray())
                        {
                            if (fmt.ValueKind != JsonValueKind.Object)
                                continue;

                            var fmtVcodec = fmt.GetStringSafe("vcodec").ToLowerInvariant();
                            var fmtExt = fmt.GetStringSafe("ext").ToLowerInvariant();
                            var fmtHeight = fmt.GetInt32Nullable("height");
                            var fmtWidth = fmt.GetInt32Nullable("width");
                            var isFmtImg = fmtExt == "jpg" || fmtExt == "jpeg" || fmtExt == "png" || fmtExt == "webp" || fmtExt == "heic" || fmtExt == "bmp";

                            if (!string.IsNullOrEmpty(fmtVcodec) && fmtVcodec != "none")
                            {
                                hasVideoFormat = true;
                                break;
                            }
                            if (!isFmtImg && ((fmtHeight.HasValue && fmtHeight.Value > 0) || (fmtWidth.HasValue && fmtWidth.Value > 0)))
                            {
                                hasVideoFormat = true;
                                break;
                            }
                        }
                    }

                    bool hasExplicitVideoStreams = hasVideoFormat || (!string.IsNullOrEmpty(vcodec) && vcodec != "none");

                    if (!hasExplicitVideoStreams && !isExplicitUnavailable)
                    {
                        // Evidências de que o item é imagem estática
                        bool hasPhotoEvidence = isImageExt ||
                            formatNote.Contains("photo") ||
                            formatNote.Contains("image") ||
                            (vcodec == "none" && acodec == "none") ||
                            entryThumb.Contains("regular_photo", StringComparison.OrdinalIgnoreCase) ||
                            thumbnail.Contains("regular_photo", StringComparison.OrdinalIgnoreCase) ||
                            (platform == PlatformType.Instagram && (!entry.TryGetProperty("formats", out var f) || f.GetArrayLength() == 0) && !isExplicitUnavailable);

                        if (hasPhotoEvidence)
                        {
                            isPhoto = true;
                        }
                        else
                        {
                            isExplicitUnavailable = true;
                            explicitUnavailableNotice = "Mídia indisponível ou sem formatos de vídeo compatíveis";
                        }
                    }
                }

                var entryUrl = entry.GetStringSafe("url");
                if (isCarousel && platform == PlatformType.Instagram)
                {
                    entryUrl = !string.IsNullOrWhiteSpace(originalUrl)
                        ? originalUrl
                        : (!string.IsNullOrWhiteSpace(id) ? $"https://www.instagram.com/p/{id}/" : entryUrl);
                }
                else if (string.IsNullOrWhiteSpace(entryUrl) || !entryUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
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

                bool isUnavailable = !isPhoto && (isExplicitUnavailable || string.IsNullOrEmpty(entryId));

                string notice = string.Empty;
                bool isItemAvailable = !isUnavailable;
                bool isItemSelected = !isUnavailable;

                if (isPhoto)
                {
                    isItemAvailable = false;
                    isItemSelected = false;
                    notice = "Foto / imagem estática (download de imagens em carrossel planejado para versão futura)";

                    if (string.IsNullOrWhiteSpace(entryTitle) ||
                        entryTitle.StartsWith("Video by", StringComparison.OrdinalIgnoreCase) ||
                        entryTitle.StartsWith("Post by", StringComparison.OrdinalIgnoreCase) ||
                        entryTitle.StartsWith("Vídeo #", StringComparison.OrdinalIgnoreCase))
                    {
                        entryTitle = $"Foto #{index}";
                    }
                }
                else if (isUnavailable)
                {
                    notice = explicitUnavailableNotice;
                    if (string.IsNullOrWhiteSpace(entryTitle) ||
                        entryTitle.StartsWith("Video by", StringComparison.OrdinalIgnoreCase) ||
                        entryTitle.StartsWith("Post by", StringComparison.OrdinalIgnoreCase) ||
                        entryTitle.StartsWith("Vídeo #", StringComparison.OrdinalIgnoreCase))
                    {
                        entryTitle = $"Mídia #{index} (Indisponível)";
                    }
                }

                var finalId = !string.IsNullOrEmpty(entryId) ? entryId : (!string.IsNullOrEmpty(id) ? $"{id}_{index}" : $"item_{index}");

                var item = new PlaylistItemInfo
                {
                    Id = finalId,
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
                        finalId,
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

    private List<FormatOption> BuildTikTokFormatOptions(VideoInfo info)
    {
        var options = new List<FormatOption>();

        foreach (var fmt in info.Formats)
        {
            if (fmt.HasVideo && fmt.WatermarkStatus == WatermarkStatus.NotApplicable)
            {
                fmt.WatermarkStatus = ClassifyTikTokFormatWatermark(fmt);
            }
        }

        var cleanFormats = info.Formats
            .Where(f => f.HasVideo &&
                        !f.IsUnplayable &&
                        !string.IsNullOrWhiteSpace(f.FormatId) &&
                        f.WatermarkStatus == WatermarkStatus.NoWatermark &&
                        ClassifyTikTokFormatWatermark(f) == WatermarkStatus.NoWatermark)
            .ToList();

        var unknownFormats = info.Formats
            .Where(f => f.HasVideo && !f.IsUnplayable && f.WatermarkStatus == WatermarkStatus.Unknown)
            .ToList();

        var watermarkedFormats = info.Formats
            .Where(f => f.HasVideo && !f.IsUnplayable && f.WatermarkStatus == WatermarkStatus.Watermarked)
            .ToList();

        if (cleanFormats.Count > 0)
        {
            info.WatermarkStatus = WatermarkStatus.NoWatermark;

            var uniqueHeights = cleanFormats
                .Where(f => f.Height.HasValue && f.Height.Value > 0)
                .Select(f => f.Height!.Value)
                .Distinct()
                .OrderByDescending(h => h)
                .ToList();

            var bestHeight = uniqueHeights.FirstOrDefault();
            var bestWidth = cleanFormats
                .Where(f => f.Height == bestHeight && f.Width.HasValue && f.Width.Value > 0)
                .Select(f => f.Width)
                .FirstOrDefault();

            var bestFps = cleanFormats
                .Where(f => f.Height == bestHeight && f.Fps.HasValue && f.Fps.Value > 0)
                .Select(f => (int)Math.Round(f.Fps!.Value))
                .DefaultIfEmpty(0)
                .Max();

            var bestLabel = "Melhor qualidade disponível (Sem marca-d'água)";
            if (bestHeight > 0)
            {
                var resName = GetTikTokResolutionLabel(bestHeight, bestWidth);
                bestLabel = bestFps > 30
                    ? $"Melhor qualidade disponível ({resName} • {bestFps} FPS • Sem marca-d'água)"
                    : $"Melhor qualidade disponível ({resName} • Sem marca-d'água)";
            }

            options.Add(new FormatOption
            {
                Label = bestLabel,
                Height = bestHeight > 0 ? bestHeight : null,
                Fps = bestFps > 0 ? bestFps : null,
                FormatSelector = BuildVerifiedCleanTikTokSelector(cleanFormats),
                IsBestQuality = true,
                IsAudioOnly = false,
                WatermarkStatus = WatermarkStatus.NoWatermark,
                RequiresWatermarkConsent = false,
                Description = "Prioriza a fonte original de reprodução sem marca-d'água na maior qualidade disponível com áudio e vídeo."
            });

            foreach (var height in uniqueHeights)
            {
                var formatsForHeight = cleanFormats.Where(f => f.Height == height).ToList();
                var width = formatsForHeight
                    .Where(f => f.Width.HasValue && f.Width.Value > 0)
                    .Select(f => f.Width)
                    .FirstOrDefault();

                var fps = formatsForHeight
                    .Where(f => f.Fps.HasValue && f.Fps.Value > 0)
                    .Select(f => (int)Math.Round(f.Fps!.Value))
                    .DefaultIfEmpty(0)
                    .Max();

                var resName = GetTikTokResolutionLabel(height, width);
                var label = fps > 30
                    ? $"{resName} • {fps} FPS • Sem marca-d'água"
                    : $"{resName} • Sem marca-d'água";

                options.Add(new FormatOption
                {
                    Label = label,
                    Height = height,
                    Fps = fps > 0 ? fps : null,
                    FormatSelector = BuildCleanTikTokResolutionSelector(cleanFormats, height),
                    IsBestQuality = false,
                    IsAudioOnly = false,
                    WatermarkStatus = WatermarkStatus.NoWatermark,
                    RequiresWatermarkConsent = false,
                    Description = $"Fonte de reprodução até {resName} sem marca-d'água da plataforma."
                });
            }
        }
        else if (unknownFormats.Count > 0)
        {
            info.WatermarkStatus = WatermarkStatus.Unknown;

            var usableUnknownFormats = unknownFormats
                .Where(f => !string.IsNullOrWhiteSpace(f.FormatId) &&
                            f.WatermarkStatus == WatermarkStatus.Unknown &&
                            ClassifyTikTokFormatWatermark(f) == WatermarkStatus.Unknown &&
                            !f.IsUnplayable &&
                            f.HasVideo)
                .ToList();

            if (usableUnknownFormats.Count > 0)
            {
                var uniqueHeights = usableUnknownFormats
                    .Where(f => f.Height.HasValue && f.Height.Value > 0)
                    .Select(f => f.Height!.Value)
                    .Distinct()
                    .OrderByDescending(h => h)
                    .ToList();

                var bestHeight = uniqueHeights.FirstOrDefault();
                var bestWidth = usableUnknownFormats
                    .Where(f => f.Height == bestHeight && f.Width.HasValue && f.Width.Value > 0)
                    .Select(f => f.Width)
                    .FirstOrDefault();

                var bestLabel = "Qualidade disponível (Marca-d'água não verificada)";
                if (bestHeight > 0)
                {
                    var resName = GetTikTokResolutionLabel(bestHeight, bestWidth);
                    bestLabel = $"Qualidade até {resName} (Marca-d'água não verificada)";
                }

                options.Add(new FormatOption
                {
                    Label = bestLabel,
                    Height = bestHeight > 0 ? bestHeight : null,
                    FormatSelector = BuildUnknownTikTokSelector(usableUnknownFormats),
                    IsBestQuality = false, // NUNCA seleciona automaticamente formato não verificado como fonte limpa
                    IsAudioOnly = false,
                    WatermarkStatus = WatermarkStatus.Unknown,
                    RequiresWatermarkConsent = false,
                    Description = "O extrator não forneceu informações suficientes para confirmar o estado da marca-d'água."
                });

                foreach (var height in uniqueHeights)
                {
                    var width = usableUnknownFormats
                        .Where(f => f.Height == height && f.Width.HasValue && f.Width.Value > 0)
                        .Select(f => f.Width)
                        .FirstOrDefault();

                    var resName = GetTikTokResolutionLabel(height, width);
                    options.Add(new FormatOption
                    {
                        Label = $"{resName} • Marca-d'água não verificada",
                        Height = height,
                        FormatSelector = BuildUnknownTikTokSelector(usableUnknownFormats, height),
                        IsBestQuality = false,
                        IsAudioOnly = false,
                        WatermarkStatus = WatermarkStatus.Unknown,
                        RequiresWatermarkConsent = false,
                        Description = $"Vídeo até {resName} (estado da marca-d'água desconhecido)."
                    });
                }
            }
        }
        else if (watermarkedFormats.Count > 0)
        {
            info.WatermarkStatus = WatermarkStatus.Watermarked;

            var bestWmHeight = watermarkedFormats
                .Where(f => f.Height.HasValue && f.Height.Value > 0)
                .Select(f => f.Height!.Value)
                .DefaultIfEmpty(0)
                .Max();

            options.Add(new FormatOption
            {
                Label = "⚠️ Vídeo com marca-d'água (única fonte disponível)",
                Height = bestWmHeight > 0 ? bestWmHeight : null,
                FormatSelector = "download/download_addr/best[vcodec!=bytevc2][vcodec!=none]",
                IsBestQuality = false, // Nunca selecionada automaticamente sem ciência explícita
                IsAudioOnly = false,
                WatermarkStatus = WatermarkStatus.Watermarked,
                RequiresWatermarkConsent = true,
                Description = "Nenhuma fonte sem marca-d'água foi disponibilizada pelo TikTok. Requer confirmação explícita."
            });
        }

        // Opção: Somente Áudio
        options.Add(new FormatOption
        {
            Label = "🎵 Somente áudio",
            FormatSelector = "bestaudio/best",
            IsBestQuality = false,
            IsAudioOnly = true,
            WatermarkStatus = WatermarkStatus.NotApplicable,
            RequiresWatermarkConsent = false,
            Description = "Extrai apenas a melhor trilha de áudio disponível."
        });

        return options;
    }

    public static string BuildVerifiedCleanTikTokSelector(IReadOnlyList<VideoFormatRaw> cleanFormats, int? maxHeight = null)
    {
        if (cleanFormats == null)
        {
            throw new ArgumentNullException(nameof(cleanFormats));
        }

        var candidates = cleanFormats
            .Where(f => !string.IsNullOrWhiteSpace(f.FormatId) &&
                        f.WatermarkStatus == WatermarkStatus.NoWatermark &&
                        ClassifyTikTokFormatWatermark(f) == WatermarkStatus.NoWatermark &&
                        !f.IsUnplayable &&
                        f.HasVideo &&
                        (!maxHeight.HasValue || (f.Height.HasValue && f.Height.Value > 0 && f.Height.Value <= maxHeight.Value)))
            .OrderByDescending(f => f.Height ?? 0)
            .ThenByDescending(f => f.Width ?? 0)
            .ThenByDescending(f => f.Fps ?? 0)
            .ThenByDescending(f => f.HasAudio)
            .ThenByDescending(f => IsPreferredTikTokCodec(f.VCodec))
            .ThenByDescending(f => f.Tbr ?? 0)
            .ThenByDescending(f => f.FileSize ?? f.FileSizeApprox ?? 0)
            .Select(f => f.HasAudio ? f.FormatId.Trim() : $"{f.FormatId.Trim()}+bestaudio")
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("Nenhum formato comprovadamente sem marca-d'água está disponível para construir o seletor.");
        }

        return string.Join("/", candidates);
    }

    public static string BuildCleanTikTokResolutionSelector(IReadOnlyList<VideoFormatRaw> cleanFormats, int maxHeight) =>
        BuildVerifiedCleanTikTokSelector(cleanFormats, maxHeight);

    public static string BuildUnknownTikTokSelector(IReadOnlyList<VideoFormatRaw> unknownFormats, int? maxHeight = null)
    {
        if (unknownFormats == null)
        {
            throw new ArgumentNullException(nameof(unknownFormats));
        }

        var candidates = unknownFormats
            .Where(f => !string.IsNullOrWhiteSpace(f.FormatId) &&
                        f.WatermarkStatus == WatermarkStatus.Unknown &&
                        ClassifyTikTokFormatWatermark(f) == WatermarkStatus.Unknown &&
                        !f.IsUnplayable &&
                        f.HasVideo &&
                        (!maxHeight.HasValue || (f.Height.HasValue && f.Height.Value > 0 && f.Height.Value <= maxHeight.Value)))
            .OrderByDescending(f => f.Height ?? 0)
            .ThenByDescending(f => f.Width ?? 0)
            .ThenByDescending(f => f.Fps ?? 0)
            .ThenByDescending(f => f.HasAudio)
            .ThenByDescending(f => f.Tbr ?? 0)
            .Select(f => f.HasAudio ? f.FormatId.Trim() : $"{f.FormatId.Trim()}+bestaudio")
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("Nenhum formato de estado não verificado possui format_id utilizável para construir o seletor.");
        }

        return string.Join("/", candidates);
    }

    private static int IsPreferredTikTokCodec(string? vcodec)
    {
        if (string.IsNullOrWhiteSpace(vcodec)) return 0;
        if (vcodec.Contains("h265", StringComparison.OrdinalIgnoreCase) ||
            vcodec.Contains("hevc", StringComparison.OrdinalIgnoreCase) ||
            vcodec.Contains("bytevc1", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }
        if (vcodec.Contains("h264", StringComparison.OrdinalIgnoreCase) ||
            vcodec.Contains("avc", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }
        return 0;
    }

    public static string GetTikTokResolutionLabel(int height, int? width = null)
    {
        if (width.HasValue && width.Value > 0 && width.Value < height)
        {
            var shortEdge = width.Value;
            var baseRes = shortEdge switch
            {
                >= 2160 => "4K (2160p)",
                >= 1440 => "1440p (2K)",
                >= 1080 => "1080p (Full HD)",
                >= 720 => "720p (HD)",
                576 => "576p",
                540 => "540p",
                >= 480 => "480p",
                >= 360 => "360p",
                _ => $"{shortEdge}p"
            };
            return $"{baseRes} Vertical ({width.Value}x{height})";
        }

        return GetResolutionLabel(height, width);
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
