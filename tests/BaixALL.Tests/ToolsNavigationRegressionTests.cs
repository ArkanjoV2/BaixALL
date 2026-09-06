using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BaixALL.App.Models;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using Xunit;

namespace BaixALL.Tests;

public class ToolsNavigationRegressionTests : IDisposable
{
    private readonly string _testToolsDir;
    private readonly DependencyManager _dependencyManager;

    public ToolsNavigationRegressionTests()
    {
        _testToolsDir = Path.Combine(Path.GetTempPath(), "baixall_test_nav_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testToolsDir);
        _dependencyManager = new DependencyManager(null, null, _testToolsDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testToolsDir))
            {
                Directory.Delete(_testToolsDir, recursive: true);
            }
        }
        catch
        {
        }
    }

    private void CreateDummyBinaries()
    {
        var ytDir = Path.Combine(_testToolsDir, "yt-dlp");
        Directory.CreateDirectory(ytDir);
        File.WriteAllText(Path.Combine(ytDir, "yt-dlp.exe"), "dummy");

        var ffmpegDir = Path.Combine(_testToolsDir, "ffmpeg");
        Directory.CreateDirectory(ffmpegDir);
        File.WriteAllText(Path.Combine(ffmpegDir, "ffmpeg.exe"), "dummy");
        File.WriteAllText(Path.Combine(ffmpegDir, "ffprobe.exe"), "dummy");

        var denoDir = Path.Combine(_testToolsDir, "deno");
        Directory.CreateDirectory(denoDir);
        File.WriteAllText(Path.Combine(denoDir, "deno.exe"), "dummy");
    }

    private MainViewModel CreateMainViewModel()
    {
        var settings = new SettingsService();
        var history = new HistoryService();
        var ytDlp = new YtDlpService(_dependencyManager);
        var formatService = new FormatSelectionService();
        var ytService = new YoutubeService(ytDlp, formatService);
        var downloadService = new DownloadService(ytDlp, history, settings);
        var updateService = new UpdateService(_dependencyManager, downloadService);

        return new MainViewModel(
            ytService,
            formatService,
            downloadService,
            settings,
            _dependencyManager,
            updateService,
            history);
    }

    [Fact]
    public void DependenciesViewModel_InitializesWithAllFourTools()
    {
        var vm = new DependenciesViewModel(_dependencyManager);
        Assert.Equal(4, vm.Dependencies.Count);

        var toolNames = vm.Dependencies.Select(d => d.Name).ToList();
        Assert.Contains("yt-dlp", toolNames);
        Assert.Contains("FFmpeg", toolNames);
        Assert.Contains("ffprobe", toolNames);
        Assert.Contains("Deno", toolNames);
    }

    [Fact]
    public void DependenciesViewModel_StatusProperty_ReturnsExpectedValuesForStates()
    {
        var item = new DependencyItem { Name = "test-tool" };

        item.State = DependencyState.Installed;
        Assert.Equal("Pronto", item.Status);

        item.State = DependencyState.NotInstalled;
        Assert.Equal("Não instalado", item.Status);

        item.State = DependencyState.Checking;
        Assert.Equal("Verificando", item.Status);

        item.State = DependencyState.UpdateAvailable;
        Assert.Equal("Atualização disponível", item.Status);

        item.State = DependencyState.Error;
        Assert.Equal("Erro", item.Status);
    }

    [Fact]
    public void DependenciesViewModel_WhenAllInstalled_StatusTextIsClear()
    {
        foreach (var d in _dependencyManager.GetDependencies())
        {
            Directory.CreateDirectory(Path.GetDirectoryName(d.LocalPath)!);
            File.WriteAllText(d.LocalPath, "");
            d.SetInstalled("v1.0.0");
        }

        var vm = new DependenciesViewModel(_dependencyManager);

        Assert.True(vm.AreAllInstalled);
        Assert.Equal("Todas as ferramentas estão prontas.", vm.StatusText);
    }

    [Fact]
    public void MainViewModel_OpenToolsCommand_NavigatesToDependenciesTab()
    {
        var mainVm = CreateMainViewModel();

        Assert.Equal("Downloader", mainVm.CurrentTab);

        mainVm.OpenToolsCommand.Execute(null);

        Assert.Equal("Dependencies", mainVm.CurrentTab);
    }

    [Fact]
    public void MainViewModel_SwitchTabCommand_NavigatesCorrectly()
    {
        var mainVm = CreateMainViewModel();

        mainVm.SwitchTabCommand.Execute("Dependencies");
        Assert.Equal("Dependencies", mainVm.CurrentTab);

        mainVm.SwitchTabCommand.Execute("History");
        Assert.Equal("History", mainVm.CurrentTab);

        mainVm.SwitchTabCommand.Execute("Settings");
        Assert.Equal("Settings", mainVm.CurrentTab);

        mainVm.SwitchTabCommand.Execute("Downloader");
        Assert.Equal("Downloader", mainVm.CurrentTab);
    }

    [Fact]
    public void MainViewModel_SynchronizesDependenciesReadyWithDependenciesVm()
    {
        var mainVm = CreateMainViewModel();

        // Quando DependenciesVm reporta AreAllInstalled = true, MainViewModel sincroniza
        mainVm.DependenciesVm.AreAllInstalled = true;
        Assert.True(mainVm.DependenciesReady);

        // Quando reporta false, MainViewModel sincroniza
        mainVm.DependenciesVm.AreAllInstalled = false;
        Assert.False(mainVm.DependenciesReady);
    }
}
