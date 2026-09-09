namespace BaixALL.App.Models;

public class DownloadProgressReport
{
    public long DownloadedBytes { get; set; }
    public long? TotalBytes { get; set; }
    public double SpeedBytesPerSec { get; set; }
    public double? EtaSeconds { get; set; }
    public double Percentage { get; set; }
    public DownloadStatus Status { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}

public class DownloadRequest
{
    public string VideoUrl { get; set; } = string.Empty;
    public string VideoTitle { get; set; } = string.Empty;
    public string DestinationFolder { get; set; } = string.Empty;
    public FormatOption Format { get; set; } = new();
    public ContainerOption Container { get; set; } = ContainerOption.DefaultOptions[0];
    public AudioFormatOption AudioFormat { get; set; } = AudioFormatOption.DefaultOptions[0];
    public bool IsAudioOnly { get; set; }
    public System.Guid? BatchId { get; set; }
    public string? BatchTitle { get; set; }
    public int? BatchIndex { get; set; }
    public int? BatchTotal { get; set; }
    public bool IsBatchItem => BatchId.HasValue;
    public PlatformType Platform { get; set; } = PlatformType.YouTube;
    public string CanonicalKey { get; set; } = string.Empty;
}
