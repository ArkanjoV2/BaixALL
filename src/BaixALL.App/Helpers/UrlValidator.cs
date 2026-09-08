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
}
