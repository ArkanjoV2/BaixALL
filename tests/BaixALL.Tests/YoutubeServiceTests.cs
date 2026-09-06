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
}
