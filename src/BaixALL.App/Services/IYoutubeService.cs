using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public interface IYoutubeService
{
    Task<VideoInfo> AnalyzeVideoAsync(string url, CancellationToken ct = default);
}
