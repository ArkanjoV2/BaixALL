using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Helpers;
using BaixALL.App.Models;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using Xunit;

namespace BaixALL.Tests;

public class BatteryHardeningRegressionTests
{
    private class TestDispatcherService : IDispatcherService
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> callback) => callback();
        public async Task InvokeAsync(Action action) { action(); await Task.CompletedTask; }
        public async Task<T> InvokeAsync<T>(Func<T> callback) => await Task.FromResult(callback());
        public bool CheckAccess() => true;
    }

    private class FakeYtDlpService : IYtDlpService
    {
        public Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default) =>
            Task.FromResult(JsonDocument.Parse("{}"));

        public Task<JsonDocument> GetPlaylistMetadataJsonAsync(string playlistUrl, CancellationToken ct = default) =>
            Task.FromResult(JsonDocument.Parse("{}"));

        public async Task<string> DownloadAsync(DownloadRequest request, IProgress<DownloadProgressReport> progress, CancellationToken ct = default)
        {
            await Task.Delay(500, ct);
            return Path.Combine(request.DestinationFolder, "test.mp4");
        }
    }

    private class FakeDependencyManager : IDependencyManager
    {
        public bool EnsureAllCalled { get; private set; }
        public bool InstallToolCalled { get; private set; }

        public IReadOnlyList<DependencyItem> GetDependencies() => new List<DependencyItem>();
        public bool AreAllDependenciesInstalled() => true;
        public Task<bool> CheckDependenciesAsync() => Task.FromResult(true);
        public Task<bool> EnsureAllDependenciesAsync(IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default)
        {
            EnsureAllCalled = true;
            return Task.FromResult(true);
        }
        public Task<bool> InstallDependencyByNameAsync(string name, IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default)
        {
            InstallToolCalled = true;
            return Task.FromResult(true);
        }
        public Task<bool> ValidateDependencyAsync(string name) => Task.FromResult(true);
        public Task<bool> UpdateDependencyAsync(string name, IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public string GetYtDlpPath() => "yt-dlp.exe";
        public string GetFFmpegPath() => "ffmpeg.exe";
        public string GetFFprobePath() => "ffprobe.exe";
        public string GetDenoPath() => "deno.exe";
    }

    private class FakeDownloadServiceWithActiveFlag : IDownloadService
    {
        public ObservableCollection<DownloadItemViewModel> QueueItems { get; } = new();
        public bool HasActiveDownloads { get; set; } = true;
        public DownloadItemViewModel EnqueueDownload(DownloadRequest request, string thumbnailUrl) => new();
        public void CancelDownload(Guid id) { }
        public void RemoveDownload(Guid id) { }
        public void ClearCompleted() { }
        public void UpdateConcurrencyLimit(int maxConcurrent) { }
        public void CancelAllDownloads() { }
    }

    [Fact]
    public void DownloadService_CancelAllDownloads_CancelsAllActiveItems()
    {
        var depMgr = new FakeDependencyManager();
        var ytDlp = new FakeYtDlpService();
        var settingsService = new SettingsService();
        var historyService = new HistoryService();

        var svc = new DownloadService(ytDlp, historyService, settingsService, new TestDispatcherService());

        var item1 = svc.EnqueueDownload(new DownloadRequest { VideoTitle = "Video 1", DestinationFolder = Path.GetTempPath() }, "thumb1");
        var item2 = svc.EnqueueDownload(new DownloadRequest { VideoTitle = "Video 2", DestinationFolder = Path.GetTempPath() }, "thumb2");
        var item3 = svc.EnqueueDownload(new DownloadRequest { VideoTitle = "Video 3", DestinationFolder = Path.GetTempPath() }, "thumb3");

        Assert.Equal(3, svc.QueueItems.Count);

        svc.CancelAllDownloads();

        Assert.True(item1.CancellationTokenSource.IsCancellationRequested);
        Assert.True(item2.CancellationTokenSource.IsCancellationRequested);
        Assert.True(item3.CancellationTokenSource.IsCancellationRequested);
    }

    [Fact]
    public async Task DependenciesViewModel_WhenActiveDownloadsExist_BlocksInstallAndShowsFriendlyMessage()
    {
        var fakeDepMgr = new FakeDependencyManager();
        var fakeDownloadSvc = new FakeDownloadServiceWithActiveFlag { HasActiveDownloads = true };

        var vm = new DependenciesViewModel(fakeDepMgr, null, fakeDownloadSvc);

        // Act
        await vm.InstallMissingAsync();

        // Assert
        Assert.True(vm.HasError);
        Assert.Contains("downloads em andamento", vm.ErrorMessage);
        Assert.False(fakeDepMgr.EnsureAllCalled);
    }

    [Fact]
    public async Task DependenciesViewModel_WhenActiveDownloadsExist_BlocksUpdateToolAndShowsFriendlyMessage()
    {
        var fakeDepMgr = new FakeDependencyManager();
        var fakeDownloadSvc = new FakeDownloadServiceWithActiveFlag { HasActiveDownloads = true };

        var vm = new DependenciesViewModel(fakeDepMgr, null, fakeDownloadSvc);

        // Act
        await vm.UpdateToolAsync("yt-dlp");

        // Assert
        Assert.True(vm.HasError);
        Assert.Contains("downloads em andamento", vm.ErrorMessage);
        Assert.False(fakeDepMgr.InstallToolCalled);
    }

    [Fact]
    public void DownloadItemViewModel_MarkCompleted_UpdatesFormatToActualFileExtension()
    {
        var item = new DownloadItemViewModel
        {
            Format = "MP4 (AUTO)"
        };

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testFile = Path.Combine(tempDir, "video.mkv");
        File.WriteAllText(testFile, "dummy");

        try
        {
            item.MarkCompleted(testFile);

            Assert.Equal("MKV", item.Format);
            Assert.True(item.IsCompleted);
            Assert.False(item.IsActive);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void FileHelper_GetUniqueFilePath_CreatesSequentialNamesWithoutOverwriting()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var path1 = FileHelper.GetUniqueFilePath(tempDir, "TestVideo", "mp4");
            File.WriteAllText(path1, "content1");

            var path2 = FileHelper.GetUniqueFilePath(tempDir, "TestVideo", "mp4");
            File.WriteAllText(path2, "content2");

            var path3 = FileHelper.GetUniqueFilePath(tempDir, "TestVideo", "mp4");

            Assert.EndsWith("TestVideo.mp4", path1);
            Assert.EndsWith("TestVideo (1).mp4", path2);
            Assert.EndsWith("TestVideo (2).mp4", path3);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
