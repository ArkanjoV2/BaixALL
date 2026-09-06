using System.Collections.Generic;
using System.Linq;
using BaixALL.App.Models;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class FormatSelectionMultiFpsRegressionTests
{
    private readonly FormatSelectionService _service = new();

    [Fact]
    public void BuildFormatOptions_WhenVideoHasMultipleFpsForSameHeight_ShouldGenerateDistinctOptions()
    {
        var info = new VideoInfo
        {
            Id = "test-multi-fps",
            Title = "Vídeo Multi FPS",
            Formats = new List<VideoFormatRaw>
            {
                // 1080p a 60 FPS
                new() { FormatId = "f1080_60", Height = 1080, Fps = 60, VCodec = "avc1" },
                // 1080p a 30 FPS
                new() { FormatId = "f1080_30", Height = 1080, Fps = 30, VCodec = "avc1" },
                // 720p a 60 FPS
                new() { FormatId = "f720_60", Height = 720, Fps = 60, VCodec = "avc1" },
                // 480p a 30 FPS
                new() { FormatId = "f480_30", Height = 480, Fps = 30, VCodec = "avc1" }
            }
        };

        var options = _service.BuildFormatOptions(info);

        Assert.NotEmpty(options);
        // Primeiro item: Melhor qualidade disponível
        Assert.True(options[0].IsBestQuality);
        Assert.StartsWith("Melhor qualidade disponível", options[0].Label);

        // Opções de 1080p: devem existir tanto 60 FPS quanto 30 FPS
        var opt1080_60 = options.FirstOrDefault(o => !o.IsBestQuality && o.Height == 1080 && o.Fps == 60);
        var opt1080_30 = options.FirstOrDefault(o => !o.IsBestQuality && o.Height == 1080 && o.Fps == 30);

        Assert.NotNull(opt1080_60);
        Assert.NotNull(opt1080_30);
        Assert.Equal("1080p (Full HD) • 60 FPS", opt1080_60.Label);
        Assert.Equal("1080p (Full HD) • 30 FPS", opt1080_30.Label);
        Assert.Contains("fps>30", opt1080_60.FormatSelector);
        Assert.Contains("fps<=30", opt1080_30.FormatSelector);

        // Opção de 720p (única taxa de quadros 60 FPS)
        var opt720 = options.FirstOrDefault(o => o.Height == 720);
        Assert.NotNull(opt720);
        Assert.Equal("720p (HD) • 60 FPS", opt720.Label);

        // Opção de 480p (padrão 30 FPS)
        var opt480 = options.FirstOrDefault(o => o.Height == 480);
        Assert.NotNull(opt480);
        Assert.Equal("480p", opt480.Label);

        // Último item: Somente áudio
        var last = options.Last();
        Assert.True(last.IsAudioOnly);
        Assert.Equal("🎵 Somente áudio", last.Label);
    }

    [Fact]
    public void ContainerOptions_ShouldIncludeAllExpectedFormats()
    {
        var containers = ContainerOption.DefaultOptions;
        var ids = containers.Select(c => c.Id).ToList();

        Assert.Contains("auto", ids);
        Assert.Contains("mp4", ids);
        Assert.Contains("mkv", ids);
        Assert.Contains("webm", ids);

        var auto = containers.First(c => c.Id == "auto");
        Assert.Contains("Automático", auto.Label);
    }
}
