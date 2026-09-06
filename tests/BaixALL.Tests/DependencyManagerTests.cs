using System.IO;
using BaixALL.App.Infrastructure;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class DependencyManagerTests
{
    [Fact]
    public void DependencyManager_ShouldListRequiredTools()
    {
        var manager = new DependencyManager();
        var deps = manager.GetDependencies();

        Assert.Equal(4, deps.Count);
        Assert.Contains(deps, d => d.Name == "yt-dlp");
        Assert.Contains(deps, d => d.Name == "FFmpeg");
        Assert.Contains(deps, d => d.Name == "ffprobe");
        Assert.Contains(deps, d => d.Name == "Deno");
    }

    [Fact]
    public void DependencyManager_ShouldReturnExpectedPaths()
    {
        var manager = new DependencyManager();

        Assert.Equal(AppConstants.YtDlpExe, manager.GetYtDlpPath());
        Assert.Equal(AppConstants.FFmpegExe, manager.GetFFmpegPath());
        Assert.Equal(AppConstants.FFprobeExe, manager.GetFFprobePath());
        Assert.Equal(AppConstants.DenoExe, manager.GetDenoPath());
    }
}
