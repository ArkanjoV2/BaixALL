using BaixALL.App.Infrastructure;

namespace BaixALL.App.Models;

public class AppSettings
{
    public string DownloadFolder { get; set; } = AppConstants.DefaultDownloadFolder;
    public int MaxConcurrentDownloads { get; set; } = 1;
    public string DefaultQuality { get; set; } = "best";
    public string DefaultContainer { get; set; } = "auto";
    public string DefaultAudioFormat { get; set; } = "best";
    public string Theme { get; set; } = "System"; // "System", "Dark", "Light"
    public bool AutoCheckUpdates { get; set; } = true;
    public bool AutoPasteClipboard { get; set; } = true;
}
