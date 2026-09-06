using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BaixALL.App.Helpers;
using BaixALL.App.Infrastructure;
using BaixALL.App.Models;
using BaixALL.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BaixALL.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IYoutubeService _youtubeService;
    private readonly IFormatSelectionService _formatSelectionService;
    private readonly IDownloadService _downloadService;
    private readonly ISettingsService _settingsService;
    private readonly IDependencyManager _dependencyManager;
    private readonly ILoggerService? _logger;

    public SettingsViewModel SettingsVm { get; }
    public HistoryViewModel HistoryVm { get; }
    public DependenciesViewModel DependenciesVm { get; }

    [ObservableProperty]
    private string _urlInput = string.Empty;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private bool _hasVideoInfo;

    [ObservableProperty]
    private VideoInfo? _videoInfo;

    public ObservableCollection<FormatOption> FormatOptions { get; } = new();

    [ObservableProperty]
    private FormatOption? _selectedFormat;

    public ObservableCollection<ContainerOption> ContainerOptions { get; } = new();

    [ObservableProperty]
    private ContainerOption? _selectedContainer;

    public ObservableCollection<AudioFormatOption> AudioFormatOptions { get; } = new();

    [ObservableProperty]
    private AudioFormatOption? _selectedAudioFormat;

    [ObservableProperty]
    private bool _isAudioOnlyMode;

    [ObservableProperty]
    private string _destinationFolder = string.Empty;

    [ObservableProperty]
    private string _statusNotification = string.Empty;

    [ObservableProperty]
    private bool _hasStatusNotification;

    [ObservableProperty]
    private string _statusType = "Info"; // "Info", "Success", "Warning", "Error"

    [ObservableProperty]
    private string _currentTab = "Downloader"; // "Downloader", "Queue", "History", "Settings", "Dependencies"

    [ObservableProperty]
    private bool _dependenciesReady;

    public ObservableCollection<DownloadItemViewModel> QueueItems => _downloadService.QueueItems;

    public int ActiveDownloadsCount => QueueItems.Count(x => x.IsActive);

    public MainViewModel(
        IYoutubeService youtubeService,
        IFormatSelectionService formatSelectionService,
        IDownloadService downloadService,
        ISettingsService settingsService,
        IDependencyManager dependencyManager,
        IUpdateService updateService,
        IHistoryService historyService,
        ILoggerService? logger = null)
    {
        _youtubeService = youtubeService;
        _formatSelectionService = formatSelectionService;
        _downloadService = downloadService;
        _settingsService = settingsService;
        _dependencyManager = dependencyManager;
        _logger = logger;

        SettingsVm = new SettingsViewModel(_settingsService, _dependencyManager, updateService, _downloadService, _logger);
        HistoryVm = new HistoryViewModel(historyService, _logger);
        DependenciesVm = new DependenciesViewModel(_dependencyManager, _logger);

        DependenciesVm.AllReady += (_, _) =>
        {
            DependenciesReady = true;
            if (CurrentTab == "Dependencies")
            {
                CurrentTab = "Downloader";
            }
        };

        DestinationFolder = _settingsService.Settings.DownloadFolder;

        // Inicializa containers
        foreach (var c in ContainerOption.DefaultOptions)
            ContainerOptions.Add(c);
        SelectedContainer = ContainerOptions.FirstOrDefault();

        // Inicializa formatos de áudio
        foreach (var a in AudioFormatOption.DefaultOptions)
            AudioFormatOptions.Add(a);
        SelectedAudioFormat = AudioFormatOptions.FirstOrDefault();

        _downloadService.QueueItems.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ActiveDownloadsCount));
        };
    }

    public async Task InitializeStartupAsync()
    {
        _logger?.Info("Inicializando BaixALL e verificando dependências...");
        var ready = await _dependencyManager.CheckDependenciesAsync();
        DependenciesReady = ready;

        if (!ready)
        {
            CurrentTab = "Dependencies";
            ShowNotification("Componentes necessários ausentes. Configure as ferramentas para prosseguir.", "Warning");
        }
        else
        {
            // Tenta colar da área de transferência se habilitado
            if (_settingsService.Settings.AutoPasteClipboard)
            {
                TryAutoPasteFromClipboard();
            }
        }
    }

    public void TryAutoPasteFromClipboard()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText()?.Trim();
                if (!string.IsNullOrWhiteSpace(text) && UrlValidator.IsValidYouTubeUrl(text) && string.IsNullOrWhiteSpace(UrlInput))
                {
                    UrlInput = text;
                    _ = AnalyzeAsync();
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task AnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(UrlInput))
        {
            ShowNotification("Por favor, cole a URL de um vídeo do YouTube.", "Warning");
            return;
        }

        if (!UrlValidator.IsValidYouTubeUrl(UrlInput))
        {
            ShowNotification("A URL informada não é válida para vídeos do YouTube.", "Error");
            return;
        }

        if (!DependenciesReady && !_dependencyManager.AreAllDependenciesInstalled())
        {
            CurrentTab = "Dependencies";
            ShowNotification("Por favor, instale as ferramentas necessárias antes de analisar vídeos.", "Warning");
            return;
        }

        IsAnalyzing = true;
        HasVideoInfo = false;
        ClearNotification();

        try
        {
            var info = await _youtubeService.AnalyzeVideoAsync(UrlInput);
            VideoInfo = info;

            // Preenche opções de formato
            FormatOptions.Clear();
            var options = _formatSelectionService.BuildFormatOptions(info);
            foreach (var opt in options)
            {
                FormatOptions.Add(opt);
            }

            SelectedFormat = FormatOptions.FirstOrDefault(x => x.IsBestQuality) ?? FormatOptions.FirstOrDefault();
            HasVideoInfo = true;

            ShowNotification($"Vídeo encontrado: {info.Title}", "Success");
        }
        catch (Exception ex)
        {
            _logger?.Error($"Erro ao analisar '{UrlInput}': {ex.Message}", ex);
            ShowNotification(ex.Message, "Error");
            HasVideoInfo = false;
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private void PasteAndAnalyze()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText()?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    UrlInput = text;
                    _ = AnalyzeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            ShowNotification("Falha ao acessar área de transferência.", "Warning");
            _logger?.Warning($"Erro ao ler área de transferência: {ex.Message}");
        }
    }

    [RelayCommand]
    private void StartDownload()
    {
        if (VideoInfo == null || SelectedFormat == null)
        {
            ShowNotification("Por favor, analise um vídeo antes de iniciar o download.", "Warning");
            return;
        }

        if (string.IsNullOrWhiteSpace(DestinationFolder))
        {
            DestinationFolder = AppConstants.DefaultDownloadFolder;
        }

        FileHelper.EnsureDirectoryExists(DestinationFolder);

        var isAudioOnly = IsAudioOnlyMode || SelectedFormat.IsAudioOnly;

        var request = new DownloadRequest
        {
            VideoUrl = VideoInfo.OriginalUrl,
            VideoTitle = VideoInfo.Title,
            DestinationFolder = DestinationFolder,
            Format = SelectedFormat,
            Container = SelectedContainer ?? ContainerOption.DefaultOptions[0],
            AudioFormat = SelectedAudioFormat ?? AudioFormatOption.DefaultOptions[0],
            IsAudioOnly = isAudioOnly
        };

        _downloadService.EnqueueDownload(request, VideoInfo.ThumbnailUrl);

        ShowNotification($"Download adicionado à fila: '{VideoInfo.Title}'", "Success");
        CurrentTab = "Queue";
    }

    [RelayCommand]
    private void ChooseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Selecionar pasta para salvar os vídeos",
            InitialDirectory = Directory.Exists(DestinationFolder) ? DestinationFolder : AppConstants.DefaultDownloadFolder
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            DestinationFolder = dialog.FolderName;
            _settingsService.Settings.DownloadFolder = DestinationFolder;
            _settingsService.SaveSettings();
        }
    }

    [RelayCommand]
    private void SwitchTab(string? tab)
    {
        if (!string.IsNullOrWhiteSpace(tab))
        {
            CurrentTab = tab;
            ClearNotification();
        }
    }

    [RelayCommand]
    private void ClearCompletedQueue()
    {
        _downloadService.ClearCompleted();
    }

    public void ShowNotification(string message, string type = "Info")
    {
        StatusNotification = message;
        StatusType = type;
        HasStatusNotification = true;
    }

    public void ClearNotification()
    {
        HasStatusNotification = false;
        StatusNotification = string.Empty;
    }
}
