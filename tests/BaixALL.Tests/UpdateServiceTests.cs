using System;
using System.Threading.Tasks;
using BaixALL.App.Models;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class UpdateServiceTests
{
    [Fact]
    public async Task UpdateToolAsync_WhenActiveDownloadsExist_ThrowsInvalidOperationException()
    {
        var depManager = new DependencyManager();
        var settingsService = new SettingsService();
        var historyService = new HistoryService();
        var ytDlpService = new YtDlpService(depManager);
        var downloadService = new DownloadService(ytDlpService, historyService, settingsService);
        var updateService = new UpdateService(depManager, downloadService);

        // Adiciona um item ativo na fila simulado
        var req = new DownloadRequest
        {
            VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            VideoTitle = "Teste Ativo",
            DestinationFolder = @"C:\Downloads"
        };
        downloadService.EnqueueDownload(req, "");

        await Assert.ThrowsAsync<InvalidOperationException>(() => updateService.UpdateToolAsync("yt-dlp"));
    }

    [Fact]
    public async Task CheckYtDlpUpdateAsync_ReturnsValidUpdateResult()
    {
        var depManager = new DependencyManager();
        var settingsService = new SettingsService();
        var historyService = new HistoryService();
        var ytDlpService = new YtDlpService(depManager);
        var downloadService = new DownloadService(ytDlpService, historyService, settingsService);
        var updateService = new UpdateService(depManager, downloadService);

        var result = await updateService.CheckYtDlpUpdateAsync();

        Assert.Equal("yt-dlp", result.ToolName);
        Assert.NotNull(result.CurrentVersion);
        Assert.NotNull(result.LatestVersion);
    }
}
