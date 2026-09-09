using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public interface IMediaAnalysisService
{
    Task<VideoInfo> AnalyzeVideoAsync(string url, CancellationToken ct = default);
    Task<PlaylistInfo> AnalyzeCollectionAsync(string url, CancellationToken ct = default);
    Task<MediaAnalysisResult> AnalyzeAsync(string url, CancellationToken ct = default);
}

public class MediaAnalysisResult
{
    public PlatformType Platform { get; init; }
    public bool IsCollection { get; init; }
    public bool IsHybrid { get; init; }
    public VideoInfo? Video { get; init; }
    public PlaylistInfo? Collection { get; init; }
}
