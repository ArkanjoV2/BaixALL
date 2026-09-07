using System;
using System.Collections.ObjectModel;
using BaixALL.App.Models;
using BaixALL.App.ViewModels;

namespace BaixALL.App.Services;

public interface IDownloadService
{
    ObservableCollection<DownloadItemViewModel> QueueItems { get; }
    bool HasActiveDownloads { get; }
    DownloadItemViewModel EnqueueDownload(DownloadRequest request, string thumbnailUrl);
    void CancelDownload(Guid id);
    void RemoveDownload(Guid id);
    void ClearCompleted();
    void UpdateConcurrencyLimit(int maxConcurrent);
    void CancelAllDownloads();
}
