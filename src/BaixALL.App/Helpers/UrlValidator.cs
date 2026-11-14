using System;
using System.Text.RegularExpressions;

namespace BaixALL.App.Helpers;

public static class UrlValidator
{
    private static readonly Regex YouTubeVideoRegex = new(
        @"^(?:https?:\/\/)?(?:www\.|m\.|music\.)?(?:youtube\.com\/(?:watch\?(?:.*&)?v=|shorts\/|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YouTubePlaylistRegex = new(
        @"^(?:https?:\/\/)?(?:www\.|m\.|music\.)?youtube\.com\/playlist\?(?:.*&)?list=([a-zA-Z0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GeneralListParamRegex = new(
        @"[?&]list=([a-zA-Z0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YouTubeDomainRegex = new(
        @"^(?:https?:\/\/)?(?:www\.|m\.|music\.)?(?:youtube\.com|youtu\.be)\/",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TikTokDomainRegex = new(
        @"^(?:https?:\/\/)?(?:(?:www|m|vm|vt|t)\.)?tiktok(?:v)?\.com\/",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TikTokVideoRegex = new(
        @"^(?:https?:\/\/)?(?:(?:www|m|t)\.)?tiktok(?:v)?\.com\/(?:@([a-zA-Z0-9_.-]*)\/video|(?:i18n\/)?share\/video|v|embed(?:\/v2)?)\/(\d+)(?:\.html)?(?:[\/?#]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TikTokShortLinkRegex = new(
        @"^(?:https?:\/\/)?(vm\.tiktok\.com|vt\.tiktok\.com|(?:(?:www|m)\.)?tiktok\.com\/t)\/([a-zA-Z0-9_-]{3,})(?:[\/?#]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TikTokPhotoRegex = new(
        @"^(?:https?:\/\/)?(?:(?:www|m|t)\.)?tiktok(?:v)?\.com\/@(?:[a-zA-Z0-9_.-]*)\/photo\/(\d+)(?:[\/?#]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Verifica se a URL informada é uma URL válida do YouTube (vídeo ou playlist).
    /// </summary>
    public static bool IsValidYouTubeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        return IsValidVideoUrl(trimmed) || IsPlaylistUrl(trimmed);
    }

    /// <summary>
    /// Verifica se a URL informada é uma URL válida de vídeo individual do YouTube.
    /// </summary>
    public static bool IsValidVideoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        return YouTubeVideoRegex.IsMatch(trimmed);
    }

    /// <summary>
    /// Verifica se a URL informada aponta para uma playlist do YouTube (pura ou contendo o parâmetro list=).
    /// </summary>
    public static bool IsPlaylistUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        if (!YouTubeDomainRegex.IsMatch(trimmed))
            return false;

        return GeneralListParamRegex.IsMatch(trimmed);
    }

    /// <summary>
    /// Verifica se a URL é exclusivamente uma playlist (sem ID de vídeo específico associado).
    /// </summary>
    public static bool IsPurePlaylistUrl(string? url)
    {
        return IsPlaylistUrl(url) && ExtractVideoId(url) == null;
    }

    /// <summary>
    /// Verifica se a URL é híbrida (contém tanto o ID do vídeo quanto o ID da playlist).
    /// </summary>
    public static bool IsHybridUrl(string? url)
    {
        return ExtractVideoId(url) != null && ExtractPlaylistId(url) != null;
    }

    /// <summary>
    /// Extrai o ID do vídeo da URL, se válido.
    /// </summary>
    public static string? ExtractVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = YouTubeVideoRegex.Match(url.Trim());
        return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Extrai o ID da playlist da URL, se presente.
    /// </summary>
    public static string? ExtractPlaylistId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();
        if (!YouTubeDomainRegex.IsMatch(trimmed))
            return null;

