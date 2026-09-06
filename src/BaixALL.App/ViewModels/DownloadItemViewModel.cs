using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using BaixALL.App.Helpers;
using BaixALL.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaixALL.App.ViewModels;

public partial class DownloadItemViewModel : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _thumbnailUrl = string.Empty;

    [ObservableProperty]
    private string _quality = string.Empty;

    [ObservableProperty]
    private string _format = string.Empty;

    [ObservableProperty]
    private string _destinationPath = string.Empty;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _downloadedSizeText = "0 B";

    [ObservableProperty]
    private string _totalSizeText = "--";

    [ObservableProperty]
    private string _speedText = "--";

    [ObservableProperty]
    private string _etaText = "--";

    [ObservableProperty]
    private DownloadStatus _status = DownloadStatus.Queued;

    [ObservableProperty]
    private string _statusMessage = "Na fila de download...";

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _isFailed;

    [ObservableProperty]
    private bool _isCanceled;

    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public DownloadRequest Request { get; init; } = new();

    public event EventHandler? CancelRequested;
    public event EventHandler? RemoveRequested;

    public void UpdateProgress(DownloadProgressReport report)
    {
        Status = report.Status;
        StatusMessage = report.StatusMessage;
        ProgressPercentage = Math.Clamp(report.Percentage, 0, 100);

        DownloadedSizeText = ByteSizeFormatter.Format(report.DownloadedBytes);
        TotalSizeText = report.TotalBytes.HasValue && report.TotalBytes.Value > 0
            ? ByteSizeFormatter.Format(report.TotalBytes.Value)
            : "--";

        SpeedText = ByteSizeFormatter.FormatSpeed(report.SpeedBytesPerSec);
        EtaText = TimeFormatter.FormatEta(report.EtaSeconds);

        UpdateStateBooleans();
    }

    public void MarkCompleted(string finalFilePath)
    {
        DestinationPath = finalFilePath;
        Status = DownloadStatus.Completed;
        StatusMessage = "Download concluído com sucesso!";
        ProgressPercentage = 100;
        SpeedText = "--";
        EtaText = "--";
        UpdateStateBooleans();
    }

    public void MarkError(string errorMessage)
    {
        Status = DownloadStatus.Error;
        StatusMessage = errorMessage;
        SpeedText = "--";
        EtaText = "--";
        UpdateStateBooleans();
    }

    public void MarkCanceled()
    {
        Status = DownloadStatus.Canceled;
        StatusMessage = "Download cancelado pelo usuário.";
        SpeedText = "--";
        EtaText = "--";
        UpdateStateBooleans();
    }

    private void UpdateStateBooleans()
    {
        IsCompleted = Status == DownloadStatus.Completed;
        IsFailed = Status == DownloadStatus.Error;
        IsCanceled = Status == DownloadStatus.Canceled;
        IsActive = Status != DownloadStatus.Completed &&
                   Status != DownloadStatus.Error &&
                   Status != DownloadStatus.Canceled;
    }

    [RelayCommand]
    private void Cancel()
    {
        if (!IsActive) return;
        try
        {
            CancellationTokenSource.Cancel();
        }
        catch { }
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenFile()
    {
        if (string.IsNullOrWhiteSpace(DestinationPath) || !File.Exists(DestinationPath))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DestinationPath,
                UseShellExecute = true
            });
        }
        catch { }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var targetFile = DestinationPath;
        var folder = !string.IsNullOrEmpty(targetFile) && File.Exists(targetFile)
            ? Path.GetDirectoryName(targetFile)
            : Request.DestinationFolder;

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        try
        {
            if (!string.IsNullOrEmpty(targetFile) && File.Exists(targetFile))
            {
                // Abre o explorer selecionando o arquivo baixado
                Process.Start("explorer.exe", $"/select,\"{targetFile}\"");
            }
            else
            {
                Process.Start("explorer.exe", $"\"{folder}\"");
            }
        }
        catch { }
    }

    [RelayCommand]
    private void Remove()
    {
        RemoveRequested?.Invoke(this, EventArgs.Empty);
    }
}
