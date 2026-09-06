using System;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Helpers;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class YoutubeService : IYoutubeService
{
    private readonly IYtDlpService _ytDlpService;
    private readonly IFormatSelectionService _formatSelectionService;
    private readonly ILoggerService? _logger;

    public YoutubeService(
        IYtDlpService ytDlpService,
        IFormatSelectionService formatSelectionService,
        ILoggerService? logger = null)
    {
        _ytDlpService = ytDlpService;
        _formatSelectionService = formatSelectionService;
        _logger = logger;
    }

    public async Task<VideoInfo> AnalyzeVideoAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Por favor, informe a URL do vídeo.", nameof(url));
        }

        if (!UrlValidator.IsValidYouTubeUrl(url))
        {
            throw new ArgumentException("A URL informada não pertence ao YouTube ou possui formato inválido.", nameof(url));
        }

        var normalizedUrl = UrlValidator.NormalizeYouTubeUrl(url);
        _logger?.Info($"Iniciando análise de vídeo para: {normalizedUrl}");

        using var jsonDoc = await _ytDlpService.GetMetadataJsonAsync(normalizedUrl, ct).ConfigureAwait(false);
        var videoInfo = _formatSelectionService.ParseVideoInfo(jsonDoc, normalizedUrl);

        _logger?.Info($"Vídeo analisado com sucesso: '{videoInfo.Title}' ({videoInfo.MaxResolution} - {videoInfo.FormattedDuration})");
        return videoInfo;
    }
}
