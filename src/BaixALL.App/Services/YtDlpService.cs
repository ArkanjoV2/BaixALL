using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Helpers;
using BaixALL.App.Infrastructure;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class YtDlpService : IYtDlpService
{
    private readonly IDependencyManager _dependencyManager;
    private readonly ILoggerService? _logger;

    public const string ProgressTemplate = "baixall_prog:[%(progress.downloaded_bytes)s/%(progress.total_bytes_estimate)s|%(progress.speed)s|%(progress.eta)s|%(progress._percent_str)s|%(progress.status)s]";

    public YtDlpService(IDependencyManager dependencyManager, ILoggerService? logger = null)
    {
        _dependencyManager = dependencyManager;
        _logger = logger;
    }

    public async Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default)
    {
        var ytDlpPath = _dependencyManager.GetYtDlpPath();
        if (!File.Exists(ytDlpPath))
        {
            throw new FileNotFoundException("O executável do yt-dlp não está instalado. Verifique as ferramentas.", ytDlpPath);
        }

        var arguments = new List<string>
        {
            "-J",
            "--no-playlist",
            "--skip-download",
            "--no-warnings",
            "--no-check-certificates"
        };

        // Adiciona localização do ffmpeg se existir
        var ffmpegDir = Path.GetDirectoryName(_dependencyManager.GetFFmpegPath());
        if (!string.IsNullOrEmpty(ffmpegDir) && Directory.Exists(ffmpegDir))
        {
            arguments.Add("--ffmpeg-location");
            arguments.Add(ffmpegDir);
        }

        arguments.Add(url);

        var env = BuildProcessEnvironment();

        _logger?.Info($"Obtendo metadados para URL: {url}");

        var result = await ProcessRunner.RunAsync(
            ytDlpPath,
            arguments,
            environmentVariables: env,
            cancellationToken: ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var friendlyError = ParseYtDlpError(result.StandardError);
            _logger?.Error($"Falha ao analisar vídeo. Saída: {result.StandardError}");
            throw new InvalidOperationException(friendlyError);
        }

        try
        {
            var json = JsonDocument.Parse(result.StandardOutput);
            return json;
        }
        catch (JsonException ex)
        {
            _logger?.Error("Falha ao interpretar JSON retornado pelo yt-dlp.", ex);
            throw new InvalidOperationException("Não foi possível interpretar a resposta estruturada do YouTube.", ex);
        }
    }

    public async Task<JsonDocument> GetPlaylistMetadataJsonAsync(string playlistUrl, CancellationToken ct = default)
    {
        var ytDlpPath = _dependencyManager.GetYtDlpPath();
        if (!File.Exists(ytDlpPath))
        {
            throw new FileNotFoundException("O executável do yt-dlp não está instalado. Verifique as ferramentas.", ytDlpPath);
        }

        var arguments = new List<string>
        {
            "-J",
            "--flat-playlist",
            "--skip-download",
            "--no-warnings",
            "--no-check-certificates"
        };

        var ffmpegDir = Path.GetDirectoryName(_dependencyManager.GetFFmpegPath());
        if (!string.IsNullOrEmpty(ffmpegDir) && Directory.Exists(ffmpegDir))
        {
            arguments.Add("--ffmpeg-location");
            arguments.Add(ffmpegDir);
        }

        arguments.Add(playlistUrl);

        var env = BuildProcessEnvironment();

        _logger?.Info($"Obtendo metadados estruturados da playlist: {playlistUrl}");

        var result = await ProcessRunner.RunAsync(
            ytDlpPath,
            arguments,
            environmentVariables: env,
            cancellationToken: ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var friendlyError = ParseYtDlpError(result.StandardError);
            _logger?.Error($"Falha ao analisar playlist. Saída: {result.StandardError}");
            throw new InvalidOperationException(friendlyError);
        }

        try
        {
            var json = JsonDocument.Parse(result.StandardOutput);
            return json;
        }
        catch (JsonException ex)
        {
            _logger?.Error("Falha ao interpretar JSON da playlist retornado pelo yt-dlp.", ex);
            throw new InvalidOperationException("Não foi possível interpretar os dados da playlist do YouTube.", ex);
        }
    }

    public async Task<string> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgressReport> progress,
        CancellationToken ct = default)
    {
        var ytDlpPath = _dependencyManager.GetYtDlpPath();
        if (!File.Exists(ytDlpPath))
        {
            throw new FileNotFoundException("O executável do yt-dlp não está instalado.", ytDlpPath);
        }

        FileHelper.EnsureDirectoryExists(request.DestinationFolder);

        // Cria diretório temporário exclusivo e isolado para este job
        var jobId = Guid.NewGuid().ToString("N");
        var jobTempDir = Path.Combine(Path.GetTempPath(), "BaixALL_Jobs", jobId);
        Directory.CreateDirectory(jobTempDir);

        var outputTemplate = Path.Combine(jobTempDir, "stream.%(ext)s");

        var arguments = new List<string>
        {
            "--newline",
            "--no-playlist",
            "--no-warnings",
            "--progress-template", ProgressTemplate
        };

        // Aponta para o FFmpeg
        var ffmpegDir = Path.GetDirectoryName(_dependencyManager.GetFFmpegPath());
        if (!string.IsNullOrEmpty(ffmpegDir) && Directory.Exists(ffmpegDir))
        {
            arguments.Add("--ffmpeg-location");
            arguments.Add(ffmpegDir);
        }

        if (request.IsAudioOnly)
        {
            arguments.Add("-f");
            arguments.Add("bestaudio/best");
            arguments.Add("-x");

            if (request.AudioFormat.Id == "mp3")
            {
                arguments.Add("--audio-format");
                arguments.Add("mp3");
                arguments.Add("--audio-quality");
                arguments.Add("0"); // Melhor qualidade VBR MP3 (~320kbps)
            }
            else if (request.AudioFormat.Id == "m4a")
            {
                arguments.Add("--audio-format");
                arguments.Add("m4a");
            }
            else if (request.AudioFormat.Id == "opus")
            {
                arguments.Add("--audio-format");
                arguments.Add("opus");
            }
        }
        else
        {
            // Seleção de formato de vídeo
            arguments.Add("-f");
            arguments.Add(request.Format.FormatSelector);

            if (request.Container.Id != "auto")
            {
                arguments.Add("--merge-output-format");
                arguments.Add(request.Container.Extension);
                arguments.Add("--remux-video");
                arguments.Add(request.Container.Extension);
            }
            else
            {
                // No modo automático, mescla para mp4 ou mkv conforme melhor compatibilidade
                arguments.Add("--merge-output-format");
                arguments.Add("mp4/mkv");
            }
        }

        arguments.Add("-o");
        arguments.Add(outputTemplate);
        arguments.Add(request.VideoUrl);

        var env = BuildProcessEnvironment();

        _logger?.Info($"Iniciando download de '{request.VideoTitle}' no job '{jobId}'");

        progress.Report(new DownloadProgressReport
        {
            Status = DownloadStatus.Preparing,
            StatusMessage = "Preparando conexão...",
            Percentage = 0
        });

        var currentReport = new DownloadProgressReport
        {
            Status = DownloadStatus.Preparing,
            StatusMessage = "Iniciando download..."
        };

        try
        {
            var result = await ProcessRunner.RunAsync(
                ytDlpPath,
                arguments,
                workingDirectory: jobTempDir,
                environmentVariables: env,
                onOutputLine: line =>
                {
                    HandleProgressLine(line, currentReport, progress, request.IsAudioOnly);
                },
                onErrorLine: line =>
                {
                    _logger?.Debug($"yt-dlp stderr: {line}");
                },
                cancellationToken: ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(ct);
                }
                var friendlyError = ParseYtDlpError(result.StandardError);
                _logger?.Error($"Falha no download. Erro: {result.StandardError}");
                throw new InvalidOperationException(friendlyError);
            }

            // Localiza o arquivo final resultante dentro do diretório temporário do job
            var generatedFiles = Directory.GetFiles(jobTempDir)
                .Where(f => !f.EndsWith(".part", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".temp", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var producedFile = generatedFiles.FirstOrDefault();
            if (producedFile == null || !File.Exists(producedFile))
            {
                throw new FileNotFoundException("O arquivo resultante não foi localizado no diretório de processamento.", jobTempDir);
            }

            var actualExtension = Path.GetExtension(producedFile).TrimStart('.');
            if (string.IsNullOrWhiteSpace(actualExtension))
            {
                actualExtension = request.IsAudioOnly
                    ? request.AudioFormat.Extension
                    : (request.Container.Id == "auto" ? "mp4" : request.Container.Extension);
            }

            // Garante nome de arquivo único e sem colisões na pasta de destino final
            var finalDestinationPath = FileHelper.GetUniqueFilePath(
                request.DestinationFolder,
                request.VideoTitle,
                actualExtension);

            File.Move(producedFile, finalDestinationPath, overwrite: true);

            progress.Report(new DownloadProgressReport
            {
                Status = DownloadStatus.Completed,
                StatusMessage = "Download concluído!",
                Percentage = 100
            });

            _logger?.Info($"Download concluído com sucesso: {finalDestinationPath}");
            return finalDestinationPath;
        }
        catch (OperationCanceledException)
        {
            _logger?.Warning($"Download de '{request.VideoTitle}' cancelado pelo usuário.");
            progress.Report(new DownloadProgressReport
            {
                Status = DownloadStatus.Canceled,
                StatusMessage = "Download cancelado.",
                Percentage = 0
            });
            throw;
        }
        catch (Exception ex)
        {
            progress.Report(new DownloadProgressReport
            {
                Status = DownloadStatus.Error,
                StatusMessage = ex.Message,
                Percentage = 0
            });
            throw;
        }
        finally
        {
            try
            {
                if (Directory.Exists(jobTempDir))
                {
                    Directory.Delete(jobTempDir, recursive: true);
                }
            }
            catch { }
        }
    }

    public static bool ParseProgressLine(string line, out long downloaded, out long? total, out double speed, out double? eta, out double percent, out string status)
    {
        downloaded = 0;
        total = null;
        speed = 0;
        eta = null;
        percent = 0;
        status = string.Empty;

        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("baixall_prog:[") || !line.EndsWith("]"))
            return false;

        var inside = line.Substring(14, line.Length - 15);
        var parts = inside.Split('|');
        if (parts.Length < 5) return false;

        // 1. Downloaded / Total
        var sizePart = parts[0];
        var sizeTokens = sizePart.Split('/');
        if (sizeTokens.Length >= 1 && long.TryParse(sizeTokens[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            downloaded = d;
        }
        if (sizeTokens.Length >= 2 && long.TryParse(sizeTokens[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var t))
        {
            total = t;
        }

        // 2. Speed (bytes/sec)
        if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
        {
            speed = s;
        }

        // 3. ETA (sec)
        if (double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var e))
        {
            eta = e;
        }

        // 4. Percent
        var percentStr = parts[3].Replace("%", "").Trim();
        if (double.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
        {
            percent = p;
        }

        // 5. Status
        status = parts[4];

        return true;
    }

    private void HandleProgressLine(
        string line,
        DownloadProgressReport current,
        IProgress<DownloadProgressReport> progress,
        bool isAudioOnly)
    {
        if (ParseProgressLine(line, out var downloaded, out var total, out var speed, out var eta, out var percent, out var ytStatus))
        {
            current.DownloadedBytes = downloaded;
            current.TotalBytes = total;
            current.SpeedBytesPerSec = speed;
            current.EtaSeconds = eta;
            current.Percentage = percent;

            if (ytStatus.Equals("finished", StringComparison.OrdinalIgnoreCase))
            {
                current.Status = DownloadStatus.Finalizing;
                current.StatusMessage = "Finalizando processamento...";
            }
            else
            {
                current.Status = isAudioOnly ? DownloadStatus.DownloadingAudio : DownloadStatus.DownloadingVideo;
                current.StatusMessage = isAudioOnly ? "Baixando áudio..." : "Baixando vídeo...";
            }

            progress.Report(current);
            return;
        }

        // Detecta transições pelo output do yt-dlp / ffmpeg
        if (line.Contains("[Merger]") || line.Contains("Merging formats into"))
        {
            current.Status = DownloadStatus.Merging;
            current.StatusMessage = "Mesclando áudio e vídeo (FFmpeg)...";
            progress.Report(current);
        }
        else if (line.Contains("[ExtractAudio]") || line.Contains("Destination:"))
        {
            current.Status = DownloadStatus.Converting;
            current.StatusMessage = "Convertendo faixa de áudio...";
            progress.Report(current);
        }
        else if (line.Contains("[ffmpeg]") || line.Contains("Fixing"))
        {
            current.Status = DownloadStatus.Finalizing;
            current.StatusMessage = "Ajustando container com FFmpeg...";
            progress.Report(current);
        }
    }

    public static string? FindActualOutputFile(string folder, string baseFileName)
    {
        try
        {
            var files = Directory.GetFiles(folder, $"{baseFileName}.*")
                .Where(f => !f.EndsWith(".part") && !f.EndsWith(".ytdl") && !f.EndsWith(".tmp"))
                .ToList();

            return files.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static void CleanupPartialFiles(string folder, string baseFileName)
    {
        try
        {
            if (!Directory.Exists(folder)) return;

            var partialFiles = Directory.GetFiles(folder, $"{baseFileName}*")
                .Where(f => f.EndsWith(".part", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".temp", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                            System.Text.RegularExpressions.Regex.IsMatch(f, @"\.f\d+\.[^.]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase));

            foreach (var file in partialFiles)
            {
                FileHelper.SafeDeleteFile(file);
            }
        }
        catch { }
    }

    private Dictionary<string, string> BuildProcessEnvironment()
    {
        var env = new Dictionary<string, string>();
        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        // Inclui diretórios gerenciados de Deno e FFmpeg no PATH do processo filho
        var denoDir = Path.GetDirectoryName(_dependencyManager.GetDenoPath());
        var ffmpegDir = Path.GetDirectoryName(_dependencyManager.GetFFmpegPath());

        var extraPaths = new List<string>();
        if (!string.IsNullOrEmpty(denoDir) && Directory.Exists(denoDir)) extraPaths.Add(denoDir);
        if (!string.IsNullOrEmpty(ffmpegDir) && Directory.Exists(ffmpegDir)) extraPaths.Add(ffmpegDir);

        if (extraPaths.Any())
        {
            env["PATH"] = string.Join(";", extraPaths) + ";" + currentPath;
        }

        return env;
    }

    public static string ParseYtDlpError(string? rawError)
    {
        if (string.IsNullOrWhiteSpace(rawError))
            return "Ocorreu um erro desconhecido durante a operação.";

        var lower = rawError.ToLowerInvariant();

        if (lower.Contains("private video"))
            return "Este vídeo é privado e não pode ser acessado publicamente.";

        if (lower.Contains("video unavailable") || lower.Contains("is unavailable") || lower.Contains("not available") || lower.Contains("video is unavailable"))
            return "Este vídeo não está disponível no YouTube.";

        if (lower.Contains("this video has been removed") || lower.Contains("video has been removed"))
            return "Este vídeo foi removido pelo YouTube ou pelo criador.";

        if (lower.Contains("sign in to confirm your age"))
            return "Este vídeo requer autenticação de idade e não pode ser baixado sem login.";

        if (lower.Contains("confirm you’re not a bot") || lower.Contains("bot detection"))
            return "O YouTube solicitou verificação anti-bot. Certifique-se de que o Deno está atualizado.";

        if (lower.Contains("unable to download") || lower.Contains("no internet") || lower.Contains("failed to establish a new connection") || lower.Contains("getaddrinfo failed"))
            return "Falha de conexão com o YouTube. Verifique sua conexão com a internet.";

        if (lower.Contains("incomplete youtube id") || lower.Contains("is not a valid url"))
            return "A URL informada não é válida para o YouTube.";

        if (lower.Contains("http error 403"))
            return "O YouTube recusou o acesso (HTTP 403). Tente atualizar o yt-dlp nas configurações.";

        if (lower.Contains("disk full") || lower.Contains("there is not enough space on the disk"))
            return "Não há espaço em disco suficiente para completar o download.";

        // Retorna a última linha relevante de erro se não mapeada
        var lines = rawError.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (lines.Any())
        {
            return lines.Last().Replace("ERROR:", "").Trim();
        }

        return "O YouTube retornou um erro ao processar o vídeo. Verifique os logs para detalhes técnicos.";
    }
}
