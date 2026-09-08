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
    [NotifyPropertyChangedFor(nameof(CanAnalyze))]
    private bool _isAnalyzing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoActiveContent))]
    private bool _hasVideoInfo;

    [ObservableProperty]
    private VideoInfo? _videoInfo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoActiveContent))]
    private bool _hasPlaylistInfo;

    [ObservableProperty]
    private PlaylistInfo? _playlistInfo;

    public bool HasNoActiveContent => !HasVideoInfo && !HasPlaylistInfo;

    public ObservableCollection<FormatOption> BatchFormatOptions { get; } = new();

    [ObservableProperty]
    private FormatOption? _selectedBatchFormat;

    [ObservableProperty]
    private bool _isBatchAudioOnlyMode;

    [ObservableProperty]
    private bool _createPlaylistSubfolder = true;

    [ObservableProperty]
    private bool _isHybridUrlDetected;

    [ObservableProperty]
    private string _hybridPlaylistNotice = string.Empty;

    public ObservableCollection<PlaylistItemInfo> PlaylistItems { get; } = new();

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
    [NotifyPropertyChangedFor(nameof(CanAnalyze))]
    [NotifyPropertyChangedFor(nameof(ShowSetupPrompt))]
    private bool _dependenciesReady;

    [ObservableProperty]
    private bool _isInitialSetupVisible;

    [ObservableProperty]
    private bool _canRetryAnalysis;

    public bool CanAnalyze => DependenciesReady && !IsAnalyzing;
    public bool ShowSetupPrompt => !DependenciesReady;

    public ObservableCollection<DownloadItemViewModel> QueueItems => _downloadService.QueueItems;
    public ObservableCollection<BatchProgressViewModel> ActiveBatches { get; } = new();

    public int ActiveDownloadsCount => QueueItems.Count(x => x.IsActive);

    public bool HasActiveDownloads => _downloadService.HasActiveDownloads;

    private System.Threading.CancellationTokenSource? _analysisCts;

    public void CancelAllDownloads() => _downloadService.CancelAllDownloads();

    public MainViewModel(
        IYoutubeService youtubeService,
        IFormatSelectionService formatSelectionService,
        IDownloadService downloadService,
        ISettingsService settingsService,
        IDependencyManager dependencyManager,
        IUpdateService updateService,
        IHistoryService historyService,
        IDispatcherService? dispatcher = null,
        ILoggerService? logger = null)
    {
        _youtubeService = youtubeService;
        _formatSelectionService = formatSelectionService;
        _downloadService = downloadService;
        _settingsService = settingsService;
        _dependencyManager = dependencyManager;
        _logger = logger;

        SettingsVm = new SettingsViewModel(_settingsService, _dependencyManager, updateService, _downloadService, _logger);
        HistoryVm = new HistoryViewModel(historyService, dispatcher, _logger);
        DependenciesVm = new DependenciesViewModel(_dependencyManager, updateService, _downloadService, _logger);

        DependenciesVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DependenciesViewModel.AreAllInstalled))
            {
                DependenciesReady = DependenciesVm.AreAllInstalled;
            }
        };

        DependenciesVm.AllReady += (_, _) =>
        {
            DependenciesReady = true;
            IsInitialSetupVisible = false;
            ShowNotification("Todas as ferramentas estão prontas! Cole a URL para começar.", "Success");
        };

        DependenciesVm.CloseRequested += (_, _) =>
        {
            IsInitialSetupVisible = false;
            DependenciesReady = _dependencyManager.AreAllDependenciesInstalled();
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

        foreach (var item in _downloadService.QueueItems)
        {
            item.PropertyChanged += OnQueueItemPropertyChanged;
        }
        UpdateBatchProgress();

        _downloadService.QueueItems.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(ActiveDownloadsCount));
            if (e.NewItems != null)
            {
                foreach (DownloadItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += OnQueueItemPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (DownloadItemViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= OnQueueItemPropertyChanged;
                }
            }
            UpdateBatchProgress();
        };
    }

    public async Task InitializeStartupAsync()
    {
        _logger?.Info("Inicializando BaixALL e verificando dependências...");
        await DependenciesVm.CheckAsync();
        DependenciesReady = DependenciesVm.AreAllInstalled;

        if (!DependenciesReady)
        {
            // Não remove o conteúdo da Home! Mantém a Home visível com banner e exibe o modal
            CurrentTab = "Downloader";
            IsInitialSetupVisible = true;
            ShowNotification("Componentes necessários ausentes. O BaixALL pode instalá-los automaticamente.", "Warning");
        }
        else
        {
            IsInitialSetupVisible = false;
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
            ShowNotification("Por favor, cole a URL de um vídeo ou playlist do YouTube.", "Warning");
            return;
        }

        if (!UrlValidator.IsValidYouTubeUrl(UrlInput))
        {
            ShowNotification("A URL informada não é válida para conteúdos do YouTube.", "Error");
            return;
        }

        if (!DependenciesReady && !_dependencyManager.AreAllDependenciesInstalled())
        {
            IsInitialSetupVisible = true;
            ShowNotification("Instale os componentes necessários antes de analisar conteúdos.", "Warning");
            return;
        }

        // Se for URL pura de playlist, analisa a playlist diretamente
        if (UrlValidator.IsPurePlaylistUrl(UrlInput))
        {
            await AnalyzePlaylistInternalAsync(UrlInput);
            return;
        }

        // Se for URL híbrida (vídeo com lista associada), ativa aviso com opção de carregar a playlist completa
        IsHybridUrlDetected = UrlValidator.IsHybridUrl(UrlInput);
        HybridPlaylistNotice = IsHybridUrlDetected ? "Esta URL pertence a uma playlist do YouTube." : string.Empty;

        IsAnalyzing = true;
        HasVideoInfo = false;
        HasPlaylistInfo = false;
        CanRetryAnalysis = false;
        ClearNotification();

        _analysisCts?.Cancel();
        _analysisCts?.Dispose();
        _analysisCts = new System.Threading.CancellationTokenSource();
        var ct = _analysisCts.Token;

        try
        {
            var info = await _youtubeService.AnalyzeVideoAsync(UrlInput, ct);
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
        catch (OperationCanceledException)
        {
            ShowNotification("Análise cancelada pelo usuário.", "Info");
            HasVideoInfo = false;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Erro ao analisar '{UrlInput}' no método AnalyzeAsync: {ex.GetType().FullName}: {ex.Message}", ex);

            CanRetryAnalysis = true;
            string friendlyMessage = ex switch
            {
                ArgumentException argEx => argEx.Message,
                _ => "Não foi possível analisar este vídeo. Algumas informações retornadas pelo YouTube não puderam ser processadas."
            };

            ShowNotification(friendlyMessage, "Error");
            HasVideoInfo = false;
        }
        finally
        {
            IsAnalyzing = false;
            _analysisCts?.Dispose();
            _analysisCts = null;
        }
    }

    [RelayCommand]
    public void CancelAnalysis()
    {
        if (IsAnalyzing && _analysisCts != null)
        {
            try
            {
                _analysisCts.Cancel();
                ShowNotification("Cancelando análise...", "Info");
            }
            catch { }
        }
    }

    public async Task AnalyzePlaylistInternalAsync(string url)
    {
        IsAnalyzing = true;
        HasVideoInfo = false;
        HasPlaylistInfo = false;
        CanRetryAnalysis = false;
        ClearNotification();

        _analysisCts?.Cancel();
        _analysisCts?.Dispose();
        _analysisCts = new System.Threading.CancellationTokenSource();
        var ct = _analysisCts.Token;

        try
        {
            var info = await _youtubeService.AnalyzePlaylistAsync(url, ct);
            PlaylistInfo = info;

            BatchFormatOptions.Clear();
            var batchOpts = _formatSelectionService.BuildBatchFormatOptions();
            foreach (var opt in batchOpts)
            {
                BatchFormatOptions.Add(opt);
            }
            SelectedBatchFormat = BatchFormatOptions.FirstOrDefault(x => x.IsBestQuality) ?? BatchFormatOptions.FirstOrDefault();

            PlaylistItems.Clear();
            foreach (var item in info.Items)
            {
                item.PropertyChanged += OnPlaylistItemPropertyChanged;
                PlaylistItems.Add(item);
            }

            info.UpdateCounts();
            HasPlaylistInfo = true;

            ShowNotification($"Playlist encontrada: {info.Title} ({info.TotalVideosCount} vídeos)", "Success");
        }
        catch (OperationCanceledException)
        {
            ShowNotification("Análise da playlist cancelada pelo usuário.", "Info");
            HasPlaylistInfo = false;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Erro ao analisar playlist '{url}': {ex.GetType().FullName}: {ex.Message}", ex);

            CanRetryAnalysis = true;
            string friendlyMessage = ex switch
            {
                ArgumentException argEx => argEx.Message,
                _ => "Não foi possível carregar a playlist. Verifique se a playlist é pública ou não listada."
            };

            ShowNotification(friendlyMessage, "Error");
            HasPlaylistInfo = false;
        }
        finally
        {
            IsAnalyzing = false;
            _analysisCts?.Dispose();
            _analysisCts = null;
        }
    }

    private void OnPlaylistItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaylistItemInfo.IsSelected))
        {
            PlaylistInfo?.UpdateCounts();
        }
    }

    [RelayCommand]
    private void SelectAllPlaylistItems()
    {
        if (PlaylistInfo == null) return;
        foreach (var item in PlaylistInfo.Items)
        {
            if (item.IsAvailable) item.IsSelected = true;
        }
        PlaylistInfo.UpdateCounts();
    }

    [RelayCommand]
    private void DeselectAllPlaylistItems()
    {
        if (PlaylistInfo == null) return;
        foreach (var item in PlaylistInfo.Items)
        {
            item.IsSelected = false;
        }
        PlaylistInfo.UpdateCounts();
    }

    [RelayCommand]
    private void InvertPlaylistSelection()
    {
        if (PlaylistInfo == null) return;
        foreach (var item in PlaylistInfo.Items)
        {
            if (item.IsAvailable) item.IsSelected = !item.IsSelected;
        }
        PlaylistInfo.UpdateCounts();
    }

    [RelayCommand]
    private void ClearPlaylist()
    {
        HasPlaylistInfo = false;
        PlaylistInfo = null;
        PlaylistItems.Clear();
        IsHybridUrlDetected = false;
        ClearNotification();
    }

    [RelayCommand]
    private async Task LoadFullPlaylistFromHybridAsync()
    {
        if (!string.IsNullOrWhiteSpace(UrlInput))
        {
            await AnalyzePlaylistInternalAsync(UrlInput);
        }
    }

    [RelayCommand]
    private void EnqueueSelectedPlaylistItems()
    {
        if (PlaylistInfo == null || SelectedBatchFormat == null)
        {
            ShowNotification("Analise uma playlist antes de iniciar o download em lote.", "Warning");
            return;
        }

        var selected = PlaylistInfo.Items.Where(x => x.IsSelected && x.IsAvailable).ToList();
        if (selected.Count == 0)
        {
            ShowNotification("Nenhum vídeo disponível foi selecionado na playlist.", "Warning");
            return;
        }

        // Deduplicação interna na seleção de itens da playlist (por URL)
        var distinctSelected = selected
            .GroupBy(x => x.VideoUrl, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        // Evita adicionar vídeos que já estão ativamente baixando ou na fila
        var activeUrls = new System.Collections.Generic.HashSet<string>(
            _downloadService.QueueItems
                .Where(x => x.IsActive)
                .Select(x => x.Request.VideoUrl),
            StringComparer.OrdinalIgnoreCase);

        var itemsToEnqueue = distinctSelected.Where(x => !activeUrls.Contains(x.VideoUrl)).ToList();
        int skippedDuplicates = distinctSelected.Count - itemsToEnqueue.Count;

        if (itemsToEnqueue.Count == 0)
        {
            ShowNotification("Todos os vídeos selecionados já estão ativos ou aguardando na fila.", "Warning");
            CurrentTab = "Queue";
            return;
        }

        var baseFolder = string.IsNullOrWhiteSpace(DestinationFolder)
            ? AppConstants.DefaultDownloadFolder
            : DestinationFolder;

        var targetFolder = CreatePlaylistSubfolder
            ? Path.Combine(baseFolder, FileHelper.SanitizeFileName(PlaylistInfo.Title))
            : baseFolder;

        FileHelper.EnsureDirectoryExists(targetFolder);

        var isAudioOnly = IsBatchAudioOnlyMode || SelectedBatchFormat.IsAudioOnly;
        var batchId = Guid.NewGuid();
        int total = itemsToEnqueue.Count;

        for (int i = 0; i < itemsToEnqueue.Count; i++)
        {
            var item = itemsToEnqueue[i];
            var request = new DownloadRequest
            {
                VideoUrl = item.VideoUrl,
                VideoTitle = item.Title,
                DestinationFolder = targetFolder,
                Format = SelectedBatchFormat,
                Container = SelectedContainer ?? ContainerOption.DefaultOptions[0],
                AudioFormat = SelectedAudioFormat ?? AudioFormatOption.DefaultOptions[0],
                IsAudioOnly = isAudioOnly,
                BatchId = batchId,
                BatchTitle = PlaylistInfo.Title,
                BatchIndex = i + 1,
                BatchTotal = total
            };

            _downloadService.EnqueueDownload(request, item.ThumbnailUrl);
        }

        var notifMsg = skippedDuplicates > 0
            ? $"Lote adicionado: {total} vídeo(s) da playlist '{PlaylistInfo.Title}' ({skippedDuplicates} duplicado(s) já na fila ignorados)."
            : $"Lote adicionado à fila: {total} vídeo(s) da playlist '{PlaylistInfo.Title}'";

        ShowNotification(notifMsg, "Success");
        CurrentTab = "Queue";
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

        // Verifica duplicata na fila ativa
        if (_downloadService.QueueItems.Any(x => x.IsActive && string.Equals(x.Request.VideoUrl, VideoInfo.OriginalUrl, StringComparison.OrdinalIgnoreCase)))
        {
            ShowNotification("Este vídeo já está ativo ou na fila de downloads.", "Warning");
            CurrentTab = "Queue";
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
    public void CancelBatch(Guid batchId)
    {
        _downloadService.CancelBatch(batchId);
        var batch = ActiveBatches.FirstOrDefault(b => b.BatchId == batchId);
        var title = batch?.BatchTitle ?? "Lote";
        ShowNotification($"Download do lote '{title}' cancelado.", "Info");
        UpdateBatchProgress();
    }

    private void OnQueueItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DownloadItemViewModel.Status) ||
            e.PropertyName == nameof(DownloadItemViewModel.IsActive) ||
            e.PropertyName == nameof(DownloadItemViewModel.IsCompleted) ||
            e.PropertyName == nameof(DownloadItemViewModel.IsFailed) ||
            e.PropertyName == nameof(DownloadItemViewModel.IsCanceled))
        {
            UpdateBatchProgress();
        }
    }

    public void UpdateBatchProgress()
    {
        var batchGroups = QueueItems
            .Where(x => x.IsBatchItem && x.BatchId.HasValue)
            .GroupBy(x => x.BatchId!.Value)
            .ToList();

        var activeBatchIds = batchGroups.Select(g => g.Key).ToHashSet();

        for (int i = ActiveBatches.Count - 1; i >= 0; i--)
        {
            if (!activeBatchIds.Contains(ActiveBatches[i].BatchId))
            {
                ActiveBatches.RemoveAt(i);
            }
        }

        foreach (var group in batchGroups)
        {
            var batchId = group.Key;
            var firstItem = group.First();
            var batchVm = ActiveBatches.FirstOrDefault(b => b.BatchId == batchId);

            if (batchVm == null)
            {
                batchVm = new BatchProgressViewModel
                {
                    BatchId = batchId,
                    BatchTitle = firstItem.BatchTitle ?? "Lote de Vídeos",
                    TotalItems = firstItem.BatchTotal.GetValueOrDefault(group.Count())
                };
                batchVm.CancelRequested += (_, id) => CancelBatch(id);
                ActiveBatches.Add(batchVm);
            }

            batchVm.UpdateCounts(group);
        }
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
            if (tab == "Dependencies")
            {
                _ = DependenciesVm.CheckAsync();
            }
        }
    }

    [RelayCommand]
    private void ClearCompletedQueue()
    {
        _downloadService.ClearCompleted();
    }

    [RelayCommand]
    private void SetupNow()
    {
        IsInitialSetupVisible = true;
    }

    [RelayCommand]
    private void CloseSetup()
    {
        IsInitialSetupVisible = false;
        DependenciesReady = _dependencyManager.AreAllDependenciesInstalled();
    }

    [RelayCommand]
    private void OpenTools()
    {
        CurrentTab = "Dependencies";
        ClearNotification();
        _ = DependenciesVm.CheckAsync();
    }

    public void ShowNotification(string message, string type = "Info")
    {
        StatusNotification = message;
        StatusType = type;
        HasStatusNotification = true;
    }

    [RelayCommand]
    public void ClearNotification()
    {
        HasStatusNotification = false;
        StatusNotification = string.Empty;
        CanRetryAnalysis = false;
    }
}
