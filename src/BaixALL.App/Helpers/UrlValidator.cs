using System;
using System.Text.RegularExpressions;

namespace BaixALL.App.Helpers;

public static class UrlValidator
{
    private static readonly Regex YouTubeRegex = new(
        @"^(?:https?:\/\/)?(?:www\.|m\.|music\.)?(?:youtube\.com\/(?:watch\?(?:.*&)?v=|shorts\/|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Verifica se a URL informada é uma URL válida do YouTube.
    /// </summary>
    public static bool IsValidYouTubeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        return YouTubeRegex.IsMatch(trimmed);
    }

    /// <summary>
    /// Extrai o ID do vídeo da URL, se válido.
    /// </summary>
    public static string? ExtractVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = YouTubeRegex.Match(url.Trim());
        return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Normaliza a URL para o formato canônico: https://www.youtube.com/watch?v={id}
    /// </summary>
    public static string NormalizeYouTubeUrl(string? url)
    {
        var id = ExtractVideoId(url);
        return id != null ? $"https://www.youtube.com/watch?v={id}" : (url?.Trim() ?? string.Empty);
    }
}
