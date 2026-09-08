using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public interface IYtDlpService
{
    Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default);
    Task<JsonDocument> GetPlaylistMetadataJsonAsync(string playlistUrl, CancellationToken ct = default);
    Task<string> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgressReport> progress,
        CancellationToken ct = default);
}
