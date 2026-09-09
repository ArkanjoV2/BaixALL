using BaixALL.App.Models;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class PlatformServiceTests
{
    private readonly PlatformService _service = new();

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", PlatformType.YouTube)]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", PlatformType.YouTube)]
    [InlineData("https://www.youtube.com/shorts/abcdefghijk", PlatformType.YouTube)]
    [InlineData("https://www.youtube.com/playlist?list=PL1234567890", PlatformType.YouTube)]
    [InlineData("https://www.instagram.com/reel/Chunk8-jurw/", PlatformType.Instagram)]
    [InlineData("https://instagram.com/p/aye83DjauH/?foo=bar#abc", PlatformType.Instagram)]
    [InlineData("https://www.instagram.com/marvelskies.fc/reel/CWqAgUZgCku/", PlatformType.Instagram)]
    [InlineData("https://www.instagram.com/tv/BkfuX9UB-eK/", PlatformType.Instagram)]
    [InlineData("https://x.com/captainamerica/status/719944021058060289", PlatformType.Twitter)]
    [InlineData("https://twitter.com/NASA/status/1735036375630545220?s=20", PlatformType.Twitter)]
    [InlineData("https://mobile.twitter.com/user/status/1234567890", PlatformType.Twitter)]
    [InlineData("https://x.com/i/status/987654321", PlatformType.Twitter)]
    [InlineData("https://tiktok.com/@user/video/123456", PlatformType.Unknown)]
    [InlineData("https://google.com", PlatformType.Unknown)]
    [InlineData("", PlatformType.Unknown)]
    [InlineData(null, PlatformType.Unknown)]
    public void DetectPlatform_IdentifiesPlatformCorrectly(string? url, PlatformType expected)
    {
        var actual = _service.DetectPlatform(url);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", true)]
    [InlineData("https://www.instagram.com/reel/Chunk8-jurw/", true)]
    [InlineData("https://www.instagram.com/p/BQ0eAlwhDrw/?igsh=123", true)]
    [InlineData("https://x.com/user/status/719944021058060289", true)]
    [InlineData("https://twitter.com/user/status/719944021058060289?t=abc", true)]
    [InlineData("https://www.instagram.com/direct/inbox/", false)]
    [InlineData("https://x.com/home", false)]
    [InlineData("https://random-site.org/video.mp4", false)]
    [InlineData("not a url", false)]
    public void IsSupportedUrl_ValidatesCorrectly(string url, bool expected)
    {
        var actual = _service.IsSupportedUrl(url);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExtractMediaId_ExtractsCorrectIdForPlatforms()
    {
        Assert.Equal("dQw4w9WgXcQ", _service.ExtractMediaId("https://www.youtube.com/watch?v=dQw4w9WgXcQ", PlatformType.YouTube));
        Assert.Equal("Chunk8-jurw", _service.ExtractMediaId("https://www.instagram.com/reel/Chunk8-jurw/?igsh=MWQ1", PlatformType.Instagram));
        Assert.Equal("BQ0eAlwhDrw", _service.ExtractMediaId("https://instagram.com/p/BQ0eAlwhDrw/", PlatformType.Instagram));
        Assert.Equal("719944021058060289", _service.ExtractMediaId("https://x.com/user/status/719944021058060289?s=20", PlatformType.Twitter));
    }

    [Fact]
    public void GetCanonicalKey_GeneratesNamespacedKeyWithoutCollisions()
    {
        var ytKey = _service.GetCanonicalKey("https://www.youtube.com/watch?v=12345678901", PlatformType.YouTube);
        var igKey = _service.GetCanonicalKey("https://www.instagram.com/reel/12345678901/", PlatformType.Instagram);
        var xKey = _service.GetCanonicalKey("https://x.com/user/status/12345678901", PlatformType.Twitter);

        Assert.Equal("yt:12345678901", ytKey);
        Assert.Equal("ig:12345678901", igKey);
        Assert.Equal("x:12345678901", xKey);

        // Nenhuma colisão mesmo se os IDs forem idênticos
        Assert.NotEqual(ytKey, igKey);
        Assert.NotEqual(igKey, xKey);
        Assert.NotEqual(ytKey, xKey);
    }

    [Fact]
    public void NormalizeUrl_NormalizesCorrectly()
    {
        var igReel = _service.NormalizeUrl("https://www.instagram.com/someuser/reel/Chunk8-jurw/?igsh=MWQ1", PlatformType.Instagram);
        Assert.Equal("https://www.instagram.com/reel/Chunk8-jurw/", igReel);

        var igPost = _service.NormalizeUrl("https://instagram.com/p/BQ0eAlwhDrw/?utm_source=copy", PlatformType.Instagram);
        Assert.Equal("https://www.instagram.com/p/BQ0eAlwhDrw/", igPost);

        var xPost = _service.NormalizeUrl("https://twitter.com/user/status/719944021058060289?s=20&t=123", PlatformType.Twitter);
        Assert.Equal("https://x.com/i/status/719944021058060289", xPost);
    }

    [Fact]
    public void GetCanonicalKey_MultiMediaCarousel_GeneratesUniqueKeysForEachVideo()
    {
        var postUrl = "https://www.instagram.com/p/BQ0eAlwhDrw/";

        // Mídia filha com ID próprio
        var key1 = _service.GetCanonicalKey(postUrl, PlatformType.Instagram, mediaId: "BQ0dSaohpPW");
        var key2 = _service.GetCanonicalKey(postUrl, PlatformType.Instagram, mediaId: "BQ0dTpOhuHT");
        Assert.NotEqual(key1, key2);
        Assert.Equal("ig:BQ0dSaohpPW", key1);
        Assert.Equal("ig:BQ0dTpOhuHT", key2);

        // Mídia compartilhando o mesmo ID de post com índice de sub-mídia
        var keyIdx1 = _service.GetCanonicalKey(postUrl, PlatformType.Instagram, mediaId: "BQ0eAlwhDrw", subIndex: 1);
        var keyIdx2 = _service.GetCanonicalKey(postUrl, PlatformType.Instagram, mediaId: "BQ0eAlwhDrw", subIndex: 2);
        Assert.NotEqual(keyIdx1, keyIdx2);
        Assert.Equal("ig:BQ0eAlwhDrw:idx:1", keyIdx1);
        Assert.Equal("ig:BQ0eAlwhDrw:idx:2", keyIdx2);
    }

    [Fact]
    public void GetCanonicalKey_EquivalentUrlsWithDifferentParameters_ProduceIdenticalKey()
    {
        var urlA = "https://x.com/captainamerica/status/719944021058060289?s=20";
        var urlB = "https://twitter.com/captainamerica/status/719944021058060289?t=abc&ref=twsrc";

        var keyA = _service.GetCanonicalKey(urlA, PlatformType.Twitter);
        var keyB = _service.GetCanonicalKey(urlB, PlatformType.Twitter);

        Assert.Equal("x:719944021058060289", keyA);
        Assert.Equal(keyA, keyB);
    }

    [Fact]
    public void GetCanonicalKey_FallbackWhenNoIdAvailable_UsesNormalizedUrlNamespace()
    {
        var url = "https://x.com/unknown/path";
        var key = _service.GetCanonicalKey(url, PlatformType.Twitter);
        Assert.StartsWith("twitter:", key);
    }
}
