using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BaixALL.App.Helpers;

public static class FileHelper
{
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();
    private static readonly string[] ReservedNames = new[]
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Sanitiza o nome de arquivo removendo caracteres inválidos do Windows e limitando o tamanho.
    /// </summary>
    public static string SanitizeFileName(string? fileName, string replacement = "_")
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "video";

        var clean = fileName.Trim();

        // Substitui caracteres inválidos
        foreach (var c in InvalidChars)
        {
            clean = clean.Replace(c.ToString(), replacement);
        }

        // Remove caracteres de controle ASCII adicionais
        clean = Regex.Replace(clean, @"[\x00-\x1f\x7f]", replacement);

        // Remove múltiplos substitutos seguidos
        if (!string.IsNullOrEmpty(replacement))
        {
            var pattern = Regex.Escape(replacement) + "+";
            clean = Regex.Replace(clean, pattern, replacement);
        }

        // Remove espaços e pontos no final (inválido no Windows)
        clean = clean.Trim('.', ' ');

        // Verifica nomes reservados do DOS/Windows
        var upper = clean.ToUpperInvariant();
        if (ReservedNames.Contains(upper))
        {
            clean = $"{clean}_file";
        }

        // Limita o tamanho do nome para evitar estouro de caminho no Windows (max 180 caracteres)
        if (clean.Length > 180)
        {
            clean = clean.Substring(0, 180).Trim('.', ' ');
        }

        return string.IsNullOrWhiteSpace(clean) ? "video" : clean;
    }

    /// <summary>
    /// Gera um caminho de arquivo único, evitando sobrescrita silenciosa.
    /// Exemplo: "video.mp4", "video (1).mp4", "video (2).mp4"
    /// </summary>
    public static string GetUniqueFilePath(string folder, string baseFileName, string extension)
    {
        EnsureDirectoryExists(folder);

        var sanitized = SanitizeFileName(baseFileName);
        var ext = extension.TrimStart('.');
        var candidate = Path.Combine(folder, $"{sanitized}.{ext}");

        if (!File.Exists(candidate))
            return candidate;

        var counter = 1;
        while (true)
        {
            candidate = Path.Combine(folder, $"{sanitized} ({counter}).{ext}");
            if (!File.Exists(candidate))
                return candidate;

            counter++;
        }
    }

    /// <summary>
    /// Garante que o diretório especificado existe.
    /// </summary>
    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Exclui um arquivo de forma segura, ignorando exceções se o arquivo estiver bloqueado ou não existir.
    /// </summary>
    public static bool SafeDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
        }
        catch
        {
            // Não interromper por falha de exclusão de arquivo temporário
        }

        return false;
    }
}
