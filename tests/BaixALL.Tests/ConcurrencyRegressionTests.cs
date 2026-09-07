using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

public class ConcurrencyRegressionTests
{
    private class TestDispatcherService : IDispatcherService
    {
        private readonly object _lock = new();
        public int InvokeCount { get; private set; }

        public void Invoke(Action action)
        {
            lock (_lock)
            {
                InvokeCount++;
                action();
            }
        }

        public async Task InvokeAsync(Action action)
        {
            lock (_lock)
            {
                InvokeCount++;
                action();
            }
            await Task.CompletedTask;
        }

        public async Task<T> InvokeAsync<T>(Func<T> func)
        {
            lock (_lock)
            {
                InvokeCount++;
                return Task.FromResult(func()).Result;
            }
        }

        public bool CheckAccess() => true;
    }

    private class ControlledYtDlpService : IYtDlpService
    {
        private readonly object _lock = new();
        private int _currentActive = 0;
        public int MaxActiveObserved { get; private set; } = 0;
        public int TotalStarts { get; private set; } = 0;

        public ConcurrentDictionary<string, TaskCompletionSource<string>> GateMap { get; } = new();
        public ConcurrentDictionary<string, TaskCompletionSource<bool>> StartedMap { get; } = new();

        public void Release(string title, string filePath)
        {
            var tcs = GateMap.GetOrAdd(title, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));
            tcs.TrySetResult(filePath);
        }

