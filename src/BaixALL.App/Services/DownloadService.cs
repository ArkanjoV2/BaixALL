using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BaixALL.App.Models;
using BaixALL.App.ViewModels;

namespace BaixALL.App.Services;

public class DownloadService : IDownloadService
{
    private readonly IYtDlpService _ytDlpService;
    private readonly IHistoryService _historyService;
    private readonly ISettingsService _settingsService;
    private readonly IDispatcherService _dispatcher;
    private readonly ILoggerService? _logger;

    private SemaphoreSlim _concurrencySemaphore;
    private int _maxConcurrent;

    public ObservableCollection<DownloadItemViewModel> QueueItems { get; } = new();

    public bool HasActiveDownloads => QueueItems.Any(x => x.IsActive);

    public DownloadService(
        IYtDlpService ytDlpService,
        IHistoryService historyService,
        ISettingsService settingsService,
        IDispatcherService? dispatcher = null,
        ILoggerService? logger = null)
    {
        _ytDlpService = ytDlpService;
        _historyService = historyService;
        _settingsService = settingsService;
        _dispatcher = dispatcher ?? new DispatcherService();
        _logger = logger;

        _maxConcurrent = Math.Clamp(_settingsService.Settings.MaxConcurrentDownloads, 1, 3);
        _concurrencySemaphore = new SemaphoreSlim(_maxConcurrent, _maxConcurrent);
    }

    public void UpdateConcurrencyLimit(int maxConcurrent)
    {
        var clamped = Math.Clamp(maxConcurrent, 1, 3);
        if (clamped == _maxConcurrent) return;

        _maxConcurrent = clamped;
        _concurrencySemaphore = new SemaphoreSlim(_maxConcurrent, _maxConcurrent);
        _logger?.Info($"Limite de concorrência alterado para: {_maxConcurrent}");
    }

    public DownloadItemViewModel EnqueueDownload(DownloadRequest request, string thumbnailUrl)
    {
        var qualityLabel = request.IsAudioOnly
            ? $"Áudio ({request.AudioFormat.Label})"
            : request.Format.Label;

        var formatLabel = request.IsAudioOnly
            ? request.AudioFormat.Extension.ToUpperInvariant()
            : (request.Container.Id == "auto" ? "MP4 (Auto)" : request.Container.Extension.ToUpperInvariant());

        var item = new DownloadItemViewModel
        {
            Title = request.VideoTitle,
            ThumbnailUrl = thumbnailUrl,
            Quality = qualityLabel,
            Format = formatLabel,
            Request = request
        };

        item.CancelRequested += (_, _) => CancelDownload(item.Id);
        item.RemoveRequested += (_, _) => RemoveDownload(item.Id);

        _dispatcher.Invoke(() => QueueItems.Insert(0, item));
        _logger?.Info($"Download enfileirado: '{request.VideoTitle}'");

        // Dispara processamento em segundo plano
        _ = Task.Run(() => ProcessDownloadItemAsync(item));

        return item;
    }

    public void CancelDownload(Guid id)
    {
        var item = QueueItems.FirstOrDefault(x => x.Id == id);
        if (item != null && item.IsActive)
        {
            try
            {
                item.CancellationTokenSource.Cancel();
            }
            catch { }
            _logger?.Info($"Cancelamento solicitado para: '{item.Title}'");
        }
    }

    public void RemoveDownload(Guid id)
    {
        var item = QueueItems.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            if (item.IsActive)
            {
                CancelDownload(id);
            }
            _dispatcher.Invoke(() => QueueItems.Remove(item));
        }
    }

    public void ClearCompleted()
    {
        var completed = QueueItems.Where(x => !x.IsActive).ToList();
        _dispatcher.Invoke(() =>
        {
            foreach (var item in completed)
            {
                QueueItems.Remove(item);
            }
        });
    }

    private async Task ProcessDownloadItemAsync(DownloadItemViewModel item)
    {
        try
        {
            await _concurrencySemaphore.WaitAsync(item.CancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _dispatcher.Invoke(() => item.MarkCanceled());
            return;
        }

        try
        {
            if (item.CancellationTokenSource.IsCancellationRequested)
            {
                _dispatcher.Invoke(() => item.MarkCanceled());
                return;
            }

            var progress = new Progress<DownloadProgressReport>(report =>
            {
                _dispatcher.Invoke(() => item.UpdateProgress(report));
            });

            var completedFilePath = await _ytDlpService.DownloadAsync(
                item.Request,
                progress,
                item.CancellationTokenSource.Token).ConfigureAwait(false);

            // Validação estrita da existência do arquivo final no disco
            if (!File.Exists(completedFilePath))
            {
                var baseFileName = Path.GetFileNameWithoutExtension(completedFilePath);
                var candidate = YtDlpService.FindActualOutputFile(item.Request.DestinationFolder, baseFileName);
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                {
                    completedFilePath = candidate;
                }
                else
                {
                    throw new FileNotFoundException("O arquivo resultante não foi localizado após a conclusão do processamento.", completedFilePath);
                }
            }

            // Marca o item como concluído com mensagem positiva na UI Thread
            _dispatcher.Invoke(() => item.MarkCompleted(completedFilePath));

            // Salva no histórico de forma segura
            long fileSize = 0;
            try
            {
                if (File.Exists(completedFilePath))
                {
                    fileSize = new FileInfo(completedFilePath).Length;
                }
            }
            catch { }

            _historyService.AddItem(new HistoryItem
            {
                Title = item.Title,
                Quality = item.Quality,
                Format = item.Format,
                FinalFilePath = completedFilePath,
                FileSizeBytes = fileSize,
                Status = "Concluído",
                ThumbnailUrl = item.ThumbnailUrl
            });
        }
        catch (OperationCanceledException)
        {
            _dispatcher.Invoke(() => item.MarkCanceled());
        }
        catch (Exception ex)
        {
            _logger?.Error($"Erro durante download de '{item.Title}'.", ex);
            string friendlyMessage = ex switch
            {
                FileNotFoundException => "O arquivo resultante não foi localizado após o download.",
                OperationCanceledException => "Download cancelado pelo usuário.",
                _ => "O download não pôde ser concluído."
            };
            _dispatcher.Invoke(() => item.MarkError(friendlyMessage));
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }
}
