using System;
using System.IO;

namespace BaixALL.App.Infrastructure;

/// <summary>
/// Centraliza nomes, caminhos e constantes da aplicação.
/// O nome BaixALL fica centralizado aqui para fácil alteração futura.
/// </summary>
public static class AppConstants
{
    public const string AppName = "BaixALL";
    public const string AppVersion = "1.0.0-rc.2";
    public const string AppTitle = "BaixALL - Baixar Vídeos do YouTube";
    public const string AppTagline = "Baixe seus vídeos na melhor qualidade disponível";

    public static readonly string LocalAppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName);

    public static readonly string ToolsFolder = Path.Combine(LocalAppDataFolder, "tools");
    public static readonly string LogsFolder = Path.Combine(LocalAppDataFolder, "logs");
    public static readonly string SettingsFilePath = Path.Combine(LocalAppDataFolder, "settings.json");
    public static readonly string HistoryFilePath = Path.Combine(LocalAppDataFolder, "history.json");

    public static readonly string YtDlpDir = Path.Combine(ToolsFolder, "yt-dlp");
    public static readonly string YtDlpExe = Path.Combine(YtDlpDir, "yt-dlp.exe");

    public static readonly string FFmpegDir = Path.Combine(ToolsFolder, "ffmpeg");
    public static readonly string FFmpegExe = Path.Combine(FFmpegDir, "ffmpeg.exe");
    public static readonly string FFprobeExe = Path.Combine(FFmpegDir, "ffprobe.exe");

    public static readonly string DenoDir = Path.Combine(ToolsFolder, "deno");
    public static readonly string DenoExe = Path.Combine(DenoDir, "deno.exe");

    public static readonly string DefaultDownloadFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads");

    // URLs oficiais de download
    public const string YtDlpDownloadUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
    public const string YtDlpApiLatestRelease = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";

    public const string FFmpegDownloadUrl = "https://github.com/yt-dlp/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-win64-gpl.zip";
    public const string FFmpegApiLatestRelease = "https://api.github.com/repos/yt-dlp/FFmpeg-Builds/releases/latest";

    public const string DenoDownloadUrl = "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";
    public const string DenoApiLatestRelease = "https://api.github.com/repos/denoland/deno/releases/latest";
}