        public async Task<bool> WaitForStartAsync(string title, int timeoutMs = 5000)
        {
            var tcs = StartedMap.GetOrAdd(title, _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            return completed == tcs.Task;
        }

        public async Task<string> DownloadAsync(DownloadRequest request, IProgress<DownloadProgressReport> progress, CancellationToken ct = default)
        {
            lock (_lock)
            {
                _currentActive++;
                TotalStarts++;
                if (_currentActive > MaxActiveObserved)
                {
                    MaxActiveObserved = _currentActive;
                }
            }

            var startedTcs = StartedMap.GetOrAdd(request.VideoTitle, _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
            startedTcs.TrySetResult(true);

            var gate = GateMap.GetOrAdd(request.VideoTitle, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));

            try
            {
                using var reg = ct.Register(() => gate.TrySetCanceled(ct));
                var result = await gate.Task;
                return result;
            }
            finally
            {
                lock (_lock)
                {
                    _currentActive--;
                }
            }
        }

        public Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("{}"));
        }
    }

    [Fact]
    public async Task ConcurrencyLimit1_NeverExceedsOneActiveDownload()
    {
        var tempHistFile = Path.Combine(Path.GetTempPath(), $"baixall_hist_lim1_{Guid.NewGuid():N}.json");
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_down_lim1_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var historyService = new HistoryService(historyFilePath: tempHistFile);
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            settingsService.Settings.MaxConcurrentDownloads = 1;

            var mockYtDlp = new ControlledYtDlpService();
            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var fileA = Path.Combine(tempFolder, "VideoA.mp4");
            var fileB = Path.Combine(tempFolder, "VideoB.mp4");
            var fileC = Path.Combine(tempFolder, "VideoC.mp4");
            await File.WriteAllTextAsync(fileA, "A");
            await File.WriteAllTextAsync(fileB, "B");
            await File.WriteAllTextAsync(fileC, "C");

            var itemA = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoA", DestinationFolder = tempFolder }, "");
            var itemB = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoB", DestinationFolder = tempFolder }, "");
            var itemC = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoC", DestinationFolder = tempFolder }, "");

            // Wait for A to start
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoA"));

            // B and C must still be queued
            Assert.Equal(DownloadStatus.Queued, itemB.Status);
            Assert.Equal(DownloadStatus.Queued, itemC.Status);
            Assert.Equal(1, mockYtDlp.MaxActiveObserved);

            // Complete A
            mockYtDlp.Release("VideoA", fileA);
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoB"));

            // C still queued
            Assert.Equal(DownloadStatus.Queued, itemC.Status);
            Assert.Equal(1, mockYtDlp.MaxActiveObserved);

            // Complete B
            mockYtDlp.Release("VideoB", fileB);
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoC"));

            // Complete C
            mockYtDlp.Release("VideoC", fileC);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && (!itemA.IsCompleted || !itemB.IsCompleted || !itemC.IsCompleted))
            {
                await Task.Delay(20);
            }

            Assert.True(itemA.IsCompleted);
            Assert.True(itemB.IsCompleted);
            Assert.True(itemC.IsCompleted);
            Assert.Equal(1, mockYtDlp.MaxActiveObserved);
            Assert.Equal(3, historyService.GetHistory().Count);
        }
        finally
        {
            if (File.Exists(tempHistFile)) File.Delete(tempHistFile);
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task ConcurrencyLimit2_ExactlyTwoExecuteSimultaneouslyAndThirdWaits()
    {
        var tempHistFile = Path.Combine(Path.GetTempPath(), $"baixall_hist_lim2_{Guid.NewGuid():N}.json");
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_down_lim2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var historyService = new HistoryService(historyFilePath: tempHistFile);
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            settingsService.Settings.MaxConcurrentDownloads = 2;

            var mockYtDlp = new ControlledYtDlpService();
            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var fileA = Path.Combine(tempFolder, "VideoA.mp4");
            var fileB = Path.Combine(tempFolder, "VideoB.mp4");
            var fileC = Path.Combine(tempFolder, "VideoC.mp4");
            await File.WriteAllTextAsync(fileA, "A");
            await File.WriteAllTextAsync(fileB, "B");
            await File.WriteAllTextAsync(fileC, "C");

            var itemA = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoA", DestinationFolder = tempFolder }, "");
            var itemB = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoB", DestinationFolder = tempFolder }, "");
            var itemC = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoC", DestinationFolder = tempFolder }, "");

            // Both A and B must start concurrently
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoA"));
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoB"));

            // C must wait
            Assert.Equal(DownloadStatus.Queued, itemC.Status);
            Assert.Equal(2, mockYtDlp.MaxActiveObserved);

            // Complete A -> C must immediately start
            mockYtDlp.Release("VideoA", fileA);
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoC"));

            // Complete B and C
            mockYtDlp.Release("VideoB", fileB);
            mockYtDlp.Release("VideoC", fileC);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && (!itemA.IsCompleted || !itemB.IsCompleted || !itemC.IsCompleted))
            {
                await Task.Delay(20);
            }

            Assert.True(itemA.IsCompleted);
            Assert.True(itemB.IsCompleted);
            Assert.True(itemC.IsCompleted);
            Assert.Equal(2, mockYtDlp.MaxActiveObserved);
            Assert.Equal(3, historyService.GetHistory().Count);
        }
        finally
        {
            if (File.Exists(tempHistFile)) File.Delete(tempHistFile);
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task ConcurrencyLimit3_ThreeExecuteSimultaneouslyAndFourthWaits()
    {
        var tempHistFile = Path.Combine(Path.GetTempPath(), $"baixall_hist_lim3_{Guid.NewGuid():N}.json");
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_down_lim3_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var historyService = new HistoryService(historyFilePath: tempHistFile);
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            settingsService.Settings.MaxConcurrentDownloads = 3;

            var mockYtDlp = new ControlledYtDlpService();
            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var fileA = Path.Combine(tempFolder, "VideoA.mp4");
            var fileB = Path.Combine(tempFolder, "VideoB.mp4");
            var fileC = Path.Combine(tempFolder, "VideoC.mp4");
            var fileD = Path.Combine(tempFolder, "VideoD.mp4");
            await File.WriteAllTextAsync(fileA, "A");
            await File.WriteAllTextAsync(fileB, "B");
            await File.WriteAllTextAsync(fileC, "C");
            await File.WriteAllTextAsync(fileD, "D");

            var itemA = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoA", DestinationFolder = tempFolder }, "");
            var itemB = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoB", DestinationFolder = tempFolder }, "");
            var itemC = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoC", DestinationFolder = tempFolder }, "");
            var itemD = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoD", DestinationFolder = tempFolder }, "");

            // A, B, C start simultaneously
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoA"));
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoB"));
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoC"));

            // D must wait in queue
            Assert.Equal(DownloadStatus.Queued, itemD.Status);
            Assert.Equal(3, mockYtDlp.MaxActiveObserved);

            // Finish A -> D starts
            mockYtDlp.Release("VideoA", fileA);
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoD"));

            // Finish B, C, D
            mockYtDlp.Release("VideoB", fileB);
            mockYtDlp.Release("VideoC", fileC);
            mockYtDlp.Release("VideoD", fileD);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && (!itemA.IsCompleted || !itemB.IsCompleted || !itemC.IsCompleted || !itemD.IsCompleted))
            {
                await Task.Delay(20);
            }

            Assert.True(itemA.IsCompleted);
            Assert.True(itemB.IsCompleted);
            Assert.True(itemC.IsCompleted);
            Assert.True(itemD.IsCompleted);
            Assert.Equal(3, mockYtDlp.MaxActiveObserved);
            Assert.Equal(4, historyService.GetHistory().Count);
        }
        finally
        {
            if (File.Exists(tempHistFile)) File.Delete(tempHistFile);
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task CancelActiveDownload_FreesSlotImmediatelyForWaitingItem()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_cancel_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var historyService = new HistoryService(historyFilePath: Path.Combine(tempFolder, "hist.json"));
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            settingsService.Settings.MaxConcurrentDownloads = 2;

            var mockYtDlp = new ControlledYtDlpService();
            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var fileB = Path.Combine(tempFolder, "VideoB.mp4");
            var fileC = Path.Combine(tempFolder, "VideoC.mp4");
            await File.WriteAllTextAsync(fileB, "B");
            await File.WriteAllTextAsync(fileC, "C");

            var itemA = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoA", DestinationFolder = tempFolder }, "");
            var itemB = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoB", DestinationFolder = tempFolder }, "");
            var itemC = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoC", DestinationFolder = tempFolder }, "");

            Assert.True(await mockYtDlp.WaitForStartAsync("VideoA"));
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoB"));

            Assert.Equal(DownloadStatus.Queued, itemC.Status);

            // Cancel active item A
            downloadService.CancelDownload(itemA.Id);

            // C must start immediately!
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoC"));

            // Complete B and C
            mockYtDlp.Release("VideoB", fileB);
            mockYtDlp.Release("VideoC", fileC);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && (!itemB.IsCompleted || !itemC.IsCompleted))
            {
                await Task.Delay(20);
            }

            Assert.True(itemA.IsCanceled);
            Assert.True(itemB.IsCompleted);
            Assert.True(itemC.IsCompleted);
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task CancelWaitingDownload_DoesNotStartProcessNorAffectActiveDownloads()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_cancel_wait_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var historyService = new HistoryService(historyFilePath: Path.Combine(tempFolder, "hist.json"));
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            settingsService.Settings.MaxConcurrentDownloads = 1;

            var mockYtDlp = new ControlledYtDlpService();
            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var fileA = Path.Combine(tempFolder, "VideoA.mp4");
            await File.WriteAllTextAsync(fileA, "A");

            var itemA = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoA", DestinationFolder = tempFolder }, "");
            var itemB = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoB", DestinationFolder = tempFolder }, "");

            // Wait for A to start
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoA"));
            Assert.Equal(DownloadStatus.Queued, itemB.Status);

            // Cancel waiting item B
            downloadService.CancelDownload(itemB.Id);
            Assert.True(itemB.IsCanceled);

            // Complete A
            mockYtDlp.Release("VideoA", fileA);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && !itemA.IsCompleted)
            {
                await Task.Delay(20);
            }

            Assert.True(itemA.IsCompleted);
            // B was never started!
            Assert.Equal(1, mockYtDlp.TotalStarts);
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task DynamicLimitChange_DecreasingFrom3To1While3Active_DoesNotCancelAndFinishesOrderly()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_dyn_dec_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var historyService = new HistoryService(historyFilePath: Path.Combine(tempFolder, "hist.json"));
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            settingsService.Settings.MaxConcurrentDownloads = 3;

            var mockYtDlp = new ControlledYtDlpService();
            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var fileA = Path.Combine(tempFolder, "VideoA.mp4");
            var fileB = Path.Combine(tempFolder, "VideoB.mp4");
            var fileC = Path.Combine(tempFolder, "VideoC.mp4");
            var fileD = Path.Combine(tempFolder, "VideoD.mp4");
            await File.WriteAllTextAsync(fileA, "A");
            await File.WriteAllTextAsync(fileB, "B");
            await File.WriteAllTextAsync(fileC, "C");
            await File.WriteAllTextAsync(fileD, "D");

            var itemA = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoA", DestinationFolder = tempFolder }, "");
            var itemB = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoB", DestinationFolder = tempFolder }, "");
            var itemC = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoC", DestinationFolder = tempFolder }, "");
            var itemD = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoD", DestinationFolder = tempFolder }, "");

            Assert.True(await mockYtDlp.WaitForStartAsync("VideoA"));
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoB"));
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoC"));

            Assert.Equal(DownloadStatus.Queued, itemD.Status);

            // Change limit dynamically to 1
            downloadService.UpdateConcurrencyLimit(1);

            // A, B, C must STILL be active
            Assert.True(itemA.IsActive);
            Assert.True(itemB.IsActive);
            Assert.True(itemC.IsActive);
            Assert.Equal(DownloadStatus.Queued, itemD.Status);

            // Finish A: active count is now 2 (>= 1), so D should NOT start yet!
            mockYtDlp.Release("VideoA", fileA);
            await Task.Delay(100);
            Assert.Equal(DownloadStatus.Queued, itemD.Status);

            // Finish B: active count is now 1 (>= 1), so D should NOT start yet!
            mockYtDlp.Release("VideoB", fileB);
            await Task.Delay(100);
            Assert.Equal(DownloadStatus.Queued, itemD.Status);

            // Finish C: active count is now 0 (< 1), so D MUST start now!
            mockYtDlp.Release("VideoC", fileC);
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoD"));

            mockYtDlp.Release("VideoD", fileD);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && (!itemA.IsCompleted || !itemB.IsCompleted || !itemC.IsCompleted || !itemD.IsCompleted))
            {
                await Task.Delay(20);
            }

            Assert.True(itemA.IsCompleted);
            Assert.True(itemB.IsCompleted);
            Assert.True(itemC.IsCompleted);
            Assert.True(itemD.IsCompleted);
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task DynamicLimitChange_IncreasingFrom1To3_StartsWaitingItemsImmediately()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_dyn_inc_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var historyService = new HistoryService(historyFilePath: Path.Combine(tempFolder, "hist.json"));
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            settingsService.Settings.MaxConcurrentDownloads = 1;

            var mockYtDlp = new ControlledYtDlpService();
            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var fileA = Path.Combine(tempFolder, "VideoA.mp4");
            var fileB = Path.Combine(tempFolder, "VideoB.mp4");
            var fileC = Path.Combine(tempFolder, "VideoC.mp4");
            await File.WriteAllTextAsync(fileA, "A");
            await File.WriteAllTextAsync(fileB, "B");
            await File.WriteAllTextAsync(fileC, "C");

            var itemA = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoA", DestinationFolder = tempFolder }, "");
            var itemB = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoB", DestinationFolder = tempFolder }, "");
            var itemC = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoC", DestinationFolder = tempFolder }, "");

            // A started, B and C waiting
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoA"));
            Assert.Equal(DownloadStatus.Queued, itemB.Status);
            Assert.Equal(DownloadStatus.Queued, itemC.Status);

            // Increase limit to 3!
            downloadService.UpdateConcurrencyLimit(3);

            // B and C should start immediately!
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoB"));
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoC"));

            Assert.Equal(3, mockYtDlp.MaxActiveObserved);

            mockYtDlp.Release("VideoA", fileA);
            mockYtDlp.Release("VideoB", fileB);
            mockYtDlp.Release("VideoC", fileC);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && (!itemA.IsCompleted || !itemB.IsCompleted || !itemC.IsCompleted))
            {
                await Task.Delay(20);
            }

            Assert.True(itemA.IsCompleted);
            Assert.True(itemB.IsCompleted);
            Assert.True(itemC.IsCompleted);
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task SimultaneousCompletions_ProduceDistinctRecordsInHistoryWithoutLoss()
    {
        var tempHistFile = Path.Combine(Path.GetTempPath(), $"baixall_hist_race_{Guid.NewGuid():N}.json");
        try
        {
            var historyService = new HistoryService(historyFilePath: tempHistFile);

            var tasks = Enumerable.Range(1, 10).Select(i => Task.Run(() =>
            {
                historyService.AddItem(new HistoryItem
                {
                    Title = $"Video {i}",
                    FinalFilePath = $@"C:\Downloads\video_{i}.mp4",
                    Status = "Concluído"
                });
            }));

            await Task.WhenAll(tasks);

            var records = historyService.GetHistory();
            Assert.Equal(10, records.Count);

            var service2 = new HistoryService(historyFilePath: tempHistFile);
            Assert.Equal(10, service2.GetHistory().Count);
        }
        finally
        {
            if (File.Exists(tempHistFile)) File.Delete(tempHistFile);
        }
    }

    [Fact]
    public void DuplicateVideoDownloads_ProduceUniqueFilePathsWithoutCollision()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_dup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var path1 = FileHelper.GetUniqueFilePath(tempFolder, "MyVideo", "mp4");
            File.WriteAllText(path1, "content1");

            var path2 = FileHelper.GetUniqueFilePath(tempFolder, "MyVideo", "mp4");
            File.WriteAllText(path2, "content2");

            var path3 = FileHelper.GetUniqueFilePath(tempFolder, "MyVideo", "mp4");

            Assert.EndsWith("MyVideo.mp4", path1);
            Assert.EndsWith("MyVideo (1).mp4", path2);
            Assert.EndsWith("MyVideo (2).mp4", path3);
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task UpdateService_BlocksToolUpdatesWhileDownloadsActive()
    {
        var fakeDepMgr = new FakeDepMgr();
        var fakeDownloadService = new FakeDownloadServiceWithActiveFlag { HasActiveDownloads = true };
        var updateService = new UpdateService(fakeDepMgr, fakeDownloadService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => updateService.UpdateToolAsync("yt-dlp"));
    }

    private class FakeDepMgr : IDependencyManager
    {
        public bool AreAllDependenciesInstalled() => true;
        public Task<bool> CheckDependenciesAsync() => Task.FromResult(true);
        public Task<bool> EnsureAllDependenciesAsync(IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public string GetDenoPath() => "deno.exe";
        public IReadOnlyList<DependencyItem> GetDependencies() => new List<DependencyItem>();
        public string GetFFmpegPath() => "ffmpeg.exe";
        public string GetFFprobePath() => "ffprobe.exe";
        public string GetYtDlpPath() => "yt-dlp.exe";
        public Task<bool> InstallDependencyByNameAsync(string name, IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> UpdateDependencyAsync(string name, IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateDependencyAsync(string name) => Task.FromResult(true);
    }

    private class FakeDownloadServiceWithActiveFlag : IDownloadService
    {
        public System.Collections.ObjectModel.ObservableCollection<DownloadItemViewModel> QueueItems { get; } = new();
        public bool HasActiveDownloads { get; set; }
        public void CancelAllDownloads() { }
        public void CancelDownload(Guid id) { }
        public void ClearCompleted() { }
        public DownloadItemViewModel EnqueueDownload(DownloadRequest request, string thumbnailUrl) => new();
        public void RemoveDownload(Guid id) { }
        public void UpdateConcurrencyLimit(int maxConcurrent) { }
    }
    [Fact]
    public async Task DownloadFailure_DoesNotInterruptOtherDownloadsNorLeakSlots()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_fail_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var historyService = new HistoryService(historyFilePath: Path.Combine(tempFolder, "hist.json"));
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            settingsService.Settings.MaxConcurrentDownloads = 2;

            var mockYtDlp = new ControlledYtDlpService();
            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var fileB = Path.Combine(tempFolder, "VideoB.mp4");
            var fileC = Path.Combine(tempFolder, "VideoC.mp4");
            await File.WriteAllTextAsync(fileB, "B");
            await File.WriteAllTextAsync(fileC, "C");

            var itemA = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoA", DestinationFolder = tempFolder }, "");
            var itemB = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoB", DestinationFolder = tempFolder }, "");
            var itemC = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoC", DestinationFolder = tempFolder }, "");

            Assert.True(await mockYtDlp.WaitForStartAsync("VideoA"));
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoB"));
            Assert.Equal(DownloadStatus.Queued, itemC.Status);

            // A fails with an exception
            var aGate = mockYtDlp.GateMap.GetOrAdd("VideoA", _ => new());
            aGate.TrySetException(new InvalidOperationException("Erro simulado no download"));

            // C must start immediately!
            Assert.True(await mockYtDlp.WaitForStartAsync("VideoC"));

            // Complete B and C
            mockYtDlp.Release("VideoB", fileB);
            mockYtDlp.Release("VideoC", fileC);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && (!itemA.IsFailed || !itemB.IsCompleted || !itemC.IsCompleted))
            {
                await Task.Delay(20);
            }

            Assert.True(itemA.IsFailed);
            Assert.True(itemB.IsCompleted);
            Assert.True(itemC.IsCompleted);
            Assert.Equal(0, downloadService.ActiveDownloadsCount);
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task AllQueueModifications_InvokeUIDispatcher()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"baixall_disp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var historyService = new HistoryService(historyFilePath: Path.Combine(tempFolder, "hist.json"));
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            settingsService.Settings.MaxConcurrentDownloads = 1;

            var mockYtDlp = new ControlledYtDlpService();
            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var fileA = Path.Combine(tempFolder, "VideoA.mp4");
            await File.WriteAllTextAsync(fileA, "A");

            var initialInvokeCount = dispatcher.InvokeCount;

            var itemA = downloadService.EnqueueDownload(new DownloadRequest { VideoTitle = "VideoA", DestinationFolder = tempFolder }, "");
            Assert.True(dispatcher.InvokeCount > initialInvokeCount, "Enqueue deve invocar Dispatcher para inserir na coleção.");

            Assert.True(await mockYtDlp.WaitForStartAsync("VideoA"));

            var beforeFinish = dispatcher.InvokeCount;
            mockYtDlp.Release("VideoA", fileA);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && !itemA.IsCompleted)
            {
                await Task.Delay(20);
            }

            Assert.True(itemA.IsCompleted);
            Assert.True(dispatcher.InvokeCount > beforeFinish, "Conclusão e progresso devem invocar Dispatcher.");
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }
}

