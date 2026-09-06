using System.Text.Json;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class YtDlpJsonParsingTests
{
    private readonly FormatSelectionService _service = new();

    [Fact]
    public void ParseVideoInfo_ShouldParseCompleteJsonCorrectly()
    {
        var sampleJson = """
        {
            "id": "dQw4w9WgXcQ",
            "title": "Rick Astley - Never Gonna Give You Up",
            "uploader": "Rick Astley",
            "duration": 212,
            "thumbnail": "https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
            "upload_date": "20091025",
            "formats": [
                {
                    "format_id": "249",
                    "ext": "webm",
                    "acodec": "opus",
                    "vcodec": "none",
                    "filesize": 1335431
                },
                {
                    "format_id": "137",
                    "ext": "mp4",
                    "width": 1920,
                    "height": 1080,
                    "fps": 60,
                    "vcodec": "avc1.640028",
                    "acodec": "none",
                    "filesize": 45123000
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(sampleJson);
        var info = _service.ParseVideoInfo(doc, "https://www.youtube.com/watch?v=dQw4w9WgXcQ");

        Assert.Equal("dQw4w9WgXcQ", info.Id);
        Assert.Equal("Rick Astley - Never Gonna Give You Up", info.Title);
        Assert.Equal("Rick Astley", info.Channel);
        Assert.Equal(212, info.DurationSeconds);
        Assert.Equal("3:32", info.FormattedDuration);
        Assert.Equal("25/10/2009", info.UploadDate);
        Assert.Equal("1080p (Full HD)", info.MaxResolution);
        Assert.Equal(60, info.MaxFps);
        Assert.Equal(2, info.Formats.Count);
    }

    [Fact]
    public void ParseVideoInfo_ShouldHandleEmptyOrNullFieldsGracefully()
    {
        var minimalJson = """
        {
            "id": "abc12345678",
            "title": null,
            "formats": []
        }
        """;

        using var doc = JsonDocument.Parse(minimalJson);
        var info = _service.ParseVideoInfo(doc, "https://youtube.com/watch?v=abc12345678");

        Assert.Equal("abc12345678", info.Id);
        Assert.Equal("Vídeo sem título", info.Title);
        Assert.Equal("", info.Channel);
        Assert.Equal(0, info.DurationSeconds);
        Assert.Empty(info.Formats);
    }
}
