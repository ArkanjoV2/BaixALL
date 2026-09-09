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
    public string Platform { get; set; } = "YouTube";
    public string CanonicalKey { get; set; } = string.Empty;

    public string FormattedDate => DownloadDate.ToString("dd/MM/yyyy HH:mm");
}
