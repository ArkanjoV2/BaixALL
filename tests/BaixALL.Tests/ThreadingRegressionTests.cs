using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Models;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using Xunit;

namespace BaixALL.Tests;

public class ThreadingRegressionTests
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

    private class MockYtDlpService : IYtDlpService
    {
        public Func<DownloadRequest, IProgress<DownloadProgressReport>, CancellationToken, Task<string>>? DownloadHandler { get; set; }

        public Task<string> DownloadAsync(DownloadRequest request, IProgress<DownloadProgressReport> progress, CancellationToken ct = default)
        {
            if (DownloadHandler != null)
            {
                return DownloadHandler(request, progress, ct);
            }
            return Task.FromResult(string.Empty);
        }

        public Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("{}"));
        }

        public Task<JsonDocument> GetPlaylistMetadataJsonAsync(string playlistUrl, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("{}"));
        }
    }

    [Fact]
    public async Task BackgroundThread_AddingHistoryItem_ShouldNotThrowCollectionViewExceptionAndPopulateItems()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"baixall_hist_thread_{Guid.NewGuid()}.json");
        try
        {
            var historyService = new HistoryService(historyFilePath: tempFile);
            var dispatcher = new TestDispatcherService();
            var vm = new HistoryViewModel(historyService, dispatcher);

            Assert.Empty(vm.Items);
            Assert.False(vm.HasItems);

            // Simula adição vinda de uma thread em segundo plano (exatamente como ocorria no DownloadService)
            Exception? backgroundException = null;
            await Task.Run(() =>
            {
                try
                {
                    historyService.AddItem(new HistoryItem
                    {
                        Title = "Until Dawn 2 Trailer",
                        Quality = "1080p (Full HD)",
                        Format = "MP4",
                        FinalFilePath = @"C:\Videos\video.mp4",
                        FileSizeBytes = 104857600,
                        Status = "Concluído"
                    });
                }
                catch (Exception ex)
                {
                    backgroundException = ex;
                }
            });

            Assert.Null(backgroundException);
            Assert.Single(vm.Items);
            Assert.True(vm.HasItems);
            Assert.Equal("Until Dawn 2 Trailer", vm.Items[0].Title);
            Assert.True(dispatcher.InvokeCount > 0, "Dispatcher deve ter sido invocado para despachar a atualização da coleção.");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DownloadService_BackgroundProgressAndCompletion_ShouldUpdateUIProperlyAndEnforceStrictSequence()
    {
        var tempHistFile = Path.Combine(Path.GetTempPath(), $"baixall_hist_{Guid.NewGuid()}.json");
        var tempVideoFile = Path.Combine(Path.GetTempPath(), $"video_real_{Guid.NewGuid()}.mp4");
        await File.WriteAllTextAsync(tempVideoFile, "dummy video content with 12345 bytes");

        try
        {
            var historyService = new HistoryService(historyFilePath: tempHistFile);
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();

            var mockYtDlp = new MockYtDlpService
            {
                DownloadHandler = async (req, prog, ct) =>
                {
                    // Simula progresso em background
                    await Task.Delay(10, ct);
                    prog.Report(new DownloadProgressReport
                    {
                        Status = DownloadStatus.DownloadingVideo,
                        StatusMessage = "Baixando vídeo...",
                        Percentage = 50,
                        DownloadedBytes = 500,
                        TotalBytes = 1000
                    });

                    await Task.Delay(10, ct);
                    prog.Report(new DownloadProgressReport
                    {
                        Status = DownloadStatus.Merging,
                        StatusMessage = "Mesclando áudio e vídeo (FFmpeg)...",
                        Percentage = 95
                    });

                    return tempVideoFile;
                }
            };

            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var item = downloadService.EnqueueDownload(new DownloadRequest
            {
                VideoTitle = "Teste Finalização",
                DestinationFolder = Path.GetDirectoryName(tempVideoFile)!,
                Format = new FormatOption { Label = "1080p", FormatSelector = "bestvideo+bestaudio" },
                Container = new ContainerOption { Id = "mp4", Extension = "mp4" }
            }, "https://thumb.url/img.jpg");

            // Aguarda a conclusão em background
            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && item.IsActive)
            {
                await Task.Delay(20);
            }

            Assert.False(item.IsActive);
            Assert.True(item.IsCompleted);
            Assert.False(item.IsFailed);
            Assert.StartsWith("✓ Download concluído", item.StatusMessage);
            Assert.NotEmpty(item.FormattedFinalFileSize);
            Assert.Equal(tempVideoFile, item.DestinationPath);

            // Verifica que o histórico recebeu o registro
            var history = historyService.GetHistory();
            Assert.Single(history);
            Assert.Equal("Teste Finalização", history[0].Title);
            Assert.Equal(tempVideoFile, history[0].FinalFilePath);
            Assert.True(history[0].FileSizeBytes > 0);
            Assert.Equal("Concluído", history[0].Status);
        }
        finally
        {
            if (File.Exists(tempHistFile)) File.Delete(tempHistFile);
            if (File.Exists(tempVideoFile)) File.Delete(tempVideoFile);
        }
    }

    [Fact]
    public async Task DownloadService_WhenFileDoesNotExist_ShouldNotMarkCompletedAndShowFriendlyError()
    {
        var tempHistFile = Path.Combine(Path.GetTempPath(), $"baixall_hist_{Guid.NewGuid()}.json");
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"does_not_exist_{Guid.NewGuid()}.mp4");

        try
        {
            var historyService = new HistoryService(historyFilePath: tempHistFile);
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();

            var mockYtDlp = new MockYtDlpService
            {
                DownloadHandler = async (req, prog, ct) =>
                {
                    await Task.Delay(10, ct);
                    return nonExistentFile; // Arquivo retornado não existe fisicamente
                }
            };

            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var item = downloadService.EnqueueDownload(new DownloadRequest
            {
                VideoTitle = "Teste Arquivo Inexistente",
                DestinationFolder = Path.GetDirectoryName(nonExistentFile)!,
                Format = new FormatOption { Label = "1080p" },
                Container = new ContainerOption { Id = "mp4", Extension = "mp4" }
            }, "");

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && item.IsActive)
            {
                await Task.Delay(20);
            }

            // Não deve ser marcado como concluído!
            Assert.False(item.IsActive);
            Assert.False(item.IsCompleted);
            Assert.True(item.IsFailed);
            Assert.Equal("O arquivo resultante não foi localizado após o download.", item.StatusMessage);

            // Histórico não deve conter o registro!
            Assert.Empty(historyService.GetHistory());
        }
        finally
        {
            if (File.Exists(tempHistFile)) File.Delete(tempHistFile);
        }
    }

    [Fact]
    public async Task DownloadService_WhenExceptionOccurs_ShouldShowFriendlyErrorAndNotRawStacktrace()
    {
        var tempHistFile = Path.Combine(Path.GetTempPath(), $"baixall_hist_{Guid.NewGuid()}.json");

        try
        {
            var historyService = new HistoryService(historyFilePath: tempHistFile);
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();

            var mockYtDlp = new MockYtDlpService
            {
                DownloadHandler = (req, prog, ct) =>
                {
                    throw new InvalidOperationException("Raw technical .NET internal error: Process exited with code -1073741819");
                }
            };

            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            var item = downloadService.EnqueueDownload(new DownloadRequest
            {
                VideoTitle = "Teste Erro",
                DestinationFolder = Path.GetTempPath(),
                Format = new FormatOption { Label = "720p" },
                Container = new ContainerOption { Id = "mp4", Extension = "mp4" }
            }, "");

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && item.IsActive)
            {
                await Task.Delay(20);
            }

            Assert.True(item.IsFailed);
            // Confirma mensagem amigável sem expor stack trace interna
            Assert.Equal("O download não pôde ser concluído.", item.StatusMessage);
            Assert.DoesNotContain("InvalidOperationException", item.StatusMessage);
            Assert.DoesNotContain("Process exited with code", item.StatusMessage);
        }
        finally
        {
            if (File.Exists(tempHistFile)) File.Delete(tempHistFile);
        }
    }

    [Fact]
    public async Task DownloadService_ConcurrentQueueOperations_ShouldBeThreadSafe()
    {
        var tempHistFile = Path.Combine(Path.GetTempPath(), $"baixall_hist_{Guid.NewGuid()}.json");
        try
        {
            var historyService = new HistoryService(historyFilePath: tempHistFile);
            var dispatcher = new TestDispatcherService();
            var settingsService = new SettingsService();
            var mockYtDlp = new MockYtDlpService
            {
                DownloadHandler = async (req, prog, ct) =>
                {
                    await Task.Delay(50, ct);
                    return "file.mp4";
                }
            };

            var downloadService = new DownloadService(mockYtDlp, historyService, settingsService, dispatcher);

            // Enfileira 10 itens simultâneos de threads diferentes
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                var idx = i;
                tasks[i] = Task.Run(() =>
                {
                    downloadService.EnqueueDownload(new DownloadRequest
                    {
                        VideoTitle = $"Vídeo Concorrente {idx}",
                        DestinationFolder = Path.GetTempPath()
                    }, "");
                });
            }

            await Task.WhenAll(tasks);
            Assert.Equal(10, downloadService.QueueItems.Count);

            // Cancela e remove itens concorrentemente
            var cancelTasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                var id = downloadService.QueueItems[i].Id;
                cancelTasks[i] = Task.Run(() => downloadService.CancelDownload(id));
            }

            await Task.WhenAll(cancelTasks);
        }
        finally
        {
            if (File.Exists(tempHistFile)) File.Delete(tempHistFile);
        }
    }
}
