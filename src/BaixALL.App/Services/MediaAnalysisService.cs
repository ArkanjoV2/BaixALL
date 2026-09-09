using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Helpers;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class MediaAnalysisService : IMediaAnalysisService, IYoutubeService
{
    private readonly IYtDlpService _ytDlpService;
    private readonly IFormatSelectionService _formatSelectionService;
    private readonly IPlatformService _platformService;
    private readonly ILoggerService? _logger;

    public MediaAnalysisService(
        IYtDlpService ytDlpService,
        IFormatSelectionService formatSelectionService,
        IPlatformService? platformService = null,
        ILoggerService? logger = null)
    {
        _ytDlpService = ytDlpService;
        _formatSelectionService = formatSelectionService;
        _platformService = platformService ?? new PlatformService();
        _logger = logger;
    }

    public async Task<VideoInfo> AnalyzeVideoAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Por favor, informe a URL da mídia.", nameof(url));
        }

        var platform = _platformService.DetectPlatform(url);
        if (!_platformService.IsSupportedUrl(url))
        {
            var msg = platform switch
            {
                PlatformType.YouTube => "A URL informada não pertence ao YouTube ou possui formato inválido.",
                PlatformType.Instagram => "A URL informada não pertence ao Instagram ou possui formato inválido.",
                PlatformType.Twitter => "A URL informada não pertence ao X/Twitter ou possui formato inválido.",
                _ => "A URL informada não pertence a uma plataforma suportada (YouTube, Instagram ou X/Twitter)."
            };
            throw new ArgumentException(msg, nameof(url));
        }

        var normalizedUrl = _platformService.NormalizeUrl(url, platform);
        _logger?.Info($"Iniciando análise de vídeo para [{platform}]: {normalizedUrl}");

        using var jsonDoc = await _ytDlpService.GetMetadataJsonAsync(normalizedUrl, ct).ConfigureAwait(false);
        var videoInfo = _formatSelectionService.ParseVideoInfo(jsonDoc, normalizedUrl);
        videoInfo.Platform = platform;
        videoInfo.CanonicalKey = _platformService.GetCanonicalKey(normalizedUrl, platform, videoInfo.Id);

        // Validação de vídeo presente
        if (!videoInfo.Formats.Any(f => f.HasVideo))
        {
            if (platform == PlatformType.Twitter)
            {
                throw new InvalidOperationException("Esta publicação do X/Twitter não contém nenhum vídeo.");
            }
            if (platform == PlatformType.Instagram)
            {
                throw new InvalidOperationException("Não foi possível encontrar fluxos de vídeo válidos nesta publicação do Instagram.");
            }
        }

        _logger?.Info($"Vídeo analisado com sucesso [{platform}]: '{videoInfo.Title}' ({videoInfo.MaxResolution} - {videoInfo.FormattedDuration})");
        return videoInfo;
    }

    public async Task<PlaylistInfo> AnalyzeCollectionAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Por favor, informe a URL da coleção.", nameof(url));
        }

        var platform = _platformService.DetectPlatform(url);
        if (!_platformService.IsSupportedUrl(url))
        {
            throw new ArgumentException("A URL informada não pertence a uma plataforma suportada.", nameof(url));
        }

        var normalizedUrl = _platformService.NormalizeUrl(url, platform);
        _logger?.Info($"Iniciando análise de coleção para [{platform}]: {normalizedUrl}");

        JsonDocument jsonDoc;
        if (platform == PlatformType.YouTube)
        {
            jsonDoc = await _ytDlpService.GetPlaylistMetadataJsonAsync(normalizedUrl, ct).ConfigureAwait(false);
        }
        else
        {
            jsonDoc = await _ytDlpService.GetMetadataJsonAsync(normalizedUrl, ct).ConfigureAwait(false);
        }

        using (jsonDoc)
        {
            var playlistInfo = _formatSelectionService.ParsePlaylistInfo(jsonDoc, normalizedUrl);
            playlistInfo.Platform = platform;
            playlistInfo.IsCarousel = (platform != PlatformType.YouTube);

            _logger?.Info($"Coleção analisada com sucesso [{platform}]: '{playlistInfo.Title}' ({playlistInfo.TotalVideosCount} mídias identificadas)");
            return playlistInfo;
        }
    }

    public async Task<PlaylistInfo> AnalyzePlaylistAsync(string url, CancellationToken ct = default)
    {
        return await AnalyzeCollectionAsync(url, ct).ConfigureAwait(false);
    }

    public async Task<MediaAnalysisResult> AnalyzeAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Por favor, informe uma URL.", nameof(url));
        }

        var platform = _platformService.DetectPlatform(url);
        if (!_platformService.IsSupportedUrl(url))
        {
            var msg = platform switch
            {
                PlatformType.YouTube => "A URL informada não pertence ao YouTube ou possui formato inválido.",
                PlatformType.Instagram => "A URL informada não pertence ao Instagram ou possui formato inválido.",
                PlatformType.Twitter => "A URL informada não pertence ao X/Twitter ou possui formato inválido.",
                _ => "A URL informada não pertence a uma plataforma suportada (YouTube, Instagram ou X/Twitter)."
            };
            throw new ArgumentException(msg, nameof(url));
        }

        var normalizedUrl = _platformService.NormalizeUrl(url, platform);

        if (platform == PlatformType.YouTube)
        {
            if (UrlValidator.IsPurePlaylistUrl(normalizedUrl))
            {
                var collection = await AnalyzeCollectionAsync(normalizedUrl, ct).ConfigureAwait(false);
                return new MediaAnalysisResult
                {
                    Platform = PlatformType.YouTube,
                    IsCollection = true,
                    Collection = collection
                };
            }

            var isHybrid = UrlValidator.IsHybridUrl(normalizedUrl);
            var video = await AnalyzeVideoAsync(normalizedUrl, ct).ConfigureAwait(false);
            return new MediaAnalysisResult
            {
                Platform = PlatformType.YouTube,
                IsCollection = false,
                IsHybrid = isHybrid,
                Video = video
            };
        }

        // Para Instagram ou Twitter: obtemos o JSON de metadados
        using var jsonDoc = await _ytDlpService.GetMetadataJsonAsync(normalizedUrl, ct).ConfigureAwait(false);
        var root = jsonDoc.RootElement;

        bool isPlaylistType = root.TryGetProperty("_type", out var typeProp) &&
                              typeProp.ValueKind == JsonValueKind.String &&
                              typeProp.GetString() == "playlist";

        if (isPlaylistType)
        {
            var collection = _formatSelectionService.ParsePlaylistInfo(jsonDoc, normalizedUrl);
            collection.Platform = platform;
            collection.IsCarousel = true;
            return new MediaAnalysisResult
            {
                Platform = platform,
                IsCollection = true,
                Collection = collection
            };
        }
        else
        {
            var video = _formatSelectionService.ParseVideoInfo(jsonDoc, normalizedUrl);
            video.Platform = platform;
            video.CanonicalKey = _platformService.GetCanonicalKey(normalizedUrl, platform, video.Id);

            if (!video.Formats.Any(f => f.HasVideo))
            {
                if (platform == PlatformType.Twitter)
                {
                    throw new InvalidOperationException("Esta publicação do X/Twitter não contém nenhum vídeo.");
                }
                if (platform == PlatformType.Instagram)
                {
                    throw new InvalidOperationException("Não foi possível encontrar fluxos de vídeo válidos nesta publicação do Instagram.");
                }
            }

            return new MediaAnalysisResult
            {
                Platform = platform,
                IsCollection = false,
                Video = video
            };
        }
    }
}