        var match = GeneralListParamRegex.Match(trimmed);
        return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Normaliza a URL de vídeo para o formato canônico: https://www.youtube.com/watch?v={id}
    /// </summary>
    public static string NormalizeYouTubeUrl(string? url)
    {
        var id = ExtractVideoId(url);
        return id != null ? $"https://www.youtube.com/watch?v={id}" : (url?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// Normaliza a URL de playlist para o formato canônico: https://www.youtube.com/playlist?list={id}
    /// </summary>
    public static string NormalizePlaylistUrl(string? url)
    {
        var listId = ExtractPlaylistId(url);
        return listId != null ? $"https://www.youtube.com/playlist?list={listId}" : (url?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// Verifica se a URL pertence a um domínio do TikTok.
    /// </summary>
    public static bool IsTikTokDomain(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return TikTokDomainRegex.IsMatch(url.Trim());
    }

    /// <summary>
    /// Verifica se a URL informada é uma URL válida de vídeo individual do TikTok (canônica ou link curto/compartilhado).
    /// </summary>
    public static bool IsValidTikTokUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        return IsValidTikTokVideoUrl(trimmed) || IsTikTokShortUrl(trimmed);
    }

    /// <summary>
    /// Verifica se a URL informada é uma URL canônica ou direta de vídeo do TikTok (contendo ID numérico do vídeo).
    /// </summary>
    public static bool IsValidTikTokVideoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return TikTokVideoRegex.IsMatch(url.Trim());
    }

    /// <summary>
    /// Verifica se a URL é um link curto/compartilhado do TikTok (vm.tiktok.com, vt.tiktok.com, tiktok.com/t/).
    /// </summary>
    public static bool IsTikTokShortUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return TikTokShortLinkRegex.IsMatch(url.Trim());
    }

    /// <summary>
    /// Verifica se a URL aponta explicitamente para uma publicação de fotos/slideshow do TikTok (/photo/).
    /// </summary>
    public static bool IsTikTokPhotoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return TikTokPhotoRegex.IsMatch(url.Trim());
    }

    /// <summary>
    /// Extrai o ID numérico do vídeo do TikTok quando presente na URL.
    /// </summary>
    public static string? ExtractTikTokVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = TikTokVideoRegex.Match(url.Trim());
        return match.Success && match.Groups.Count > 2 ? match.Groups[2].Value : null;
    }

    /// <summary>
    /// Extrai o identificador do usuário (@handle) da URL do TikTok, quando presente.
    /// </summary>
    public static string? ExtractTikTokUsername(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = TikTokVideoRegex.Match(url.Trim());
        if (match.Success && match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
        {
            return match.Groups[1].Value;
        }
        return null;
    }

    /// <summary>
    /// Normaliza URLs do TikTok removendo parâmetros de rastreamento e padronizando links canônicos e curtos.
    /// </summary>
    public static string NormalizeTikTokUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim();

        var videoMatch = TikTokVideoRegex.Match(trimmed);
        if (videoMatch.Success && videoMatch.Groups.Count > 2)
        {
            var user = videoMatch.Groups[1].Value;
            var videoId = videoMatch.Groups[2].Value;
            var effectiveUser = !string.IsNullOrWhiteSpace(user) ? user : "_";
            return $"https://www.tiktok.com/@{effectiveUser}/video/{videoId}";
        }

        var shortMatch = TikTokShortLinkRegex.Match(trimmed);
        if (shortMatch.Success && shortMatch.Groups.Count > 2)
        {
            var hostOrPrefix = shortMatch.Groups[1].Value.ToLowerInvariant();
            var code = shortMatch.Groups[2].Value;
            if (hostOrPrefix.StartsWith("vm.", StringComparison.OrdinalIgnoreCase))
                return $"https://vm.tiktok.com/{code}/";
            if (hostOrPrefix.StartsWith("vt.", StringComparison.OrdinalIgnoreCase))
                return $"https://vt.tiktok.com/{code}/";
            return $"https://www.tiktok.com/t/{code}/";
        }

        return trimmed;
    }
}
