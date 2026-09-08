using System;

namespace BaixALL.App.Models;

public class BatchDownloadInfo
{
    public Guid BatchId { get; init; } = Guid.NewGuid();
    public string PlaylistTitle { get; init; } = string.Empty;
    public int TotalItems { get; init; }
    public FormatOption SelectedFormat { get; init; } = new();
    public ContainerOption SelectedContainer { get; init; } = new();
    public AudioFormatOption SelectedAudioFormat { get; init; } = new();
    public bool IsAudioOnly { get; init; }
    public string DestinationFolder { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}
