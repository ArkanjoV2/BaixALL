using System;
using System.Threading.Tasks;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class YoutubeServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AnalyzeVideoAsync_EmptyUrl_ThrowsArgumentException(string? url)
    {
        var dummyDepManager = new DependencyManager();
        var ytDlp = new YtDlpService(dummyDepManager);
        var formatService = new FormatSelectionService();
        var youtubeService = new YoutubeService(ytDlp, formatService);

        await Assert.ThrowsAsync<ArgumentException>(() => youtubeService.AnalyzeVideoAsync(url!));
    }

    [Theory]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("https://twitter.com/user/status/123456")]
    [InlineData("texto sem formato de url")]
    public async Task AnalyzeVideoAsync_InvalidYouTubeUrl_ThrowsArgumentException(string url)
    {
        var dummyDepManager = new DependencyManager();
        var ytDlp = new YtDlpService(dummyDepManager);
        var formatService = new FormatSelectionService();
        var youtubeService = new YoutubeService(ytDlp, formatService);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => youtubeService.AnalyzeVideoAsync(url));
        Assert.Contains("não pertence ao YouTube", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AnalyzePlaylistAsync_EmptyUrl_ThrowsArgumentException(string? url)
    {
        var dummyDepManager = new DependencyManager();
        var ytDlp = new YtDlpService(dummyDepManager);
        var formatService = new FormatSelectionService();
        var youtubeService = new YoutubeService(ytDlp, formatService);

        await Assert.ThrowsAsync<ArgumentException>(() => youtubeService.AnalyzePlaylistAsync(url!));
    }

    [Theory]
    [InlineData("https://vimeo.com/channels/123456")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")] // Vídeo avulso sem list=
    [InlineData("texto sem formato de url")]
    public async Task AnalyzePlaylistAsync_NonPlaylistUrl_ThrowsArgumentException(string url)
    {
        var dummyDepManager = new DependencyManager();
        var ytDlp = new YtDlpService(dummyDepManager);
        var formatService = new FormatSelectionService();
        var youtubeService = new YoutubeService(ytDlp, formatService);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => youtubeService.AnalyzePlaylistAsync(url));
        Assert.Contains("não contém uma playlist válida", ex.Message);
    }
}
