using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BaixALL.App.Models;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class FormatSelectionTests
{
    private readonly FormatSelectionService _service = new();

    [Fact]
    public void BuildFormatOptions_ShouldIncludeBestQualityAsDefault()
    {
        var info = new VideoInfo
        {
            Id = "test1",
            Title = "Vídeo 4K 60FPS",
            Formats = new List<VideoFormatRaw>
            {
                new() { FormatId = "137", Height = 1080, Fps = 60, VCodec = "avc1", ACodec = "none" },
                new() { FormatId = "313", Height = 2160, Fps = 60, VCodec = "vp9", ACodec = "none" },
                new() { FormatId = "140", Height = null, Fps = null, VCodec = "none", ACodec = "mp4a" }
            }
        };

        var options = _service.BuildFormatOptions(info);

        Assert.NotEmpty(options);
        var first = options.First();
        Assert.True(first.IsBestQuality);
        Assert.Contains("Melhor qualidade disponível", first.Label);
        Assert.Contains("4K", first.Label);
        Assert.Contains("60 FPS", first.Label);
        Assert.Equal("bestvideo+bestaudio/best", first.FormatSelector);
    }

    [Fact]
    public void BuildFormatOptions_ShouldIncludeIndividualResolutionsInOrder()
    {
        var info = new VideoInfo
        {
            Id = "test2",
            Title = "Vídeo Multi-Resoluções",
            Formats = new List<VideoFormatRaw>
            {
                new() { FormatId = "18", Height = 360, Fps = 30, VCodec = "avc1", ACodec = "mp4a" },
                new() { FormatId = "22", Height = 720, Fps = 30, VCodec = "avc1", ACodec = "mp4a" },
                new() { FormatId = "137", Height = 1080, Fps = 60, VCodec = "avc1", ACodec = "none" }
            }
        };

        var options = _service.BuildFormatOptions(info);

        // 1 melhor + 3 resoluções + 1 áudio = 5 opções
        Assert.Equal(5, options.Count);
        Assert.Contains(options, o => o.Label.Contains("1080p") && o.Label.Contains("60 FPS"));
        Assert.Contains(options, o => o.Label.Contains("720p"));
        Assert.Contains(options, o => o.Label.Contains("360p"));
        Assert.Contains(options, o => o.IsAudioOnly);
    }

    [Theory]
    [InlineData(4320, "8K (4320p)")]
    [InlineData(2160, "4K (2160p)")]
    [InlineData(1440, "1440p (2K)")]
    [InlineData(1080, "1080p (Full HD)")]
    [InlineData(720, "720p (HD)")]
    [InlineData(480, "480p")]
    [InlineData(360, "360p")]
    public void GetResolutionLabel_ShouldMapHeightsCorrectly(int height, string expectedLabel)
    {
        var result = FormatSelectionService.GetResolutionLabel(height);
        Assert.Equal(expectedLabel, result);
    }
}
