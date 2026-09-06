using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Infrastructure;

namespace BaixALL.App.Services;

public class UpdateService : IUpdateService
{
    private readonly IDependencyManager _dependencyManager;
    private readonly IDownloadService _downloadService;
    private readonly ILoggerService? _logger;
    private readonly HttpClient _httpClient;

    public UpdateService(
        IDependencyManager dependencyManager,
        IDownloadService downloadService,
        ILoggerService? logger = null,
        HttpClient? httpClient = null)
    {
        _dependencyManager = dependencyManager;
        _downloadService = downloadService;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) BaixALL/1.0");
    }

    public async Task<UpdateCheckResult> CheckYtDlpUpdateAsync(CancellationToken ct = default)
    {
        var dep = _dependencyManager.GetDependencies().FirstOrDefault(d => d.Name.Equals("yt-dlp", StringComparison.OrdinalIgnoreCase));
        var currentVer = dep?.Version ?? "Desconhecido";

        try
        {
            var response = await _httpClient.GetStringAsync(AppConstants.YtDlpApiLatestRelease, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(response);
            var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var htmlUrl = doc.RootElement.GetProperty("html_url").GetString() ?? "";

            var cleanCurrent = currentVer.Trim();
            var cleanTag = tag.Trim();

            var hasUpdate = !string.IsNullOrEmpty(cleanTag) &&
                            !cleanCurrent.Equals(cleanTag, StringComparison.OrdinalIgnoreCase) &&
                            !cleanCurrent.Contains("Não instalado");

            return new UpdateCheckResult("yt-dlp", cleanCurrent, cleanTag, hasUpdate, htmlUrl);
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Falha ao verificar atualização do yt-dlp: {ex.Message}");
            return new UpdateCheckResult("yt-dlp", currentVer, "Falha ao verificar", false, "");
        }
    }

    public async Task<UpdateCheckResult> CheckDenoUpdateAsync(CancellationToken ct = default)
    {
        var dep = _dependencyManager.GetDependencies().FirstOrDefault(d => d.Name.Equals("Deno", StringComparison.OrdinalIgnoreCase));
        var currentVer = dep?.Version ?? "Desconhecido";

        try
        {
            var response = await _httpClient.GetStringAsync(AppConstants.DenoApiLatestRelease, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(response);
            var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var htmlUrl = doc.RootElement.GetProperty("html_url").GetString() ?? "";

            var cleanCurrent = currentVer.Trim().TrimStart('v');
            var cleanTag = tag.Trim().TrimStart('v');

            var hasUpdate = !string.IsNullOrEmpty(cleanTag) &&
                            !cleanCurrent.Equals(cleanTag, StringComparison.OrdinalIgnoreCase) &&
                            !cleanCurrent.Contains("Não instalado");

            return new UpdateCheckResult("Deno", currentVer, tag, hasUpdate, htmlUrl);
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Falha ao verificar atualização do Deno: {ex.Message}");
            return new UpdateCheckResult("Deno", currentVer, "Falha ao verificar", false, "");
        }
    }

    public async Task<bool> UpdateToolAsync(string toolName, CancellationToken ct = default)
    {
        if (_downloadService.HasActiveDownloads)
        {
            _logger?.Warning("Tentativa de atualizar ferramenta bloqueada: existem downloads ativos.");
            throw new InvalidOperationException("Não é possível atualizar ferramentas enquanto houver downloads em andamento.");
        }

        _logger?.Info($"Atualizando ferramenta '{toolName}'...");
        return await _dependencyManager.UpdateDependencyAsync(toolName, ct: ct).ConfigureAwait(false);
    }
}
