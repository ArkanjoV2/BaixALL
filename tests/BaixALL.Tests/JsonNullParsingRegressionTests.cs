using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BaixALL.App.Models;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class JsonNullParsingRegressionTests
{
    private readonly FormatSelectionService _service = new();

    [Fact]
    public void ParseVideoInfo_WithAllNullableFieldsExplicitlyNull_DoesNotThrow()
    {
        var json = """
        {
            "id": "null_test_01",
            "title": "Vídeo com Campos Nulos",
            "uploader": null,
            "channel": null,
            "uploader_id": null,
            "duration": null,
            "thumbnail": null,
            "upload_date": null,
            "formats": [
                {
                    "format_id": "f_all_null",
                    "format_note": null,
                    "ext": "mp4",
                    "width": null,
                    "height": null,
                    "fps": null,
                    "vcodec": null,
                    "acodec": null,
                    "filesize": null,
                    "filesize_approx": null,
                    "tbr": null,
                    "vbr": null,
                    "abr": null,
                    "asr": null,
                    "audio_channels": null
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var info = _service.ParseVideoInfo(doc, "https://youtube.com/watch?v=null_test_01");

        Assert.NotNull(info);
        Assert.Equal("null_test_01", info.Id);
        Assert.Equal("Vídeo com Campos Nulos", info.Title);
        Assert.Equal("", info.Channel);
        Assert.Null(info.DurationSeconds);
        Assert.Equal("—", info.FormattedDuration);
        Assert.Null(info.MaxFps);
        Assert.Equal(string.Empty, info.FormattedFps);
        Assert.Single(info.Formats);

        var fmt = info.Formats[0];
        Assert.Null(fmt.Width);
        Assert.Null(fmt.Height);
        Assert.Null(fmt.Fps);
        Assert.Null(fmt.FileSize);
        Assert.Null(fmt.FileSizeApprox);
        Assert.Null(fmt.Abr);
        Assert.Null(fmt.Tbr);
        Assert.False(fmt.HasVideo);
        Assert.False(fmt.HasAudio);
    }

    [Fact]
    public void ParseVideoInfo_WithPropertiesCompletelyMissing_DoesNotThrow()
    {
        var json = """
        {
            "id": "missing_props_video"
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var info = _service.ParseVideoInfo(doc, "https://youtube.com/watch?v=missing_props_video");

        Assert.NotNull(info);
        Assert.Equal("missing_props_video", info.Id);
        Assert.Equal("Vídeo sem título", info.Title);
        Assert.Empty(info.Formats);
        Assert.Null(info.DurationSeconds);
    }

    [Fact]
    public void ParseVideoInfo_WithCombinationsFromUserSpecs_ParsesCleanly()
    {
        // Casos especificados pelo usuário:
        // 1. height = 2160, fps = null
        // 2. height = 1080, fps = 60
        // 3. height = null, fps = null
        // 4. filesize = null, filesize_approx = número
        // 5. filesize = null, filesize_approx = null
        // 6. Formato storyboard (sb3) do YouTube com fps decimal e filesize nulo
        var json = """
        {
            "id": "combos_video",
            "title": "Vídeo com Combinações Complexas",
            "duration": 185.5,
            "uploader": "Canal de Teste",
            "upload_date": "20260906",
            "formats": [
                {
                    "format_id": "sb3",
                    "ext": "mhtml",
                    "resolution": "48x27",
                    "fps": 0.6622516556291391,
                    "filesize": null,
                    "filesize_approx": null,
                    "vcodec": "none",
                    "acodec": "none"
                },
                {
                    "format_id": "fmt_2160_nofps",
                    "ext": "webm",
                    "height": 2160,
                    "width": 3840,
                    "fps": null,
                    "filesize": null,
                    "filesize_approx": 850000000,
                    "vcodec": "vp9",
                    "acodec": "none"
                },
                {
                    "format_id": "fmt_1080_60fps",
                    "ext": "mp4",
                    "height": 1080,
                    "width": 1920,
                    "fps": 60,
                    "filesize": 420000000,
                    "filesize_approx": null,
                    "vcodec": "avc1",
                    "acodec": "none"
                },
                {
                    "format_id": "fmt_no_height_no_fps",
                    "ext": "webm",
                    "height": null,
                    "width": null,
                    "fps": null,
                    "filesize": null,
                    "filesize_approx": null,
                    "vcodec": "vp9",
                    "acodec": "none"
                },
                {
                    "format_id": "fmt_audio",
                    "ext": "m4a",
                    "height": null,
                    "width": null,
                    "fps": null,
                    "filesize": 15000000,
                    "filesize_approx": null,
                    "vcodec": "none",
                    "acodec": "mp4a.40.2",
                    "abr": 128
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var info = _service.ParseVideoInfo(doc, "https://youtube.com/watch?v=combos_video");

        Assert.Equal(5, info.Formats.Count);
        Assert.Equal(2160, info.Formats[1].Height);
        Assert.Null(info.Formats[1].Fps);
        Assert.Equal(850000000, info.Formats[1].FileSizeApprox);
        Assert.Null(info.Formats[1].FileSize);

        Assert.Equal(1080, info.Formats[2].Height);
        Assert.Equal(60, info.Formats[2].Fps);
        Assert.Equal(420000000, info.Formats[2].FileSize);

        Assert.Null(info.Formats[3].Height);
        Assert.Null(info.Formats[3].Fps);
        Assert.Null(info.Formats[3].FileSize);
        Assert.Null(info.Formats[3].FileSizeApprox);

        // MaxResolution deve identificar 4K (2160p)
        Assert.Contains("4K", info.MaxResolution);
        // MaxFps deve identificar 60 FPS
        Assert.Equal(60, info.MaxFps);
        Assert.Equal("60 FPS", info.FormattedFps);

        // Opções de download devem ser geradas sem falhas
        var options = _service.BuildFormatOptions(info);
        Assert.NotEmpty(options);

        // A melhor qualidade deve ser 4K
        var best = options.First(o => o.IsBestQuality);
        Assert.Contains("4K", best.Label);
        Assert.Equal(2160, best.Height);

        // Deve existir opção 1080p 60FPS
        var opt1080 = options.FirstOrDefault(o => o.Height == 1080);
        Assert.NotNull(opt1080);
        Assert.Contains("60 FPS", opt1080.Label);
    }

    [Fact]
    public void BuildFormatOptions_WhenFormatsHaveNoFpsOrNoHeight_HandlesGracefully()
    {
        var info = new VideoInfo
        {
            Id = "graceful_test",
            Title = "Vídeo Sem FPS",
            Formats = new List<VideoFormatRaw>
            {
                new() { FormatId = "1080_nofps", Height = 1080, Fps = null, VCodec = "avc1", ACodec = "none", FileSize = null },
                new() { FormatId = "720_nofps", Height = 720, Fps = null, VCodec = "avc1", ACodec = "none", FileSize = null }
            }
        };

        var options = _service.BuildFormatOptions(info);
        Assert.NotNull(options);
        Assert.True(options.Count >= 3); // Melhor + 1080p + 720p + Somente áudio

        var best = options.First(o => o.IsBestQuality);
        Assert.Contains("1080p", best.Label);
        Assert.DoesNotContain("FPS", best.Label); // Sem FPS pois era nulo
    }

    [Fact]
    public void BuildFormatOptions_WhenOnlyAudioFormatsExist_GeneratesAudioAndBestOptions()
    {
        var info = new VideoInfo
        {
            Id = "audio_only_video",
            Title = "Podcast Somente Áudio",
            Formats = new List<VideoFormatRaw>
            {
                new() { FormatId = "140", Height = null, Fps = null, VCodec = "none", ACodec = "mp4a.40.2", FileSize = 5000000 }
            }
        };

        var options = _service.BuildFormatOptions(info);
        Assert.NotNull(options);
        Assert.Contains(options, o => o.IsAudioOnly);
        Assert.Contains(options, o => o.IsBestQuality);
    }

    [Fact]
    public void ParseVideoInfo_WithRealDumpedYouTubeJson_ParsesWithoutAnyException()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "TestData", "real_youtube_dump_gy4T-vS-Ats.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "TestData", "real_youtube_dump_gy4T-vS-Ats.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "tests", "BaixALL.Tests", "TestData", "real_youtube_dump_gy4T-vS-Ats.json"),
            Path.Combine(Path.GetTempPath(), "test_yt_dump.json")
        };

        var testDataPath = candidates.FirstOrDefault(File.Exists);
        Assert.True(testDataPath != null, "Arquivo de dump de teste real deve existir.");

        var json = File.ReadAllText(testDataPath);
        using var doc = JsonDocument.Parse(json);
        var info = _service.ParseVideoInfo(doc, "https://www.youtube.com/watch?v=gy4T-vS-Ats");

        Assert.NotNull(info);
        Assert.Equal("gy4T-vS-Ats", info.Id);
        Assert.Contains("Until Dawn", info.Title);
        Assert.Equal("PlayStation Brasil", info.Channel);
        Assert.Equal(151, info.DurationSeconds);
        Assert.NotEmpty(info.Formats);

        // O formato 'sb3' gerava a exceção anteriormente por conter filesize e filesize_approx nulos
        var sb3 = info.Formats.FirstOrDefault(f => f.FormatId == "sb3");
        Assert.NotNull(sb3);
        Assert.Null(sb3.FileSize);
        Assert.Null(sb3.FileSizeApprox);

        // Verifica que as opções de qualidade foram construídas
        var options = _service.BuildFormatOptions(info);
        Assert.NotEmpty(options);
        var best = options.FirstOrDefault(o => o.IsBestQuality);
        Assert.NotNull(best);
        Assert.Contains("4K", best.Label);
        Assert.Contains(options, o => o.Label.Contains("1080p"));
    }
}
