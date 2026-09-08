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
        public Task<string> DownloadAsync(DownloadRequest request, IProgress<DownloadProgressReport> progress, CancellationToken ct = default)
        {
            return Task.FromResult(Path.Combine(request.DestinationFolder, $"{request.VideoTitle}.mp4"));
        }

        public Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default)
        {
            var json = """
            {
                "id": "vid123",
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
                    { "id": "v1", "title": "Aula 01", "duration": 120, "uploader": "Professor X", "url": "https://www.youtube.com/watch?v=v1" },
                    { "id": "v2", "title": "Aula 02", "duration": 240, "uploader": "Professor X", "url": "https://www.youtube.com/watch?v=v2" },
                    { "id": "v3", "title": "[Private video]", "duration": 0, "uploader": "Professor X", "url": "https://www.youtube.com/watch?v=v3" }
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
        var ytDlp = new MockYtDlpService();
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
    }
}
