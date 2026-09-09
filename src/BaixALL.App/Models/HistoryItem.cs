using System;

namespace BaixALL.App.Models;

public class HistoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string FinalFilePath { get; set; } = string.Empty;
    public DateTime DownloadDate { get; set; } = DateTime.Now;
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = "Concluído";
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string CanonicalKey { get; set; } = string.Empty;

    public string FormattedDate => DownloadDate.ToString("dd/MM/yyyy HH:mm");

    public string PlatformDisplayName => string.IsNullOrWhiteSpace(Platform) ? "Desconhecido" : Platform;

    public string PlatformBadgeColor => PlatformDisplayName switch
    {
        "Instagram" => "#F472B6",
        "X / Twitter" or "Twitter" => "#38BDF8",
        "YouTube" => "#FF4444",
        _ => "#94A3B8"
    };

    public string PlatformBackgroundColor => PlatformDisplayName switch
    {
        "Instagram" => "#331424",
        "X / Twitter" or "Twitter" => "#0C2538",
        "YouTube" => "#331414",
        _ => "#1E293B"
    };
}
