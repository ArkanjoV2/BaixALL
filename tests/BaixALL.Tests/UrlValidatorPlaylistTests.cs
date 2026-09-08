using BaixALL.App.Helpers;
using Xunit;

namespace BaixALL.Tests;

public class UrlValidatorPlaylistTests
{
    [Theory]
    [InlineData("https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", true)]
    [InlineData("http://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", true)]
    [InlineData("www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", true)]
    [InlineData("https://music.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", true)]
    [InlineData("https://m.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", true)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", true)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9&index=2", true)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", false)]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", false)]
    [InlineData("https://vimeo.com/channels/staffpicks", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPlaylistUrl_ShouldValidateCorrectly(string? url, bool expected)
    {
        var result = UrlValidator.IsPlaylistUrl(url);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", true)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", false)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", false)]
    public void IsPurePlaylistUrl_ShouldIdentifyPurePlaylists(string? url, bool expected)
    {
        var result = UrlValidator.IsPurePlaylistUrl(url);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", true)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9&index=4", true)]
    [InlineData("https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", false)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", false)]
    public void IsHybridUrl_ShouldIdentifyHybridLinks(string? url, bool expected)
    {
        var result = UrlValidator.IsHybridUrl(url);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractPlaylistId_ShouldExtractListParameter()
    {
        var url = "https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9&si=abcdef";
        var id = UrlValidator.ExtractPlaylistId(url);
        Assert.Equal("PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", id);
    }

    [Fact]
    public void ExtractPlaylistId_FromHybridUrl_ShouldExtractListParameter()
    {
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PL1234567890ABCDEF&index=1";
        var id = UrlValidator.ExtractPlaylistId(url);
        Assert.Equal("PL1234567890ABCDEF", id);
    }

    [Fact]
    public void NormalizePlaylistUrl_ShouldReturnCanonicalPlaylistUrl()
    {
        var url = "https://m.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9&index=1";
        var normalized = UrlValidator.NormalizePlaylistUrl(url);
        Assert.Equal("https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", normalized);
    }

    [Fact]
    public void IsValidYouTubeUrl_ShouldAcceptBothVideosAndPlaylists()
    {
        Assert.True(UrlValidator.IsValidYouTubeUrl("https://www.youtube.com/watch?v=dQw4w9WgXcQ"));
        Assert.True(UrlValidator.IsValidYouTubeUrl("https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9"));
        Assert.False(UrlValidator.IsValidYouTubeUrl("https://vimeo.com/12345"));
    }
}
