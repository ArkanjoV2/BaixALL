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
    private readonly string _toolsFolder;
    private readonly string _ytDlpExe;
    private readonly string _ffmpegExe;
    private readonly string _ffprobeExe;
    private readonly string _denoExe;

    public DependencyManager(ILoggerService? logger = null, HttpClient? httpClient = null, string? toolsDirectory = null)
    {
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) BaixALL/1.0");

        _toolsFolder = toolsDirectory ?? AppConstants.ToolsFolder;
        _ytDlpExe = Path.Combine(_toolsFolder, "yt-dlp", "yt-dlp.exe");
        _ffmpegExe = Path.Combine(_toolsFolder, "ffmpeg", "ffmpeg.exe");
        _ffprobeExe = Path.Combine(_toolsFolder, "ffmpeg", "ffprobe.exe");
        _denoExe = Path.Combine(_toolsFolder, "deno", "deno.exe");

        _dependencies = new List<DependencyItem>
        {
            new()
            {
                Name = "yt-dlp",
                Description = "Motor principal de extração e download de vídeos",
                LocalPath = _ytDlpExe
            },
            new()
            {
                Name = "FFmpeg",
                Description = "Mesclagem de fluxos de vídeo/áudio e conversão de formatos",
                LocalPath = _ffmpegExe
            },
            new()
            {
                Name = "ffprobe",
                Description = "Analisador de codecs e streams multimídia",
                LocalPath = _ffprobeExe
            },
            new()
            {
                Name = "Deno",
                Description = "Runtime JavaScript para desafios anti-bot e assinaturas do YouTube",
                LocalPath = _denoExe
            }
        };
    }

    public IReadOnlyList<DependencyItem> GetDependencies() => _dependencies.AsReadOnly();

    public string GetYtDlpPath() => _ytDlpExe;
    public string GetFFmpegPath() => _ffmpegExe;
    public string GetFFprobePath() => _ffprobeExe;
    public string GetDenoPath() => _denoExe;

    public bool AreAllDependenciesInstalled()
    {
        return _dependencies.All(d => d.State == DependencyState.Installed && File.Exists(d.LocalPath));
    }

    public async Task<bool> CheckDependenciesAsync()
    {
        _logger?.Info("Verificando status das dependências locais...");

        foreach (var dep in _dependencies)
        {
            dep.SetChecking();

            if (File.Exists(dep.LocalPath))
            {
                dep.SetValidating();
                try
                {
                    var version = await GetToolVersionAsync(dep.Name, dep.LocalPath).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(version) && !version.Equals("Erro de execução", StringComparison.OrdinalIgnoreCase))
                    {
                        dep.SetInstalled(version);
                    }
                    else
                    {
                        dep.SetError("Binário presente, mas falhou ao executar.");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warning($"Falha na validação de {dep.Name}: {ex.Message}");
                    dep.SetError($"Falha na validação: {ex.Message}");
                }
            }
            else
            {
                dep.SetNotInstalled();
            }
        }

        return AreAllDependenciesInstalled();
    }

    public async Task<bool> ValidateDependencyAsync(string toolName)
    {
        var dep = _dependencies.FirstOrDefault(d => d.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        if (dep == null) return false;

        dep.SetValidating();

        if (!File.Exists(dep.LocalPath))
        {
            dep.SetNotInstalled();
            return false;
        }

        try
        {
            var version = await GetToolVersionAsync(dep.Name, dep.LocalPath).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(version) && !version.Equals("Erro de execução", StringComparison.OrdinalIgnoreCase))
            {
                dep.SetInstalled(version);
                return true;
            }
            else
            {
                dep.SetError("Falha na validação do executável.");
                return false;
            }
        }
        catch (Exception ex)
        {
            dep.SetError(ex.Message);
            return false;
        }
    }

    public async Task<bool> InstallDependencyByNameAsync(
        string toolName,
        IProgress<(string ToolName, double Progress, string Status)>? progress = null,
        CancellationToken ct = default)
    {
        var normalized = toolName.ToLowerInvariant();

        if (normalized.Contains("yt-dlp"))
        {
            return await DownloadYtDlpAsync(progress, ct).ConfigureAwait(false);
        }
        else if (normalized.Contains("ffmpeg") || normalized.Contains("ffprobe"))
        {
            return await DownloadFFmpegAsync(progress, ct).ConfigureAwait(false);
        }
        else if (normalized.Contains("deno"))
        {
            return await DownloadDenoAsync(progress, ct).ConfigureAwait(false);
        }

        _logger?.Warning($"Dependência não reconhecida para instalação: {toolName}");
        return false;
    }

    public async Task<bool> EnsureAllDependenciesAsync(
        IProgress<(string ToolName, double Progress, string Status)>? progress = null,
        CancellationToken ct = default)
    {
        await CheckDependenciesAsync().ConfigureAwait(false);

        // 1. yt-dlp
        var ytDep = _dependencies.First(d => d.Name.Equals("yt-dlp", StringComparison.OrdinalIgnoreCase));
        if (ytDep.State != DependencyState.Installed)
        {
            var success = await DownloadYtDlpAsync(progress, ct).ConfigureAwait(false);
            if (!success) return false;
        }

        // 2. FFmpeg e ffprobe
        var ffmpegDep = _dependencies.First(d => d.Name.Equals("FFmpeg", StringComparison.OrdinalIgnoreCase));
        var ffprobeDep = _dependencies.First(d => d.Name.Equals("ffprobe", StringComparison.OrdinalIgnoreCase));
        if (ffmpegDep.State != DependencyState.Installed || ffprobeDep.State != DependencyState.Installed)
        {
            var success = await DownloadFFmpegAsync(progress, ct).ConfigureAwait(false);
            if (!success) return false;
        }

        // 3. Deno
        var denoDep = _dependencies.First(d => d.Name.Equals("Deno", StringComparison.OrdinalIgnoreCase));
        if (denoDep.State != DependencyState.Installed)
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
        return await InstallDependencyByNameAsync(toolName, progress, ct).ConfigureAwait(false);
    }

    private async Task<bool> DownloadYtDlpAsync(
        IProgress<(string ToolName, double Progress, string Status)>? progress,
        CancellationToken ct)
    {
        const string name = "yt-dlp";
        var dep = _dependencies.First(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        _logger?.Info("Baixando yt-dlp oficial...");
        dep.SetDownloading(0, "Iniciando download...");
        progress?.Report((name, 0, "Iniciando download"));

        var ytDlpDir = Path.GetDirectoryName(_ytDlpExe)!;
        FileHelper.EnsureDirectoryExists(ytDlpDir);
        var tempFile = _ytDlpExe + ".tmp";

        try
        {
            var downloaded = await DownloadFileWithProgressAsync(
                AppConstants.YtDlpDownloadUrl,
                tempFile,
                p =>
                {
                    dep.SetDownloading(p, $"Baixando ({p:0}%)");
                    progress?.Report((name, p, $"Baixando yt-dlp ({p:0}%)"));
                },
                ct).ConfigureAwait(false);

            if (!downloaded || !File.Exists(tempFile) || new FileInfo(tempFile).Length < 1000000)
            {
                throw new InvalidOperationException("Download do yt-dlp incompleto ou corrompido.");
            }

            dep.SetInstalling("Instalando executável...");
            progress?.Report((name, 90, "Instalando executável..."));

            // Substituição atômica segura
            AtomicReplaceFile(tempFile, _ytDlpExe);

            dep.SetValidating();
            progress?.Report((name, 95, "Validando execução (yt-dlp --version)..."));

            var version = await GetToolVersionAsync(name, _ytDlpExe).ConfigureAwait(false);
            if (string.IsNullOrEmpty(version) || version.Equals("Erro de execução", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Falha na validação do yt-dlp após o download.");
            }

            dep.SetInstalled(version);
            _logger?.Info($"yt-dlp instalado com sucesso. Versão: {version}");
            progress?.Report((name, 100, $"Concluído ({version})"));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error("Falha ao baixar yt-dlp.", ex);
            FileHelper.SafeDeleteFile(tempFile);
            dep.SetError($"Não foi possível instalar yt-dlp: {ex.Message}");
            progress?.Report((name, 0, $"Erro: {ex.Message}"));
            return false;
        }
    }

    private async Task<bool> DownloadFFmpegAsync(
        IProgress<(string ToolName, double Progress, string Status)>? progress,
        CancellationToken ct)
    {
        const string name = "FFmpeg";
        var ffmpegDep = _dependencies.First(d => d.Name.Equals("FFmpeg", StringComparison.OrdinalIgnoreCase));
        var ffprobeDep = _dependencies.First(d => d.Name.Equals("ffprobe", StringComparison.OrdinalIgnoreCase));

        _logger?.Info("Baixando FFmpeg oficial...");
        ffmpegDep.SetDownloading(0, "Iniciando download do pacote FFmpeg...");
        ffprobeDep.SetDownloading(0, "Aguardando pacote FFmpeg...");
        progress?.Report((name, 0, "Iniciando download do pacote FFmpeg"));

        var ffmpegDir = Path.GetDirectoryName(_ffmpegExe)!;
        FileHelper.EnsureDirectoryExists(ffmpegDir);
        var tempZip = Path.Combine(ffmpegDir, "ffmpeg.zip.tmp");

        try
        {
            var downloaded = await DownloadFileWithProgressAsync(
                AppConstants.FFmpegDownloadUrl,
                tempZip,
                p =>
                {
                    ffmpegDep.SetDownloading(p, $"Baixando pacote ({p:0}%)");
                    ffprobeDep.SetDownloading(p, $"Baixando pacote ({p:0}%)");
                    progress?.Report((name, p * 0.8, $"Baixando FFmpeg ({p:0}%)"));
                },
                ct).ConfigureAwait(false);

            if (!downloaded || !File.Exists(tempZip) || new FileInfo(tempZip).Length < 5000000)
            {
                throw new InvalidOperationException("Download do FFmpeg incompleto ou corrompido.");
            }

            ffmpegDep.SetInstalling("Extraindo ffmpeg.exe...");
            ffprobeDep.SetInstalling("Extraindo ffprobe.exe...");
            progress?.Report((name, 85, "Extraindo ffmpeg.exe e ffprobe.exe..."));

            using (var archive = ZipFile.OpenRead(tempZip))
            {
                var ffmpegEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase));
                var ffprobeEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("ffprobe.exe", StringComparison.OrdinalIgnoreCase));

                if (ffmpegEntry == null || ffprobeEntry == null)
                {
                    throw new InvalidOperationException("Binários ffmpeg.exe ou ffprobe.exe não encontrados no pacote baixado.");
                }

                var tempFfmpeg = _ffmpegExe + ".tmp";
                var tempFfprobe = _ffprobeExe + ".tmp";

                ffmpegEntry.ExtractToFile(tempFfmpeg, overwrite: true);
                ffprobeEntry.ExtractToFile(tempFfprobe, overwrite: true);

                AtomicReplaceFile(tempFfmpeg, _ffmpegExe);
                AtomicReplaceFile(tempFfprobe, _ffprobeExe);
            }

            FileHelper.SafeDeleteFile(tempZip);

            ffmpegDep.SetValidating();
            ffprobeDep.SetValidating();
            progress?.Report((name, 95, "Validando FFmpeg e ffprobe..."));

            var ffmpegVersion = await GetToolVersionAsync("FFmpeg", _ffmpegExe).ConfigureAwait(false);
            var ffprobeVersion = await GetToolVersionAsync("ffprobe", _ffprobeExe).ConfigureAwait(false);

            if (string.IsNullOrEmpty(ffmpegVersion) || ffmpegVersion.Equals("Erro de execução", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Falha na validação do FFmpeg após o download.");
            }

            ffmpegDep.SetInstalled(ffmpegVersion);
            ffprobeDep.SetInstalled(ffprobeVersion);

            _logger?.Info($"FFmpeg instalado com sucesso: {ffmpegVersion} / {ffprobeVersion}");
            progress?.Report((name, 100, $"Concluído ({ffmpegVersion})"));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error("Falha ao baixar FFmpeg.", ex);
            FileHelper.SafeDeleteFile(tempZip);
            ffmpegDep.SetError($"Não foi possível instalar FFmpeg: {ex.Message}");
            ffprobeDep.SetError($"Não foi possível instalar ffprobe: {ex.Message}");
            progress?.Report((name, 0, $"Erro: {ex.Message}"));
            return false;
        }
    }

    private async Task<bool> DownloadDenoAsync(
        IProgress<(string ToolName, double Progress, string Status)>? progress,
        CancellationToken ct)
    {
        const string name = "Deno";
        var dep = _dependencies.First(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        _logger?.Info("Baixando Deno oficial...");
        dep.SetDownloading(0, "Iniciando download do Deno...");
        progress?.Report((name, 0, "Iniciando download do Deno"));

        var denoDir = Path.GetDirectoryName(_denoExe)!;
        FileHelper.EnsureDirectoryExists(denoDir);
        var tempZip = Path.Combine(denoDir, "deno.zip.tmp");

        try
        {
            var downloaded = await DownloadFileWithProgressAsync(
                AppConstants.DenoDownloadUrl,
                tempZip,
                p =>
                {
                    dep.SetDownloading(p, $"Baixando ({p:0}%)");
                    progress?.Report((name, p * 0.8, $"Baixando Deno ({p:0}%)"));
                },
                ct).ConfigureAwait(false);

            if (!downloaded || !File.Exists(tempZip) || new FileInfo(tempZip).Length < 5000000)
            {
                throw new InvalidOperationException("Download do Deno incompleto ou corrompido.");
            }

            dep.SetInstalling("Extraindo deno.exe...");
            progress?.Report((name, 85, "Extraindo deno.exe..."));

            using (var archive = ZipFile.OpenRead(tempZip))
            {
                var denoEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("deno.exe", StringComparison.OrdinalIgnoreCase));
                if (denoEntry == null)
                {
                    throw new InvalidOperationException("Binário deno.exe não encontrado no pacote compactado.");
                }

                var tempDeno = _denoExe + ".tmp";
                denoEntry.ExtractToFile(tempDeno, overwrite: true);

                AtomicReplaceFile(tempDeno, _denoExe);
            }

            FileHelper.SafeDeleteFile(tempZip);

            dep.SetValidating();
            progress?.Report((name, 95, "Validando execução do Deno..."));

            var version = await GetToolVersionAsync(name, _denoExe).ConfigureAwait(false);
            if (string.IsNullOrEmpty(version) || version.Equals("Erro de execução", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Falha na validação do Deno após o download.");
            }

            dep.SetInstalled(version);
            _logger?.Info($"Deno instalado com sucesso. Versão: {version}");
            progress?.Report((name, 100, $"Concluído ({version})"));
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error("Falha ao baixar Deno.", ex);
            FileHelper.SafeDeleteFile(tempZip);
            dep.SetError($"Não foi possível instalar Deno: {ex.Message}");
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
                // Se não conseguir mover, tenta sobrescrever diretamente
            }
        }

        try
        {
            File.Move(sourceFile, destinationFile, overwrite: true);
            FileHelper.SafeDeleteFile(backupFile);
        }
        catch
        {
            if (File.Exists(backupFile) && !File.Exists(destinationFile))
            {
                try { File.Move(backupFile, destinationFile); } catch { }
            }
            throw;
        }
    }

    public static async Task<string> GetToolVersionAsync(string toolName, string exePath)
    {
        if (!File.Exists(exePath))
            return "Não instalado";

        try
        {
            var args = toolName.Equals("FFmpeg", StringComparison.OrdinalIgnoreCase) ||
                       toolName.Equals("ffprobe", StringComparison.OrdinalIgnoreCase)
                ? new[] { "-version" }
                : new[] { "--version" };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
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
                    if (toolName.Equals("ffprobe", StringComparison.OrdinalIgnoreCase) && firstLine.StartsWith("ffprobe version", StringComparison.OrdinalIgnoreCase))
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
            return "Erro de execução";
        }

        return "Erro de execução";
    }
}
