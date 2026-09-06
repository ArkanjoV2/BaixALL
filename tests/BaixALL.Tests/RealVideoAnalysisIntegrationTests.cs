using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BaixALL.App.Infrastructure;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class RealVideoAnalysisIntegrationTests
{
    [Fact]
    public async Task AnalyzeRealVideo_PreviouslyFailingVideo_SucceedsWithoutNullExceptions()
    {
        var toolsDir = AppConstants.ToolsFolder;
        var ytDlpPath = Path.Combine(toolsDir, "yt-dlp", "yt-dlp.exe");

        if (!File.Exists(ytDlpPath))
        {
            return; // Executa apenas se ferramentas estiverem instaladas
        }

        var depManager = new DependencyManager();
        var ytDlpService = new YtDlpService(depManager);
        var formatService = new FormatSelectionService();
        var youtubeService = new YoutubeService(ytDlpService, formatService);

        // URL real que provocou a exceção original
        var url = "https://www.youtube.com/watch?v=gy4T-vS-Ats";
        var info = await youtubeService.AnalyzeVideoAsync(url);

        Assert.NotNull(info);
        Assert.Equal("gy4T-vS-Ats", info.Id);
        Assert.False(string.IsNullOrWhiteSpace(info.Title));
        Assert.Equal("PlayStation Brasil", info.Channel);
        Assert.NotNull(info.DurationSeconds);
        Assert.True(info.DurationSeconds > 0);
        Assert.False(string.IsNullOrWhiteSpace(info.ThumbnailUrl));
        Assert.NotEmpty(info.Formats);

        var options = formatService.BuildFormatOptions(info);
        Assert.NotEmpty(options);
        var best = options.First(o => o.IsBestQuality);
        Assert.Contains("4K", best.Label);
    }

    [Fact]
    public async Task AnalyzeRealVideo_MeAtTheZoo_SucceedsWithoutNullExceptions()
    {
        var toolsDir = AppConstants.ToolsFolder;
        var ytDlpPath = Path.Combine(toolsDir, "yt-dlp", "yt-dlp.exe");

        if (!File.Exists(ytDlpPath))
        {
            return;
        }

        var depManager = new DependencyManager();
        var ytDlpService = new YtDlpService(depManager);
        var formatService = new FormatSelectionService();
        var youtubeService = new YoutubeService(ytDlpService, formatService);

        var url = "https://www.youtube.com/watch?v=jNQXAC9IVRw";
        var info = await youtubeService.AnalyzeVideoAsync(url);

        Assert.NotNull(info);
        Assert.Equal("jNQXAC9IVRw", info.Id);
        Assert.Contains("Me at the zoo", info.Title);
        Assert.Equal("jawed", info.Channel);
        Assert.NotNull(info.DurationSeconds);
        Assert.True(info.DurationSeconds > 0);
        Assert.NotEmpty(info.Formats);

        var options = formatService.BuildFormatOptions(info);
        Assert.NotEmpty(options);
    }
}
