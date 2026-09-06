using BaixALL.App.Helpers;
using Xunit;

namespace BaixALL.Tests;

public class UrlValidationTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", true)]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ", true)]
    [InlineData("www.youtube.com/watch?v=dQw4w9WgXcQ", true)]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", true)]
    [InlineData("https://youtube.com/shorts/dQw4w9WgXcQ", true)]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", true)]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", true)]
    [InlineData("https://music.youtube.com/watch?v=dQw4w9WgXcQ", true)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=42s", true)]
    [InlineData("https://www.youtube.com/watch?list=PL123&v=dQw4w9WgXcQ", true)]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abcdef12345", true)]
    [InlineData("https://vimeo.com/12345678", false)]
    [InlineData("https://tiktok.com/@user/video/123456", false)]
    [InlineData("https://google.com", false)]
    [InlineData("qualquer texto", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("https://www.youtube.com/watch?v=curto", false)] // menos de 11 chars
    public void IsValidYouTubeUrl_ShouldValidateCorrectly(string? url, bool expected)
    {
        var result = UrlValidator.IsValidYouTubeUrl(url);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractVideoId_ShouldExtract11CharId()
    {
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&feature=share";
        var id = UrlValidator.ExtractVideoId(url);
        Assert.Equal("dQw4w9WgXcQ", id);
    }

    [Fact]
    public void ExtractVideoId_FromShortUrl_ShouldExtractId()
    {
        var url = "https://youtu.be/dQw4w9WgXcQ?t=15";
        var id = UrlValidator.ExtractVideoId(url);
        Assert.Equal("dQw4w9WgXcQ", id);
    }

    [Fact]
    public void NormalizeYouTubeUrl_ShouldReturnCanonicalUrl()
    {
        var url = "https://youtu.be/dQw4w9WgXcQ?feature=share";
        var normalized = UrlValidator.NormalizeYouTubeUrl(url);
        Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", normalized);
    }
}
