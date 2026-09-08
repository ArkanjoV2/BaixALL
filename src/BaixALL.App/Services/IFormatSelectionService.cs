using System.Collections.Generic;
using System.Text.Json;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public interface IFormatSelectionService
{
    VideoInfo ParseVideoInfo(JsonDocument json, string originalUrl);
    List<FormatOption> BuildFormatOptions(VideoInfo info);
    PlaylistInfo ParsePlaylistInfo(JsonDocument json, string originalUrl);
    List<FormatOption> BuildBatchFormatOptions();
}
