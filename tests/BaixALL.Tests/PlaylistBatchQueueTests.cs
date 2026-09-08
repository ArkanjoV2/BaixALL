using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Models;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using Xunit;

namespace BaixALL.Tests;

public class PlaylistBatchQueueTests
{
    private class DummyDispatcherService : IDispatcherService
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> callback) => callback();
        public Task InvokeAsync(Action action) { action(); return Task.CompletedTask; }
        public Task<T> InvokeAsync<T>(Func<T> callback) => Task.FromResult(callback());
        public bool CheckAccess() => true;
    }

    private class MockDependencyManager : IDependencyManager
    {
        public IReadOnlyList<DependencyItem> GetDependencies() => new List<DependencyItem>();
        public bool AreAllDependenciesInstalled() => true;
        public Task<bool> CheckDependenciesAsync() => Task.FromResult(true);
        public Task<bool> EnsureAllDependenciesAsync(IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> InstallDependencyByNameAsync(string name, IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateDependencyAsync(string name) => Task.FromResult(true);
        public Task<bool> UpdateDependencyAsync(string name, IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public string GetYtDlpPath() => "yt-dlp.exe";
        public string GetFFmpegPath() => "ffmpeg.exe";
        public string GetFFprobePath() => "ffprobe.exe";
        public string GetDenoPath() => "deno.exe";
    }

    private class MockYtDlpService : IYtDlpService
    {
        public TaskCompletionSource<bool>? DownloadBlocker { get; set; }

        public async Task<string> DownloadAsync(DownloadRequest request, IProgress<DownloadProgressReport> progress, CancellationToken ct = default)
        {
            if (DownloadBlocker != null)
            {
                await DownloadBlocker.Task.WaitAsync(ct);
            }
            var filePath = Path.Combine(request.DestinationFolder, $"{request.VideoTitle}.mp4");
            try
            {
                Directory.CreateDirectory(request.DestinationFolder);
                if (!File.Exists(filePath))
                    File.WriteAllText(filePath, "dummy content");
            }
            catch { }
            return filePath;
        }

        public Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default)
        {
            var json = """
            {
                "id": "dQw4w9WgXcQ",
                "title": "Video de Teste",
                "uploader": "Canal Teste",
                "duration": 180,
                "formats": [
                    { "format_id": "137", "vcodec": "avc1", "acodec": "none", "height": 1080, "fps": 30, "tbr": 4000 },
                    { "format_id": "140", "vcodec": "none", "acodec": "mp4a.40.2", "abr": 128 }
                ]
            }
            """;
            return Task.FromResult(JsonDocument.Parse(json));
        }

        public Task<JsonDocument> GetPlaylistMetadataJsonAsync(string playlistUrl, CancellationToken ct = default)
        {
            var json = """
            {
                "_type": "playlist",
                "id": "PLtest123",
                "title": "Playlist do Curso",
                "uploader": "Professor X",
                "entries": [
                    { "id": "12345678901", "title": "Aula 01", "duration": 120, "uploader": "Professor X", "url": "https://www.youtube.com/watch?v=12345678901" },
                    { "id": "12345678902", "title": "Aula 02", "duration": 240, "uploader": "Professor X", "url": "https://www.youtube.com/watch?v=12345678902" },
                    { "id": "12345678903", "title": "[Private video]", "duration": 0, "uploader": "Professor X", "url": "https://www.youtube.com/watch?v=12345678903" }
                ]
            }
            """;
            return Task.FromResult(JsonDocument.Parse(json));
        }
    }

    private class MockSettingsService : ISettingsService
    {
        public AppSettings Settings { get; } = new AppSettings { DownloadFolder = Path.GetTempPath(), MaxConcurrentDownloads = 3 };
        public void LoadSettings() { }
        public void SaveSettings() { }
        public void ReloadSettings() { }
    }

    private class MockUpdateService : IUpdateService
    {
        public Task<UpdateCheckResult> CheckYtDlpUpdateAsync(CancellationToken ct = default) =>
            Task.FromResult(new UpdateCheckResult("yt-dlp", "1.0", "1.0", false, ""));
        public Task<UpdateCheckResult> CheckDenoUpdateAsync(CancellationToken ct = default) =>
            Task.FromResult(new UpdateCheckResult("deno", "1.0", "1.0", false, ""));
        public Task<bool> UpdateToolAsync(string toolName, CancellationToken ct = default) =>
            Task.FromResult(true);
    }

    [Fact]
    public void PlaylistInfo_SelectionLogic_ShouldUpdateCountsAndSelectionSummary()
    {
        var item1 = new PlaylistItemInfo { Id = "1", Title = "V1", DurationSeconds = 60, IsAvailable = true, IsSelected = true };
        var item2 = new PlaylistItemInfo { Id = "2", Title = "V2", DurationSeconds = 120, IsAvailable = true, IsSelected = true };
        var item3 = new PlaylistItemInfo { Id = "3", Title = "V3 (Privado)", DurationSeconds = 0, IsAvailable = false, IsSelected = false };

        var playlist = new PlaylistInfo
        {
            Id = "PL1",
            Title = "Minha Playlist",
            TotalVideosCount = 3,
            Items = new List<PlaylistItemInfo> { item1, item2, item3 }
        };

        playlist.UpdateCounts();
        Assert.Equal(2, playlist.SelectedVideosCount);
        Assert.Equal("2 de 3 selecionados", playlist.SelectionSummary);
        Assert.Equal("3m 0s", playlist.FormattedTotalDuration);

        // Desmarcar item 1
        item1.IsSelected = false;
        playlist.UpdateCounts();
        Assert.Equal(1, playlist.SelectedVideosCount);
        Assert.Equal("1 de 3 selecionados", playlist.SelectionSummary);

        // Inverter seleção (apenas itens disponíveis)
        foreach (var it in playlist.Items)
        {
            if (it.IsAvailable) it.IsSelected = !it.IsSelected;
        }
        playlist.UpdateCounts();
        Assert.True(item1.IsSelected);
        Assert.False(item2.IsSelected);
        Assert.False(item3.IsSelected);
        Assert.Equal(1, playlist.SelectedVideosCount);
    }

    [Fact]
    public void DownloadItemViewModel_BatchProperties_ShouldExposeCorrectFormat()
    {
        var batchId = Guid.NewGuid();
        var request = new DownloadRequest
        {
            VideoTitle = "Capítulo 5",
            BatchId = batchId,
            BatchTitle = "Curso React",
            BatchIndex = 5,
            BatchTotal = 20
        };

        var item = new DownloadItemViewModel
        {
            Title = request.VideoTitle,
            Request = request
        };

        Assert.True(item.IsBatchItem);
        Assert.Equal(batchId, item.BatchId);
        Assert.Equal("Curso React", item.BatchTitle);
        Assert.Equal(5, item.BatchIndex);
        Assert.Equal(20, item.BatchTotal);
        Assert.Equal("[05/20]", item.BatchBadgeText);
    }

    [Fact]
    public async Task MainViewModel_PurePlaylistUrl_AnalyzesPlaylistSuccessfully()
    {
        var depMgr = new MockDependencyManager();
        var ytDlp = new MockYtDlpService();
        var fmtSvc = new FormatSelectionService();
        var ytSvc = new YoutubeService(ytDlp, fmtSvc);
        var settingsSvc = new MockSettingsService();
        var histSvc = new HistoryService(historyFilePath: Path.Combine(Path.GetTempPath(), $"hist_{Guid.NewGuid():N}.json"));
        var dlSvc = new DownloadService(ytDlp, histSvc, settingsSvc, new DummyDispatcherService());
        var updateSvc = new MockUpdateService();

        var vm = new MainViewModel(
            ytSvc,
            fmtSvc,
            dlSvc,
            settingsSvc,
            depMgr,
            updateSvc,
            histSvc,
            new DummyDispatcherService());

        vm.DependenciesReady = true;
        vm.UrlInput = "https://www.youtube.com/playlist?list=PLtest123";

        await vm.AnalyzeAsync();

        Assert.True(vm.HasPlaylistInfo);
        Assert.False(vm.HasVideoInfo);
        Assert.NotNull(vm.PlaylistInfo);
        Assert.Equal("Playlist do Curso", vm.PlaylistInfo.Title);
        Assert.Equal(3, vm.PlaylistInfo.TotalVideosCount);
        // Aula 01 e Aula 02 são selecionadas por padrão (Aula 03 é privada e desmarcada)
        Assert.Equal(2, vm.PlaylistInfo.SelectedVideosCount);
        Assert.NotEmpty(vm.BatchFormatOptions);
        Assert.NotNull(vm.SelectedBatchFormat);
    }

    [Fact]
    public async Task MainViewModel_EnqueueSelectedPlaylistItems_CreatesBatchDownloads()
    {
        var depMgr = new MockDependencyManager();
        var ytDlp = new MockYtDlpService();
        var fmtSvc = new FormatSelectionService();
        var ytSvc = new YoutubeService(ytDlp, fmtSvc);
        var settingsSvc = new MockSettingsService();
        var histSvc = new HistoryService(historyFilePath: Path.Combine(Path.GetTempPath(), $"hist_{Guid.NewGuid():N}.json"));
        var dlSvc = new DownloadService(ytDlp, histSvc, settingsSvc, new DummyDispatcherService());
        var updateSvc = new MockUpdateService();

        var vm = new MainViewModel(
            ytSvc,
            fmtSvc,
            dlSvc,
            settingsSvc,
            depMgr,
            updateSvc,
            histSvc,
            new DummyDispatcherService());

        vm.DependenciesReady = true;
        vm.UrlInput = "https://www.youtube.com/playlist?list=PLtest123";

        await vm.AnalyzeAsync();
        Assert.True(vm.HasPlaylistInfo);

        // Desmarca tudo e seleciona apenas o primeiro item
        vm.DeselectAllPlaylistItemsCommand.Execute(null);
        Assert.Equal(0, vm.PlaylistInfo!.SelectedVideosCount);

        vm.PlaylistItems[0].IsSelected = true;
        vm.PlaylistInfo.UpdateCounts();
        Assert.Equal(1, vm.PlaylistInfo.SelectedVideosCount);

        vm.CreatePlaylistSubfolder = true;
        vm.EnqueueSelectedPlaylistItemsCommand.Execute(null);

        // Fila deve ter 1 item adicionado com os metadados do lote
        Assert.Single(dlSvc.QueueItems);
        var enqueued = dlSvc.QueueItems[0];
        Assert.True(enqueued.IsBatchItem);
        Assert.Equal("Playlist do Curso", enqueued.BatchTitle);
        Assert.Equal(1, enqueued.BatchIndex);
        Assert.Equal(1, enqueued.BatchTotal);
        Assert.Contains("Playlist do Curso", enqueued.Request.DestinationFolder);
    }

    [Fact]
    public void DownloadService_CancelBatch_CancelsOnlyBatchItems()
    {
        var depMgr = new MockDependencyManager();
        var ytDlp = new MockYtDlpService { DownloadBlocker = new TaskCompletionSource<bool>() };
        var settingsSvc = new MockSettingsService();
        var histSvc = new HistoryService(historyFilePath: Path.Combine(Path.GetTempPath(), $"hist_{Guid.NewGuid():N}.json"));
        var dlSvc = new DownloadService(ytDlp, histSvc, settingsSvc, new DummyDispatcherService());

        var batchId = Guid.NewGuid();

        // Enqueue item 1 (Batch)
        var item1 = dlSvc.EnqueueDownload(new DownloadRequest
        {
            VideoTitle = "Batch 1",
            DestinationFolder = Path.GetTempPath(),
            BatchId = batchId,
            BatchTitle = "Playlist X",
            BatchIndex = 1,
            BatchTotal = 2
        }, "thumb1");

        // Enqueue item 2 (Batch)
        var item2 = dlSvc.EnqueueDownload(new DownloadRequest
        {
            VideoTitle = "Batch 2",
            DestinationFolder = Path.GetTempPath(),
            BatchId = batchId,
            BatchTitle = "Playlist X",
            BatchIndex = 2,
            BatchTotal = 2
        }, "thumb2");

        // Enqueue item 3 (Single video, no batch)
        var item3 = dlSvc.EnqueueDownload(new DownloadRequest
        {
            VideoTitle = "Single Video",
            DestinationFolder = Path.GetTempPath()
        }, "thumb3");

        Assert.Equal(3, dlSvc.QueueItems.Count);

        // Cancelar apenas o lote
        dlSvc.CancelBatch(batchId);

        Assert.True(item1.CancellationTokenSource.IsCancellationRequested);
        Assert.True(item2.CancellationTokenSource.IsCancellationRequested);
        Assert.False(item3.CancellationTokenSource.IsCancellationRequested);

        ytDlp.DownloadBlocker.SetResult(true);
    }

    [Fact]
    public void DownloadService_CancelSingleItemInBatch_DoesNotAffectOtherBatchItems()
    {
        var depMgr = new MockDependencyManager();
        var ytDlp = new MockYtDlpService { DownloadBlocker = new TaskCompletionSource<bool>() };
        var settingsSvc = new MockSettingsService();
        var histSvc = new HistoryService(historyFilePath: Path.Combine(Path.GetTempPath(), $"hist_{Guid.NewGuid():N}.json"));
        var dlSvc = new DownloadService(ytDlp, histSvc, settingsSvc, new DummyDispatcherService());

        var batchId = Guid.NewGuid();

        var item1 = dlSvc.EnqueueDownload(new DownloadRequest
        {
            VideoTitle = "Batch 1",
            DestinationFolder = Path.GetTempPath(),
            BatchId = batchId,
            BatchIndex = 1,
            BatchTotal = 3
        }, "thumb1");

        var item2 = dlSvc.EnqueueDownload(new DownloadRequest
        {
            VideoTitle = "Batch 2",
            DestinationFolder = Path.GetTempPath(),
            BatchId = batchId,
            BatchIndex = 2,
            BatchTotal = 3
        }, "thumb2");

        var item3 = dlSvc.EnqueueDownload(new DownloadRequest
        {
            VideoTitle = "Batch 3",
            DestinationFolder = Path.GetTempPath(),
            BatchId = batchId,
            BatchIndex = 3,
            BatchTotal = 3
        }, "thumb3");

        // Cancelar apenas o item 2
        dlSvc.CancelDownload(item2.Id);

        Assert.False(item1.CancellationTokenSource.IsCancellationRequested);
        Assert.True(item2.CancellationTokenSource.IsCancellationRequested);
        Assert.False(item3.CancellationTokenSource.IsCancellationRequested);

        ytDlp.DownloadBlocker.SetResult(true);
    }

    [Fact]
    public void FormatSelectionService_BatchFormatOptions_HasTransparentFallbacks()
    {
        var service = new FormatSelectionService();
        var options = service.BuildBatchFormatOptions();

        Assert.NotEmpty(options);
        var best = options.First(o => o.IsBestQuality);
        Assert.Equal("bestvideo+bestaudio/best", best.FormatSelector);

        var hd1080 = options.FirstOrDefault(o => o.Height == 1080);
        Assert.NotNull(hd1080);
        Assert.Contains("bestvideo[height<=1080]", hd1080.FormatSelector);

        var audioOnly = options.FirstOrDefault(o => o.IsAudioOnly);
        Assert.NotNull(audioOnly);
        Assert.Equal("bestaudio/best", audioOnly.FormatSelector);
    }

    [Fact]
    public void BatchProgressViewModel_CalculationsAndHonestProgress_ShouldBeAccurate()
    {
        var batchId = Guid.NewGuid();
        var batchVm = new BatchProgressViewModel
        {
            BatchId = batchId,
            BatchTitle = "Playlist Honesta",
            TotalItems = 5
        };

        var items = new List<DownloadItemViewModel>
        {
            new() { Status = DownloadStatus.Completed, IsActive = false, IsCompleted = true },
            new() { Status = DownloadStatus.Completed, IsActive = false, IsCompleted = true },
            new() { Status = DownloadStatus.DownloadingVideo, IsActive = true, IsCompleted = false },
            new() { Status = DownloadStatus.Queued, IsActive = true, IsCompleted = false },
            new() { Status = DownloadStatus.Error, IsActive = false, IsFailed = true }
        };

        batchVm.UpdateCounts(items);

        Assert.Equal(2, batchVm.CompletedCount);
        Assert.Equal(1, batchVm.ActiveCount);
        Assert.Equal(1, batchVm.QueuedCount);
        Assert.Equal(1, batchVm.FailedCount);
        Assert.Equal(0, batchVm.CanceledCount);
        Assert.Equal(40.0, batchVm.ItemsProgressPercentage);
        Assert.Equal("2 de 5 vídeos concluídos (40%)", batchVm.ProgressSummaryText);
        Assert.True(batchVm.IsActive);
        Assert.True(batchVm.HasFailures);
        Assert.False(batchVm.HasCanceled);

        // Testa disparo do comando de cancelamento
        Guid? requestedBatchId = null;
        batchVm.CancelRequested += (_, id) => requestedBatchId = id;
        batchVm.CancelCommand.Execute(null);

        Assert.Equal(batchId, requestedBatchId);
    }

    [Fact]
    public async Task MainViewModel_ActiveBatches_TracksQueueBatchesReactively()
    {
        var depMgr = new MockDependencyManager();
        var ytDlp = new MockYtDlpService();
        var fmtSvc = new FormatSelectionService();
        var ytSvc = new YoutubeService(ytDlp, fmtSvc);
        var settingsSvc = new MockSettingsService();
        var histSvc = new HistoryService(historyFilePath: Path.Combine(Path.GetTempPath(), $"hist_{Guid.NewGuid():N}.json"));
        var dlSvc = new DownloadService(ytDlp, histSvc, settingsSvc, new DummyDispatcherService());
        var updateSvc = new MockUpdateService();

        var vm = new MainViewModel(
            ytSvc,
            fmtSvc,
            dlSvc,
            settingsSvc,
            depMgr,
            updateSvc,
            histSvc,
            new DummyDispatcherService());

        vm.DependenciesReady = true;
        vm.UrlInput = "https://www.youtube.com/playlist?list=PLtest123";

        await vm.AnalyzeAsync();
        Assert.True(vm.HasPlaylistInfo);

        // Enfileira
        vm.EnqueueSelectedPlaylistItemsCommand.Execute(null);

        // Deve existir 1 batch ativo em ActiveBatches
        Assert.Single(vm.ActiveBatches);
        var batchVm = vm.ActiveBatches[0];
        Assert.Equal("Playlist do Curso", batchVm.BatchTitle);
        Assert.Equal(2, batchVm.TotalItems);
        Assert.Equal(2, dlSvc.QueueItems.Count);

        // Cancela o lote pelo ViewModel
        vm.CancelBatchCommand.Execute(batchVm.BatchId);
        Assert.All(dlSvc.QueueItems, item => Assert.True(item.CancellationTokenSource.IsCancellationRequested));
    }

    [Fact]
    public async Task MainViewModel_Deduplication_SkipsDuplicateUrlsInPlaylistAndQueue()
    {
        var depMgr = new MockDependencyManager();
        var ytDlp = new MockYtDlpService();
        var fmtSvc = new FormatSelectionService();
        var ytSvc = new YoutubeService(ytDlp, fmtSvc);
        var settingsSvc = new MockSettingsService();
        var histSvc = new HistoryService(historyFilePath: Path.Combine(Path.GetTempPath(), $"hist_{Guid.NewGuid():N}.json"));
        var dlSvc = new DownloadService(ytDlp, histSvc, settingsSvc, new DummyDispatcherService());
        var updateSvc = new MockUpdateService();

        var vm = new MainViewModel(
            ytSvc,
            fmtSvc,
            dlSvc,
            settingsSvc,
            depMgr,
            updateSvc,
            histSvc,
            new DummyDispatcherService());

        vm.DependenciesReady = true;
        vm.UrlInput = "https://www.youtube.com/playlist?list=PLtest123";

        // Bloqueia a conclusão imediata para que os itens permaneçam em estado ativo na fila
        ytDlp.DownloadBlocker = new TaskCompletionSource<bool>();

        await vm.AnalyzeAsync();
        Assert.True(vm.HasPlaylistInfo);

        // Enfileira primeira vez (2 itens)
        vm.EnqueueSelectedPlaylistItemsCommand.Execute(null);
        Assert.Equal(2, dlSvc.QueueItems.Count);

        // Tenta enfileirar novamente os mesmos vídeos enquanto estão ativos na fila
        vm.EnqueueSelectedPlaylistItemsCommand.Execute(null);

        // Fila deve permanecer com 2 itens (não duplicou vídeos ativos)
        Assert.Equal(2, dlSvc.QueueItems.Count);
        Assert.True(vm.HasStatusNotification);
        Assert.Contains("já estão ativos", vm.StatusNotification);

        // Libera blocker
        ytDlp.DownloadBlocker.SetResult(true);
    }

    [Fact]
    public async Task MainViewModel_StartDownload_PreventsDuplicateActiveDownload()
    {
        var depMgr = new MockDependencyManager();
        var ytDlp = new MockYtDlpService();
        var fmtSvc = new FormatSelectionService();
        var ytSvc = new YoutubeService(ytDlp, fmtSvc);
        var settingsSvc = new MockSettingsService();
        var histSvc = new HistoryService(historyFilePath: Path.Combine(Path.GetTempPath(), $"hist_{Guid.NewGuid():N}.json"));
        var dlSvc = new DownloadService(ytDlp, histSvc, settingsSvc, new DummyDispatcherService());
        var updateSvc = new MockUpdateService();

        var vm = new MainViewModel(
            ytSvc,
            fmtSvc,
            dlSvc,
            settingsSvc,
            depMgr,
            updateSvc,
            histSvc,
            new DummyDispatcherService());

        vm.DependenciesReady = true;
        vm.UrlInput = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        // Bloqueia o download para manter o item com IsActive = true
        ytDlp.DownloadBlocker = new TaskCompletionSource<bool>();

        await vm.AnalyzeAsync();
        Assert.True(vm.HasVideoInfo);

        // Inicia download
        vm.StartDownloadCommand.Execute(null);
        Assert.Single(dlSvc.QueueItems);

        // Tenta iniciar novamente o mesmo vídeo
        vm.StartDownloadCommand.Execute(null);
        Assert.Single(dlSvc.QueueItems);
        Assert.Contains("já está ativo", vm.StatusNotification);

        ytDlp.DownloadBlocker.SetResult(true);
    }

    [Fact]
    public async Task MainViewModel_CancelAnalysis_CancelsOngoingMetadataFetch()
    {
        var depMgr = new MockDependencyManager();
        var settingsSvc = new MockSettingsService();
        var histSvc = new HistoryService(historyFilePath: Path.Combine(Path.GetTempPath(), $"hist_{Guid.NewGuid():N}.json"));
        var dlSvc = new DownloadService(new MockYtDlpService(), histSvc, settingsSvc, new DummyDispatcherService());
        var updateSvc = new MockUpdateService();
        var fmtSvc = new FormatSelectionService();

        // YoutubeService com delay longo que reage a cancelamento
        var slowYtSvc = new SlowCancellableYoutubeService();

        var vm = new MainViewModel(
            slowYtSvc,
            fmtSvc,
            dlSvc,
            settingsSvc,
            depMgr,
            updateSvc,
            histSvc,
            new DummyDispatcherService());

        vm.DependenciesReady = true;
        vm.UrlInput = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        var analyzeTask = vm.AnalyzeAsync();
        Assert.True(vm.IsAnalyzing);

        // Dispara o cancelamento
        vm.CancelAnalysisCommand.Execute(null);

        await analyzeTask;

        Assert.False(vm.IsAnalyzing);
        Assert.False(vm.HasVideoInfo);
        Assert.Equal("Análise cancelada pelo usuário.", vm.StatusNotification);
    }

    private class SlowCancellableYoutubeService : IYoutubeService
    {
        public async Task<VideoInfo> AnalyzeVideoAsync(string url, CancellationToken ct = default)
        {
            await Task.Delay(5000, ct);
            return new VideoInfo { Title = "Never finishes" };
        }

        public async Task<PlaylistInfo> AnalyzePlaylistAsync(string url, CancellationToken ct = default)
        {
            await Task.Delay(5000, ct);
            return new PlaylistInfo { Title = "Never finishes" };
        }
    }
}
