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

    [ObservableProperty]
    private double _overallProgressPercentage;

    public double ItemsProgressPercentage => OverallProgressPercentage;

    public string ProgressSummaryText => TotalItems > 0
        ? (IsActive
            ? $"{CompletedCount} de {TotalItems} vídeos concluídos ({(int)OverallProgressPercentage}% por itens)"
            : $"{CompletedCount} de {TotalItems} vídeos concluídos ({(int)OverallProgressPercentage}%)")
        : "0 de 0 vídeos concluídos";

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
        var currentCompleted = list.Count(x => x.IsCompleted);
        var activeItems = list.Where(x => x.IsActive && x.Status != DownloadStatus.Queued).ToList();
        var queued = list.Count(x => x.Status == DownloadStatus.Queued);
        var failed = list.Count(x => x.IsFailed);
        var canceled = list.Count(x => x.IsCanceled);

        // Se itens concluídos foram limpos da fila visual via "Limpar Concluídos",
        // preserva a contagem acumulada para que a soma sempre bata com TotalItems.
        var missingCleared = Math.Max(0, TotalItems - (activeItems.Count + queued + failed + canceled + currentCompleted));
        CompletedCount = currentCompleted + missingCleared;
        ActiveCount = activeItems.Count;
        QueuedCount = queued;
        FailedCount = failed;
        CanceledCount = canceled;

        // Progresso por itens ponderado: itens 100% concluídos + fração dos itens ativos em andamento
        if (TotalItems > 0)
        {
            double activeFractionSum = activeItems.Sum(x => Math.Clamp(x.ProgressPercentage, 0, 100));
            double completedSum = CompletedCount * 100.0;
            OverallProgressPercentage = Math.Clamp((completedSum + activeFractionSum) / TotalItems, 0, 100);
        }
        else
        {
            OverallProgressPercentage = 0;
        }

        OnPropertyChanged(nameof(ItemsProgressPercentage));
        OnPropertyChanged(nameof(ProgressSummaryText));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(BatchStatusText));
        OnPropertyChanged(nameof(HasFailures));
        OnPropertyChanged(nameof(HasCanceled));
    }
}
