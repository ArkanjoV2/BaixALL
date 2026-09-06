using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Helpers;
using BaixALL.App.Infrastructure;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class DependencyManager : IDependencyManager
{
    private readonly ILoggerService? _logger;
    private readonly HttpClient _httpClient;
    private readonly List<DependencyItem> _dependencies;

    public DependencyManager(ILoggerService? logger = null, HttpClient? httpClient = null)
    {
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) BaixALL/1.0");

        _dependencies = new List<DependencyItem>
        {
            new()
            {
                Name = "yt-dlp",
                Description = "Motor principal de extração e download de vídeos",
                LocalPath = AppConstants.YtDlpExe
            },
            new()
            {
                Name = "FFmpeg",
                Description = "Mesclagem de fluxos de vídeo/áudio e conversão de formatos",
                LocalPath = AppConstants.FFmpegExe
            },
            new()
            {
                Name = "ffprobe",
                Description = "Analisador de codecs e streams multimídia",
                LocalPath = AppConstants.FFprobeExe
            },
            new()
            {
                Name = "Deno",
                Description = "Runtime JavaScript para desafios anti-bot e assinaturas do YouTube",
                LocalPath = AppConstants.DenoExe
            }
        };
    }

    public IReadOnlyList<DependencyItem> GetDependencies() => _dependencies.AsReadOnly();

    public string GetYtDlpPath() => AppConstants.YtDlpExe;
    public string GetFFmpegPath() => AppConstants.FFmpegExe;
    public string GetFFprobePath() => AppConstants.FFprobeExe;
    public string GetDenoPath() => AppConstants.DenoExe;

    public bool AreAllDependenciesInstalled()
    {
        return File.Exists(AppConstants.YtDlpExe) &&
               File.Exists(AppConstants.FFmpegExe) &&
               File.Exists(AppConstants.FFprobeExe) &&
               File.Exists(AppConstants.DenoExe);
    }

    public async Task<bool> CheckDependenciesAsync()
    {
        _logger?.Info("Verificando status das dependências locais...");

        foreach (var dep in _dependencies)
        {
            if (File.Exists(dep.LocalPath))
            {
                dep.IsInstalled = true;
                dep.StatusText = "Instalado";
                try
                {
                    dep.Version = await GetToolVersionAsync(dep.Name, dep.LocalPath).ConfigureAwait(false);
                }
                catch
                {
                    dep.Version = "Presente (versão não identificada)";
                }
            }
            else
            {
                dep.IsInstalled = false;
                dep.Version = "Não instalado";
                dep.StatusText = "Pendente";
            }
        }

        return AreAllDependenciesInstalled();
    }

    public async Task<bool> EnsureAllDependenciesAsync(
        IProgress<(string ToolName, double Progress, string Status)>? progress = null,
        CancellationToken ct = default)
    {
        await CheckDependenciesAsync().ConfigureAwait(false);

        // 1. yt-dlp
        if (!File.Exists(AppConstants.YtDlpExe))
        {
            var success = await DownloadYtDlpAsync(progress, ct).ConfigureAwait(false);
            if (!success) return false;
        }

        // 2. FFmpeg e ffprobe
        if (!File.Exists(AppConstants.FFmpegExe) || !File.Exists(AppConstants.FFprobeExe))
        {
            var success = await DownloadFFmpegAsync(progress, ct).ConfigureAwait(false);
            if (!success) return false;
        }

        // 3. Deno
        if (!File.Exists(AppConstants.DenoExe))
        {
            var success = await DownloadDenoAsync(progress, ct).ConfigureAwait(false);
            if (!success) return false;
        }

        await CheckDependenciesAsync().ConfigureAwait(false);
        return AreAllDependenciesInstalled();
    }

    public async Task<bool> UpdateDependencyAsync(
        string toolName,
        IProgress<(string ToolName, double Progress, string Status)>? progress = null,
        CancellationToken ct = default)
    {
        _logger?.Info($"Iniciando atualização de dependência: {toolName}");

        var normalized = toolName.ToLowerInvariant();
        bool success;

        if (normalized.Contains("yt-dlp"))
        {
            success = await DownloadYtDlpAsync(progress, ct).ConfigureAwait(false);
        }
        else if (normalized.Contains("ffmpeg") || normalized.Contains("ffprobe"))
        {
            success = await DownloadFFmpegAsync(progress, ct).ConfigureAwait(false);
        }
        else if (normalized.Contains("deno"))
        {
            success = await DownloadDenoAsync(progress, ct).ConfigureAwait(false);
        }
        else
        {
            _logger?.Warning($"Dependência desconhecida para atualização: {toolName}");
            return false;
        }

        await CheckDependenciesAsync().ConfigureAwait(false);
        return success;
    }

    private async Task<bool> DownloadYtDlpAsync(
        IProgress<(string ToolName, double Progress, string Status)>? progress,
        CancellationToken ct)
    {
        const string name = "yt-dlp";
        _logger?.Info("Baixando yt-dlp oficial...");
        progress?.Report((name, 0, "Iniciando download"));

        FileHelper.EnsureDirectoryExists(AppConstants.YtDlpDir);
        var tempFile = Path.Combine(AppConstants.YtDlpDir, "yt-dlp.exe.tmp");

        try
        {
            var downloaded = await DownloadFileWithProgressAsync(
                AppConstants.YtDlpDownloadUrl,
                tempFile,
                p => progress?.Report((name, p, $"Baixando yt-dlp ({p:0}%)")),
                ct).ConfigureAwait(false);

            if (!downloaded || !File.Exists(tempFile) || new FileInfo(tempFile).Length < 1000000)
            {
                throw new InvalidOperationException("Download do yt-dlp incompleto ou corrompido.");
            }

            progress?.Report((name, 90, "Validando executável..."));

            // Substituição atômica segura
            AtomicReplaceFile(tempFile, AppConstants.YtDlpExe);

            var version = await GetToolVersionAsync(name, AppConstants.YtDlpExe).ConfigureAwait(false);
            _logger?.Info($"yt-dlp instalado com sucesso. Versão: {version}");
            progress?.Report((name, 100, $"Concluído ({version})"));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error("Falha ao baixar yt-dlp.", ex);
            FileHelper.SafeDeleteFile(tempFile);
            progress?.Report((name, 0, $"Erro: {ex.Message}"));
            return false;
        }
    }

    private async Task<bool> DownloadFFmpegAsync(
        IProgress<(string ToolName, double Progress, string Status)>? progress,
        CancellationToken ct)
    {
        const string name = "FFmpeg";
        _logger?.Info("Baixando FFmpeg oficial...");
        progress?.Report((name, 0, "Iniciando download do pacote FFmpeg"));

        FileHelper.EnsureDirectoryExists(AppConstants.FFmpegDir);
        var tempZip = Path.Combine(AppConstants.FFmpegDir, "ffmpeg.zip.tmp");

        try
        {
            var downloaded = await DownloadFileWithProgressAsync(
                AppConstants.FFmpegDownloadUrl,
                tempZip,
                p => progress?.Report((name, p * 0.8, $"Baixando FFmpeg ({p:0}%)")),
                ct).ConfigureAwait(false);

            if (!downloaded || !File.Exists(tempZip) || new FileInfo(tempZip).Length < 5000000)
            {
                throw new InvalidOperationException("Download do FFmpeg incompleto ou corrompido.");
            }

            progress?.Report((name, 85, "Extraindo ffmpeg.exe e ffprobe.exe..."));

            using (var archive = ZipFile.OpenRead(tempZip))
            {
                var ffmpegEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase));
                var ffprobeEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("ffprobe.exe", StringComparison.OrdinalIgnoreCase));

                if (ffmpegEntry == null || ffprobeEntry == null)
                {
                    throw new InvalidOperationException("Binários ffmpeg.exe ou ffprobe.exe não encontrados no arquivo compactado.");
                }

                var tempFfmpeg = Path.Combine(AppConstants.FFmpegDir, "ffmpeg.exe.tmp");
                var tempFfprobe = Path.Combine(AppConstants.FFmpegDir, "ffprobe.exe.tmp");

                ffmpegEntry.ExtractToFile(tempFfmpeg, overwrite: true);
                ffprobeEntry.ExtractToFile(tempFfprobe, overwrite: true);

                AtomicReplaceFile(tempFfmpeg, AppConstants.FFmpegExe);
                AtomicReplaceFile(tempFfprobe, AppConstants.FFprobeExe);
            }

            FileHelper.SafeDeleteFile(tempZip);

            var version = await GetToolVersionAsync(name, AppConstants.FFmpegExe).ConfigureAwait(false);
            _logger?.Info($"FFmpeg instalado com sucesso. Versão: {version}");
            progress?.Report((name, 100, $"Concluído ({version})"));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error("Falha ao baixar FFmpeg.", ex);
            FileHelper.SafeDeleteFile(tempZip);
            progress?.Report((name, 0, $"Erro: {ex.Message}"));
            return false;
        }
    }

    private async Task<bool> DownloadDenoAsync(
        IProgress<(string ToolName, double Progress, string Status)>? progress,
        CancellationToken ct)
    {
        const string name = "Deno";
        _logger?.Info("Baixando Deno oficial...");
        progress?.Report((name, 0, "Iniciando download do Deno"));

        FileHelper.EnsureDirectoryExists(AppConstants.DenoDir);
        var tempZip = Path.Combine(AppConstants.DenoDir, "deno.zip.tmp");

        try
        {
            var downloaded = await DownloadFileWithProgressAsync(
                AppConstants.DenoDownloadUrl,
                tempZip,
                p => progress?.Report((name, p * 0.8, $"Baixando Deno ({p:0}%)")),
                ct).ConfigureAwait(false);

            if (!downloaded || !File.Exists(tempZip) || new FileInfo(tempZip).Length < 5000000)
            {
                throw new InvalidOperationException("Download do Deno incompleto ou corrompido.");
            }

            progress?.Report((name, 85, "Extraindo deno.exe..."));

            using (var archive = ZipFile.OpenRead(tempZip))
            {
                var denoEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("deno.exe", StringComparison.OrdinalIgnoreCase));
                if (denoEntry == null)
                {
                    throw new InvalidOperationException("Binário deno.exe não encontrado no arquivo compactado.");
                }

                var tempDeno = Path.Combine(AppConstants.DenoDir, "deno.exe.tmp");
                denoEntry.ExtractToFile(tempDeno, overwrite: true);

                AtomicReplaceFile(tempDeno, AppConstants.DenoExe);
            }

            FileHelper.SafeDeleteFile(tempZip);

            var version = await GetToolVersionAsync(name, AppConstants.DenoExe).ConfigureAwait(false);
            _logger?.Info($"Deno instalado com sucesso. Versão: {version}");
            progress?.Report((name, 100, $"Concluído ({version})"));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error("Falha ao baixar Deno.", ex);
            FileHelper.SafeDeleteFile(tempZip);
            progress?.Report((name, 0, $"Erro: {ex.Message}"));
            return false;
        }
    }

    private async Task<bool> DownloadFileWithProgressAsync(
        string url,
        string destinationPath,
        Action<double>? onProgress,
        CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer = new byte[81920];
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
            totalBytesRead += bytesRead;

            if (totalBytes.HasValue && totalBytes.Value > 0)
            {
                var progress = (double)totalBytesRead / totalBytes.Value * 100.0;
                onProgress?.Invoke(progress);
            }
        }

        return true;
    }

    private static void AtomicReplaceFile(string sourceFile, string destinationFile)
    {
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException("Arquivo de origem para substituição atômica não existe.", sourceFile);

        var destinationDir = Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        var backupFile = destinationFile + ".bak";
        FileHelper.SafeDeleteFile(backupFile);

        if (File.Exists(destinationFile))
        {
            try
            {
                File.Move(destinationFile, backupFile);
            }
            catch
            {
                // Se não conseguir mover (arquivo em uso ou permissão), tenta cópia
            }
        }

        try
        {
            File.Move(sourceFile, destinationFile, overwrite: true);
            FileHelper.SafeDeleteFile(backupFile);
        }
        catch
        {
            // Se falhou, tenta restaurar backup
            if (File.Exists(backupFile) && !File.Exists(destinationFile))
            {
                try { File.Move(backupFile, destinationFile); } catch { }
            }
            throw;
        }
    }

    private static async Task<string> GetToolVersionAsync(string toolName, string exePath)
    {
        if (!File.Exists(exePath))
            return "Não instalado";

        try
        {
            var args = toolName.Equals("FFmpeg", StringComparison.OrdinalIgnoreCase) ||
                       toolName.Equals("ffprobe", StringComparison.OrdinalIgnoreCase)
                ? new[] { "-version" }
                : new[] { "--version" };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var result = await ProcessRunner.RunAsync(exePath, args, cancellationToken: cts.Token).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                var firstLine = result.StandardOutput
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(firstLine))
                {
                    if (toolName.Equals("FFmpeg", StringComparison.OrdinalIgnoreCase) && firstLine.StartsWith("ffmpeg version", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = firstLine.Split(' ');
                        return parts.Length > 2 ? parts[2] : firstLine;
                    }
                    if (toolName.Equals("Deno", StringComparison.OrdinalIgnoreCase) && firstLine.StartsWith("deno", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = firstLine.Split(' ');
                        return parts.Length > 1 ? parts[1] : firstLine;
                    }
                    return firstLine.Trim();
                }
            }
        }
        catch
        {
            // Ignora erro de versão
        }

        return "Presente";
    }
}
