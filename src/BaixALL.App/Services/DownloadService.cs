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
    private readonly ILoggerService? _logger;

    private SemaphoreSlim _concurrencySemaphore;
    private int _maxConcurrent;

    public ObservableCollection<DownloadItemViewModel> QueueItems { get; } = new();

    public bool HasActiveDownloads => QueueItems.Any(x => x.IsActive);

    public DownloadService(
        IYtDlpService ytDlpService,
        IHistoryService historyService,
        ISettingsService settingsService,
        ILoggerService? logger = null)
    {
        _ytDlpService = ytDlpService;
        _historyService = historyService;
        _settingsService = settingsService;
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

        RunOnUI(() => QueueItems.Insert(0, item));
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
            RunOnUI(() => QueueItems.Remove(item));
        }
    }

    public void ClearCompleted()
    {
        var completed = QueueItems.Where(x => !x.IsActive).ToList();
        RunOnUI(() =>
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
            RunOnUI(() => item.MarkCanceled());
            return;
        }

        try
        {
            if (item.CancellationTokenSource.IsCancellationRequested)
            {
                RunOnUI(() => item.MarkCanceled());
                return;
            }

            var progress = new Progress<DownloadProgressReport>(report =>
            {
                RunOnUI(() => item.UpdateProgress(report));
            });

            var completedFilePath = await _ytDlpService.DownloadAsync(
                item.Request,
                progress,
                item.CancellationTokenSource.Token).ConfigureAwait(false);

            RunOnUI(() => item.MarkCompleted(completedFilePath));

            // Salva no histórico
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
            RunOnUI(() => item.MarkCanceled());
        }
        catch (Exception ex)
        {
            _logger?.Error($"Erro durante download de '{item.Title}'.", ex);
            RunOnUI(() => item.MarkError(ex.Message));
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    private static void RunOnUI(Action action)
    {
        if (Application.Current != null && Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }
}
