using BaixALL.App.Models;

namespace BaixALL.App.Services;

public interface IPlatformService
{
    PlatformType DetectPlatform(string? url);
    bool IsSupportedUrl(string? url);
    string NormalizeUrl(string url, PlatformType platform);
    string? ExtractMediaId(string url, PlatformType platform);
    string GetCanonicalKey(string url, PlatformType platform, string? mediaId = null, int? subIndex = null);
    string GetPlatformDisplayName(PlatformType platform);
    string GetPlatformBadgeColor(PlatformType platform);
    string GetPlatformBackgroundColor(PlatformType platform);
}
