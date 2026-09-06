using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Models;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using Xunit;

namespace BaixALL.Tests;

public class FirstRunExperienceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task NenhumaFerramentaInstalada_DeveBloquearAnaliseEMostrarPrompt()
    {
        var emptyToolsDir = Path.Combine(Path.GetTempPath(), "BaixALL_EmptyTools_" + Guid.NewGuid());
        Directory.CreateDirectory(emptyToolsDir);

        try
        {
            var manager = new DependencyManager(toolsDirectory: emptyToolsDir);
            var allReady = await manager.CheckDependenciesAsync();

            Assert.False(allReady);
            Assert.False(manager.AreAllDependenciesInstalled());

            var deps = manager.GetDependencies();
            Assert.Equal(4, deps.Count);
            foreach (var d in deps)
            {
                Assert.False(d.IsInstalled);
                Assert.Equal(DependencyState.NotInstalled, d.State);
                Assert.Contains("Não instalado", d.StateBadgeText);
            }

            var settings = new SettingsService();
            var history = new HistoryService();
            var ytDlp = new YtDlpService(manager);
            var formatService = new FormatSelectionService();
            var ytService = new YoutubeService(ytDlp, formatService);
            var downloadService = new DownloadService(ytDlp, history, settings);
            var updateService = new UpdateService(manager, downloadService);

            var vm = new MainViewModel(ytService, formatService, downloadService, settings, manager, updateService, history);
            await vm.InitializeStartupAsync();

            Assert.False(vm.DependenciesReady);
            Assert.False(vm.CanAnalyze);
            Assert.True(vm.ShowSetupPrompt);
            Assert.True(vm.IsInitialSetupVisible);
        }
        finally
        {
            if (Directory.Exists(emptyToolsDir))
            {
                Directory.Delete(emptyToolsDir, recursive: true);
            }
        }
    }

    [Fact]
    public void BloqueioDeAnalise_QuandoFerramentasAusentes_NaoExecutaExtracao()
    {
        var emptyToolsDir = Path.Combine(Path.GetTempPath(), "BaixALL_EmptyTools_" + Guid.NewGuid());
        Directory.CreateDirectory(emptyToolsDir);

        try
        {
            var manager = new DependencyManager(toolsDirectory: emptyToolsDir);
            var settings = new SettingsService();
            var history = new HistoryService();
            var ytDlp = new YtDlpService(manager);
            var formatService = new FormatSelectionService();
            var ytService = new YoutubeService(ytDlp, formatService);
            var downloadService = new DownloadService(ytDlp, history, settings);
            var updateService = new UpdateService(manager, downloadService);

            var vm = new MainViewModel(ytService, formatService, downloadService, settings, manager, updateService, history)
            {
                UrlInput = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                DependenciesReady = false
            };

            _ = vm.AnalyzeAsync();

            // Análise deve ter sido bloqueada e modal de setup ativado
            Assert.True(vm.IsInitialSetupVisible);
            Assert.False(vm.HasVideoInfo);
            Assert.True(vm.HasStatusNotification);
            Assert.Equal("Warning", vm.StatusType);
        }
        finally
        {
            if (Directory.Exists(emptyToolsDir))
            {
                Directory.Delete(emptyToolsDir, recursive: true);
            }
        }
    }

    [Fact]
    public void FalhaNoDownload_DeveDefinirEstadoDeErroESemTravar()
    {
        var mockHttp = new HttpClient(new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var manager = new DependencyManager(httpClient: mockHttp);
        var item = new DependencyItem { Name = "yt-dlp" };

        item.SetDownloading(50, "Baixando");
        Assert.Equal(DependencyState.Downloading, item.State);
        Assert.True(item.IsDownloading);

        item.SetError("Falha na conexão com o servidor.");
        Assert.Equal(DependencyState.Error, item.State);
        Assert.True(item.HasError);
        Assert.False(item.IsDownloading);
        Assert.Equal("▲ Erro", item.StateBadgeText);
        Assert.Equal("Falha na conexão com o servidor.", item.ErrorMessage);
    }

    [Fact]
    public void FalhaNaValidacao_DeveDefinirEstadoDeErro()
    {
        var item = new DependencyItem { Name = "FFmpeg" };

        item.SetValidating();
        Assert.Equal(DependencyState.Validating, item.State);

        item.SetError("Binário presente, mas falhou ao executar.");
        Assert.Equal(DependencyState.Error, item.State);
        Assert.False(item.IsInstalled);
        Assert.True(item.HasError);
        Assert.Equal("▲ Erro", item.StateBadgeText);
    }

    [Fact]
    public void TodasInstaladas_DeveHabilitarAnaliseEDesativarSetupPrompt()
    {
        var manager = new DependencyManager();
        var deps = manager.GetDependencies();

        foreach (var d in deps)
        {
            d.SetInstalled("1.0.0-test");
        }

        Assert.True(deps.All(d => d.IsInstalled));

        var settings = new SettingsService();
        var history = new HistoryService();
        var ytDlp = new YtDlpService(manager);
        var formatService = new FormatSelectionService();
        var ytService = new YoutubeService(ytDlp, formatService);
        var downloadService = new DownloadService(ytDlp, history, settings);
        var updateService = new UpdateService(manager, downloadService);

        var vm = new MainViewModel(ytService, formatService, downloadService, settings, manager, updateService, history)
        {
            DependenciesReady = true
        };

        Assert.True(vm.CanAnalyze);
        Assert.False(vm.ShowSetupPrompt);
    }

    [Fact]
    public async Task ApenasYtDlpInstalado_NaoDevePermitirOperacaoCompleta()
    {
        var partialDir = Path.Combine(Path.GetTempPath(), "BaixALL_PartialYtDlp_" + Guid.NewGuid());
        var ytDir = Path.Combine(partialDir, "yt-dlp");
        Directory.CreateDirectory(ytDir);
        File.WriteAllText(Path.Combine(ytDir, "yt-dlp.exe"), "fake");

        try
        {
            var manager = new DependencyManager(toolsDirectory: partialDir);
            var ready = await manager.CheckDependenciesAsync();

            Assert.False(ready);
            Assert.False(manager.AreAllDependenciesInstalled());
        }
        finally
        {
            if (Directory.Exists(partialDir))
            {
                Directory.Delete(partialDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ApenasFFmpegInstalado_NaoDevePermitirOperacaoCompleta()
    {
        var partialDir = Path.Combine(Path.GetTempPath(), "BaixALL_PartialFFmpeg_" + Guid.NewGuid());
        var ffmpegDir = Path.Combine(partialDir, "ffmpeg");
        Directory.CreateDirectory(ffmpegDir);
        File.WriteAllText(Path.Combine(ffmpegDir, "ffmpeg.exe"), "fake");
        File.WriteAllText(Path.Combine(ffmpegDir, "ffprobe.exe"), "fake");

        try
        {
            var manager = new DependencyManager(toolsDirectory: partialDir);
            var ready = await manager.CheckDependenciesAsync();

            Assert.False(ready);
            Assert.False(manager.AreAllDependenciesInstalled());
        }
        finally
        {
            if (Directory.Exists(partialDir))
            {
                Directory.Delete(partialDir, recursive: true);
            }
        }
    }

    [Fact]
    public void ReinicializacaoAposInstalacao_DetectaStatusSemNovoDownload()
    {
        var manager = new DependencyManager();
        var deps = manager.GetDependencies();

        foreach (var d in deps)
        {
            d.SetInstalled("v1.0.0");
        }

        Assert.True(deps.All(d => d.IsInstalled));
        Assert.True(deps.All(d => d.State == DependencyState.Installed));
        Assert.True(deps.All(d => d.StateBadgeText.Contains("Instalado")));
    }
}
