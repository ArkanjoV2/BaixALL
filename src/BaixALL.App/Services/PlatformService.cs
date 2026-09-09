using System;
using System.Text.RegularExpressions;
using BaixALL.App.Helpers;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class PlatformService : IPlatformService
{
    private static readonly Regex YouTubeDomainRegex = new(
        @"^(?:https?:\/\/)?(?:www\.|m\.|music\.)?(?:youtube\.com|youtu\.be)\/",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InstagramDomainRegex = new(
        @"^(?:https?:\/\/)?(?:www\.)?instagram\.com\/",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TwitterDomainRegex = new(
        @"^(?:https?:\/\/)?(?:www\.|m(?:obile)?\.)?(?:x|twitter)\.com\/",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YouTubeVideoRegex = new(
        @"^(?:https?:\/\/)?(?:www\.|m\.|music\.)?(?:youtube\.com\/(?:watch\?(?:.*&)?v=|shorts\/|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YouTubePlaylistRegex = new(
        @"^(?:https?:\/\/)?(?:www\.|m\.|music\.)?youtube\.com\/playlist\?(?:.*&)?list=([a-zA-Z0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InstagramMediaRegex = new(
        @"^(?:https?:\/\/)?(?:www\.)?instagram\.com\/(?:(?:reel|reels|p|tv)\/|(?:[a-zA-Z0-9._]+)\/(?:reel|reels|p|tv)\/)([a-zA-Z0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TwitterStatusRegex = new(
        @"^(?:https?:\/\/)?(?:www\.|m(?:obile)?\.)?(?:x|twitter)\.com\/(?:(?:i|i\/web|[a-zA-Z0-9_]+)\/)?(?:status|statuses)\/(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PlatformType DetectPlatform(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return PlatformType.Unknown;

        var trimmed = url.Trim();
        if (YouTubeDomainRegex.IsMatch(trimmed))
            return PlatformType.YouTube;

        if (InstagramDomainRegex.IsMatch(trimmed))
            return PlatformType.Instagram;

        if (TwitterDomainRegex.IsMatch(trimmed))
            return PlatformType.Twitter;

        return PlatformType.Unknown;
    }

    public bool IsSupportedUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var platform = DetectPlatform(url);
        var trimmed = url.Trim();

        return platform switch
        {
            PlatformType.YouTube => UrlValidator.IsValidYouTubeUrl(trimmed),
            PlatformType.Instagram => InstagramMediaRegex.IsMatch(trimmed),
            PlatformType.Twitter => TwitterStatusRegex.IsMatch(trimmed),
            _ => false
        };
    }

    public string? ExtractMediaId(string url, PlatformType platform)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();

        switch (platform)
        {
            case PlatformType.YouTube:
                return UrlValidator.ExtractVideoId(trimmed) ?? UrlValidator.ExtractPlaylistId(trimmed);

            case PlatformType.Instagram:
                var igMatch = InstagramMediaRegex.Match(trimmed);
                return igMatch.Success && igMatch.Groups.Count > 1 ? igMatch.Groups[1].Value : null;

            case PlatformType.Twitter:
                var twMatch = TwitterStatusRegex.Match(trimmed);
                return twMatch.Success && twMatch.Groups.Count > 1 ? twMatch.Groups[1].Value : null;

            default:
                return null;
        }
    }

    public string NormalizeUrl(string url, PlatformType platform)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim();
        var id = ExtractMediaId(trimmed, platform);

        switch (platform)
        {
            case PlatformType.YouTube:
                if (UrlValidator.IsPurePlaylistUrl(trimmed))
                    return UrlValidator.NormalizePlaylistUrl(trimmed);
                return UrlValidator.NormalizeYouTubeUrl(trimmed);

            case PlatformType.Instagram:
                if (!string.IsNullOrEmpty(id))
                {
                    // Detecta se a URL original é explicitamente um reel
                    if (trimmed.Contains("/reel/", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("/reels/", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"https://www.instagram.com/reel/{id}/";
                    }
                    return $"https://www.instagram.com/p/{id}/";
                }
                return trimmed;

            case PlatformType.Twitter:
                if (!string.IsNullOrEmpty(id))
                {
                    return $"https://x.com/i/status/{id}";
                }
                return trimmed;

            default:
                return trimmed;
        }
    }

    public string GetCanonicalKey(string url, PlatformType platform, string? mediaId = null, int? subIndex = null)
    {
        var effectiveId = !string.IsNullOrWhiteSpace(mediaId)
            ? mediaId.Trim()
            : ExtractMediaId(url, platform);

        string baseKey;
        if (!string.IsNullOrWhiteSpace(effectiveId))
        {
            baseKey = platform switch
            {
                PlatformType.YouTube => $"yt:{effectiveId}",
                PlatformType.Instagram => $"ig:{effectiveId}",
                PlatformType.Twitter => $"x:{effectiveId}",
                _ => $"raw:{effectiveId}"
            };
        }
        else
        {
            var normalizedUrl = NormalizeUrl(url, platform).ToLowerInvariant();
            baseKey = $"{platform.ToString().ToLowerInvariant()}:{normalizedUrl}";
        }

        var postId = ExtractMediaId(url, platform);
        if (subIndex.HasValue && (string.IsNullOrWhiteSpace(mediaId) || string.Equals(mediaId, postId, StringComparison.OrdinalIgnoreCase)))
        {
            return $"{baseKey}:idx:{subIndex.Value}";
        }

        return baseKey;
    }

    public string GetPlatformDisplayName(PlatformType platform)
    {
        return platform switch
        {
            PlatformType.YouTube => "YouTube",
            PlatformType.Instagram => "Instagram",
            PlatformType.Twitter => "X / Twitter",
            _ => "Desconhecido"
        };
    }

    public string GetPlatformBadgeColor(PlatformType platform)
    {
        return platform switch
        {
            PlatformType.YouTube => "#FF4444",   // Vermelho YouTube
            PlatformType.Instagram => "#F472B6", // Rosa/Magenta Instagram
            PlatformType.Twitter => "#38BDF8",   // Azul Céu Twitter/X
            _ => "#94A3B8"
        };
    }

    public string GetPlatformBackgroundColor(PlatformType platform)
    {
        return platform switch
        {
            PlatformType.YouTube => "#331414",
            PlatformType.Instagram => "#331424",
            PlatformType.Twitter => "#0C2538",
            _ => "#1E293B"
        };
    }
}
