using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BaixALL.App.Infrastructure;
using BaixALL.App.Models;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class RealDependencyDownloadIntegrationTests
{
    [Fact]
    public async Task EnsureAllDependenciesAsync_RealExecution_DownloadsAndValidatesAllTools()
    {
        var manager = new DependencyManager();
        var progress = new Progress<(string ToolName, double Progress, string Status)>(report =>
        {
            Console.WriteLine($"[{report.ToolName}] {report.Progress:0}% - {report.Status}");
        });

        var success = await manager.EnsureAllDependenciesAsync(progress);

        Assert.True(success);
        Assert.True(manager.AreAllDependenciesInstalled());

        var deps = manager.GetDependencies();
        Assert.All(deps, d =>
        {
            Assert.True(d.IsInstalled);
            Assert.Equal(DependencyState.Installed, d.State);
            Assert.True(File.Exists(d.LocalPath));
            Assert.False(string.IsNullOrWhiteSpace(d.Version));
            Assert.NotEqual("Não instalado", d.Version);
            Assert.NotEqual("Erro de execução", d.Version);
        });
    }
}
