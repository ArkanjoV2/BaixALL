using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BaixALL.App.Infrastructure;
using BaixALL.App.Models;
using BaixALL.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BaixALL.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IDependencyManager _dependencyManager;
    private readonly IUpdateService _updateService;
    private readonly IDownloadService _downloadService;
    private readonly ILoggerService? _logger;

    [ObservableProperty]
    private string _downloadFolder = string.Empty;

    [ObservableProperty]
    private int _maxConcurrentDownloads = 1;

    [ObservableProperty]
    private string _theme = "System";

    [ObservableProperty]
    private bool _autoCheckUpdates = true;

    [ObservableProperty]
    private bool _autoPasteClipboard = true;

    [ObservableProperty]
    private IReadOnlyList<DependencyItem> _dependencies = new List<DependencyItem>();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public string AppVersion => AppConstants.AppVersion;
    public string AppName => AppConstants.AppName;

    public List<int> ConcurrencyOptions { get; } = new() { 1, 2, 3 };
    public List<string> ThemeOptions { get; } = new() { "System", "Dark", "Light" };

    public event Action<string>? ThemeChanged;

    public SettingsViewModel(
        ISettingsService settingsService,
        IDependencyManager dependencyManager,
        IUpdateService updateService,
        IDownloadService downloadService,
        ILoggerService? logger = null)
    {
        _settingsService = settingsService;
        _dependencyManager = dependencyManager;
        _updateService = updateService;
        _downloadService = downloadService;
        _logger = logger;

        LoadFromSettings();
    }

    public void LoadFromSettings()
    {
        var s = _settingsService.Settings;
        DownloadFolder = s.DownloadFolder;
        MaxConcurrentDownloads = s.MaxConcurrentDownloads;
        Theme = s.Theme;
        AutoCheckUpdates = s.AutoCheckUpdates;
        AutoPasteClipboard = s.AutoPasteClipboard;
        Dependencies = _dependencyManager.GetDependencies();
    }

    [RelayCommand]
    private void ChooseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Selecionar pasta de destino padrão para downloads",
            InitialDirectory = Directory.Exists(DownloadFolder) ? DownloadFolder : AppConstants.DefaultDownloadFolder
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            DownloadFolder = dialog.FolderName;
            Save();
        }
    }

    [RelayCommand]
    private void Save()
    {
        var s = _settingsService.Settings;
        s.DownloadFolder = DownloadFolder;
        s.MaxConcurrentDownloads = MaxConcurrentDownloads;
        s.Theme = Theme;
        s.AutoCheckUpdates = AutoCheckUpdates;
        s.AutoPasteClipboard = AutoPasteClipboard;

        _settingsService.SaveSettings();
        _downloadService.UpdateConcurrencyLimit(MaxConcurrentDownloads);
        ThemeChanged?.Invoke(Theme);
        StatusMessage = "Configurações salvas.";
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        IsBusy = true;
        StatusMessage = "Verificando versões mais recentes no GitHub...";

        try
        {
            var ytCheck = await _updateService.CheckYtDlpUpdateAsync();
            var denoCheck = await _updateService.CheckDenoUpdateAsync();

            var msgs = new List<string>();
            if (ytCheck.HasUpdate)
                msgs.Add($"Nova versão de yt-dlp disponível ({ytCheck.LatestVersion}).");
            else
                msgs.Add("yt-dlp está atualizado.");

            if (denoCheck.HasUpdate)
                msgs.Add($"Nova versão de Deno disponível ({denoCheck.LatestVersion}).");
            else
                msgs.Add("Deno está atualizado.");

            StatusMessage = string.Join(" ", msgs);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao checar atualizações: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            await _dependencyManager.CheckDependenciesAsync();
            Dependencies = _dependencyManager.GetDependencies();
        }
    }

    [RelayCommand]
    private async Task UpdateDependencyAsync(string? toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName)) return;

        if (_downloadService.HasActiveDownloads)
        {
            StatusMessage = "Não é possível atualizar ferramentas enquanto houver downloads ativos.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Atualizando {toolName}...";

        try
        {
            var success = await _updateService.UpdateToolAsync(toolName);
            StatusMessage = success
                ? $"{toolName} atualizado com sucesso!"
                : $"Falha ao atualizar {toolName}. Verifique os logs.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao atualizar {toolName}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            await _dependencyManager.CheckDependenciesAsync();
            Dependencies = _dependencyManager.GetDependencies();
        }
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        try
        {
            var path = AppConstants.LogsFolder;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Process.Start("explorer.exe", $"\"{path}\"");
        }
        catch (Exception ex)
        {
            _logger?.Error("Erro ao abrir pasta de logs.", ex);
        }
    }

    [RelayCommand]
    private void OpenToolsFolder()
    {
        try
        {
            var path = AppConstants.ToolsFolder;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Process.Start("explorer.exe", $"\"{path}\"");
        }
        catch (Exception ex)
        {
            _logger?.Error("Erro ao abrir pasta de ferramentas.", ex);
        }
    }
}
