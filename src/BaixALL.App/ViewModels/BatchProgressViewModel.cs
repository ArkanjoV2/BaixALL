using System;
using System.Collections.Generic;
using System.Linq;
using BaixALL.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaixALL.App.ViewModels;

public partial class BatchProgressViewModel : ObservableObject
{
    public Guid BatchId { get; init; }
    public string BatchTitle { get; init; } = string.Empty;
    public int TotalItems { get; init; }

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _queuedCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private int _canceledCount;

    public double ItemsProgressPercentage => TotalItems > 0 ? Math.Clamp(((double)CompletedCount / TotalItems) * 100.0, 0, 100) : 0.0;

    public string ProgressSummaryText => $"{CompletedCount} de {TotalItems} vídeos concluídos ({(int)ItemsProgressPercentage}%)";

    public bool IsActive => (ActiveCount + QueuedCount) > 0;
    public bool HasFailures => FailedCount > 0;
    public bool HasCanceled => CanceledCount > 0;

    public string BatchStatusText
    {
        get
        {
            if (IsActive) return "Em andamento";
            if (CompletedCount == TotalItems && TotalItems > 0) return "Concluído";
            if (CanceledCount == TotalItems && TotalItems > 0) return "Cancelado";
            if (FailedCount == TotalItems && TotalItems > 0) return "Falha";
            return "Finalizado";
        }
    }

    public event EventHandler<Guid>? CancelRequested;

    [RelayCommand]
    private void Cancel()
    {
        CancelRequested?.Invoke(this, BatchId);
    }

    public void UpdateCounts(IEnumerable<DownloadItemViewModel> batchItems)
    {
        var list = batchItems.ToList();
        CompletedCount = list.Count(x => x.IsCompleted);
        ActiveCount = list.Count(x => x.IsActive && x.Status != DownloadStatus.Queued);
        QueuedCount = list.Count(x => x.Status == DownloadStatus.Queued);
        FailedCount = list.Count(x => x.IsFailed);
        CanceledCount = list.Count(x => x.IsCanceled);

        OnPropertyChanged(nameof(ItemsProgressPercentage));
        OnPropertyChanged(nameof(ProgressSummaryText));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(BatchStatusText));
        OnPropertyChanged(nameof(HasFailures));
        OnPropertyChanged(nameof(HasCanceled));
    }
}
