using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    private readonly object _syncLock = new();
    private readonly List<DownloadItemViewModel> _pendingQueue = new();
    private int _activeCount;
    private int _maxConcurrent;

    public ObservableCollection<DownloadItemViewModel> QueueItems { get; } = new();

    public int ActiveDownloadsCount
    {
        get
        {
            lock (_syncLock)
            {
                return _activeCount;
            }
        }
    }

    public bool HasActiveDownloads
    {
        get
        {
            lock (_syncLock)
            {
                return _activeCount > 0 || _pendingQueue.Any(x => x.IsActive) || QueueItems.Any(x => x.IsActive);
            }
        }
    }

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
    }

    public void UpdateConcurrencyLimit(int maxConcurrent)
    {
        var clamped = Math.Clamp(maxConcurrent, 1, 3);
        lock (_syncLock)
        {
            if (clamped == _maxConcurrent) return;
            _maxConcurrent = clamped;
        }
        _logger?.Info($"Limite de concorrência alterado para: {clamped}");
        TryStartNextDownloads();
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
            Request = request,
            Status = DownloadStatus.Queued,
            StatusMessage = "Aguardando vaga na fila..."
        };

        item.CancelRequested += (_, _) => CancelDownload(item.Id);
        item.RemoveRequested += (_, _) => RemoveDownload(item.Id);

        _dispatcher.Invoke(() => QueueItems.Insert(0, item));
        _logger?.Info($"Download enfileirado: '{request.VideoTitle}'");

        lock (_syncLock)
        {
            _pendingQueue.Add(item);
        }

        TryStartNextDownloads();

        return item;
    }

    public void CancelDownload(Guid id)
    {
        DownloadItemViewModel? item = null;
        bool wasWaitingInQueue = false;

        lock (_syncLock)
        {
            item = QueueItems.FirstOrDefault(x => x.Id == id);
            if (item == null || !item.IsActive) return;

            if (_pendingQueue.Contains(item))
            {
                _pendingQueue.Remove(item);
                wasWaitingInQueue = true;
            }
        }

        try
        {
            item.CancellationTokenSource.Cancel();
        }
        catch { }

        if (wasWaitingInQueue)
        {
            _dispatcher.Invoke(() => item.MarkCanceled());
            _logger?.Info($"Download que estava aguardando na fila foi cancelado: '{item.Title}'");
        }
        else
        {
            _logger?.Info($"Cancelamento solicitado para download ativo: '{item.Title}'");
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
            lock (_syncLock)
            {
                _pendingQueue.Remove(item);
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

    public void CancelAllDownloads()
    {
        List<DownloadItemViewModel> toCancel;
        lock (_syncLock)
        {
            toCancel = QueueItems.Where(x => x.IsActive).ToList();
            _pendingQueue.Clear();
        }

        foreach (var item in toCancel)
        {
            try
            {
                item.CancellationTokenSource.Cancel();
                if (item.Status == DownloadStatus.Queued)
                {
                    _dispatcher.Invoke(() => item.MarkCanceled());
                }
            }
            catch { }
        }
        _logger?.Info($"Cancelamento solicitado para {toCancel.Count} download(s).");
    }

    private void TryStartNextDownloads()
    {
        while (true)
        {
            DownloadItemViewModel? nextItem = null;
            lock (_syncLock)
            {
                // Limpa itens cancelados que estejam aguardando
                _pendingQueue.RemoveAll(x => !x.IsActive || x.CancellationTokenSource.IsCancellationRequested);

                if (_activeCount >= _maxConcurrent)
                {
                    return;
                }

                nextItem = _pendingQueue.FirstOrDefault(x => x.Status == DownloadStatus.Queued && !x.CancellationTokenSource.IsCancellationRequested);
                if (nextItem == null)
                {
                    return;
                }

                _pendingQueue.Remove(nextItem);
                _activeCount++;
            }

            var itemToRun = nextItem;
            _ = Task.Run(() => ProcessDownloadItemAsync(itemToRun));
        }
    }

    private async Task ProcessDownloadItemAsync(DownloadItemViewModel item)
    {
        try
        {
            if (item.CancellationTokenSource.IsCancellationRequested)
            {
                _dispatcher.Invoke(() => item.MarkCanceled());
                return;
            }

            _dispatcher.Invoke(() =>
            {
                item.Status = DownloadStatus.Preparing;
                item.StatusMessage = "Iniciando download...";
            });

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
            if (item.CancellationTokenSource.IsCancellationRequested)
            {
                _dispatcher.Invoke(() => item.MarkCanceled());
                return;
            }

            _logger?.Error($"Erro durante download de '{item.Title}'.", ex);
            string friendlyMessage = ex switch
            {
                FileNotFoundException => "O arquivo resultante não foi localizado após o download.",
                _ => "O download não pôde ser concluído."
            };
            _dispatcher.Invoke(() => item.MarkError(friendlyMessage));
        }
        finally
        {
            lock (_syncLock)
            {
                _activeCount--;
                if (_activeCount < 0) _activeCount = 0;
            }
            TryStartNextDownloads();
        }
    }
}
