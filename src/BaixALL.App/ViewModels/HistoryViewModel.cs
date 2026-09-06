using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using BaixALL.App.Models;
using BaixALL.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaixALL.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private readonly ILoggerService? _logger;

    public ObservableCollection<HistoryItem> Items { get; } = new();

    [ObservableProperty]
    private bool _hasItems;

    public HistoryViewModel(IHistoryService historyService, ILoggerService? logger = null)
    {
        _historyService = historyService;
        _logger = logger;

        _historyService.HistoryChanged += (_, _) => ReloadHistory();
        ReloadHistory();
    }

    public void ReloadHistory()
    {
        Items.Clear();
        var history = _historyService.GetHistory();
        foreach (var item in history)
        {
            Items.Add(item);
        }
        HasItems = Items.Count > 0;
    }

    [RelayCommand]
    private void OpenFile(HistoryItem? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.FinalFilePath) || !File.Exists(item.FinalFilePath))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.FinalFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.Error($"Falha ao abrir arquivo: {item.FinalFilePath}", ex);
        }
    }

    [RelayCommand]
    private void OpenFolder(HistoryItem? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.FinalFilePath))
            return;

        try
        {
            if (File.Exists(item.FinalFilePath))
            {
                Process.Start("explorer.exe", $"/select,\"{item.FinalFilePath}\"");
            }
            else
            {
                var dir = Path.GetDirectoryName(item.FinalFilePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", $"\"{dir}\"");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Falha ao abrir pasta de: {item.FinalFilePath}", ex);
        }
    }

    [RelayCommand]
    private void RemoveItem(HistoryItem? item)
    {
        if (item == null) return;
        _historyService.RemoveItem(item.Id);
    }

    [RelayCommand]
    private void ClearAll()
    {
        _historyService.ClearHistory();
    }
}
