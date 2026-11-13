using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Helpers;
using BaixALL.App.Infrastructure;
using BaixALL.App.Models;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using Xunit;

namespace BaixALL.Tests;

public class TikTokSupportTests
{
    private readonly PlatformService _platformService = new();
    private readonly FormatSelectionService _formatService = new();

    private static string GetFixturePath(string fileName)
    {
        var localPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        if (File.Exists(localPath)) return localPath;

        var sourcePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", fileName);
        if (File.Exists(sourcePath)) return sourcePath;

        throw new FileNotFoundException($"Fixture '{fileName}' não encontrada.");
    }

    // 1. Reconhecimento de URLs canônicas do TikTok
    [Theory]
    [InlineData("https://www.tiktok.com/@tiktok/video/7707063215498399006", "7707063215498399006")]
    [InlineData("https://tiktok.com/@patroxofficial/video/6742501081818877190", "6742501081818877190")]
    [InlineData("https://m.tiktok.com/@user.name_1/video/7059698374567611694?is_from_webapp=1&sender_device=pc", "7059698374567611694")]
    [InlineData("https://www.tiktok.com/@/video/7508455561915043103", "7508455561915043103")]
    [InlineData("https://www.tiktok.com/share/video/7508455561915043103", "7508455561915043103")]
    [InlineData("https://m.tiktok.com/v/7508455561915043103.html", "7508455561915043103")]
    [InlineData("https://www.tiktok.com/embed/v2/7508455561915043103", "7508455561915043103")]
    public void CanonicalTikTokUrls_AreRecognizedAndExtractVideoId(string url, string expectedVideoId)
    {
        Assert.Equal(PlatformType.TikTok, _platformService.DetectPlatform(url));
        Assert.True(_platformService.IsSupportedUrl(url));
        Assert.True(UrlValidator.IsValidTikTokUrl(url));
        Assert.True(UrlValidator.IsValidTikTokVideoUrl(url));
        Assert.Equal(expectedVideoId, _platformService.ExtractMediaId(url, PlatformType.TikTok));
    }

    // 2. Reconhecimento de URLs curtas
    [Theory]
    [InlineData("https://vm.tiktok.com/ZMS6nWQFF/")]
    [InlineData("https://vm.tiktok.com/ZSHtcGLbeqMuS-DYKPF/")]
    [InlineData("https://vt.tiktok.com/ZS8abcdef/")]
    [InlineData("https://vt.tiktok.com/ZS8abcdef?_r=1")]
    [InlineData("https://www.tiktok.com/t/ZTR45GpSF/")]
    [InlineData("https://tiktok.com/t/ZTR45GpSF")]
    public void ShortTikTokUrls_AreRecognizedAsSupported(string shortUrl)
    {
        Assert.Equal(PlatformType.TikTok, _platformService.DetectPlatform(shortUrl));
        Assert.True(_platformService.IsSupportedUrl(shortUrl));
        Assert.True(UrlValidator.IsValidTikTokUrl(shortUrl));
        Assert.True(UrlValidator.IsTikTokShortUrl(shortUrl));
        Assert.Null(_platformService.ExtractMediaId(shortUrl, PlatformType.TikTok));
    }

    // 3. Redirecionamento, normalização de links compartilhados e rejeição de escopos não suportados
    [Fact]
    public void NormalizeUrl_StandardizesCanonicalAndShortTikTokUrls()
    {
        var canonicalWithParams = "https://www.tiktok.com/@tiktok/video/7707063215498399006?is_from_webapp=1&sender_device=pc";
        Assert.Equal("https://www.tiktok.com/@tiktok/video/7707063215498399006", _platformService.NormalizeUrl(canonicalWithParams, PlatformType.TikTok));

        var shareUrl = "https://www.tiktok.com/share/video/7707063215498399006?_r=1";
        Assert.Equal("https://www.tiktok.com/@_/video/7707063215498399006", _platformService.NormalizeUrl(shareUrl, PlatformType.TikTok));

        var vmShortWithParams = "https://vm.tiktok.com/ZSHtcGLbeqMuS-DYKPF/?_r=1&utm_source=copy";
        Assert.Equal("https://vm.tiktok.com/ZSHtcGLbeqMuS-DYKPF/", _platformService.NormalizeUrl(vmShortWithParams, PlatformType.TikTok));

        var vtShort = "https://vt.tiktok.com/ZS8abcdef?is_from_webapp=1";
        Assert.Equal("https://vt.tiktok.com/ZS8abcdef/", _platformService.NormalizeUrl(vtShort, PlatformType.TikTok));

        var tShort = "https://tiktok.com/t/ZTR45GpSF?foo=bar";
        Assert.Equal("https://www.tiktok.com/t/ZTR45GpSF/", _platformService.NormalizeUrl(tShort, PlatformType.TikTok));
    }

    [Theory]
    [InlineData("https://www.tiktok.com/@tiktok")]
    [InlineData("https://www.tiktok.com/@tiktok/")]
    [InlineData("https://www.tiktok.com/@tiktok/live")]
    [InlineData("https://www.tiktok.com/@tiktok/photo/7200000000000000001")]
    [InlineData("https://www.tiktok.com/@tiktok/story/7200000000000000001")]
    [InlineData("https://www.tiktok.com/tag/music")]
    [InlineData("https://www.tiktok.com/music/som-original-123456")]
    public void UnsupportedTikTokScopes_ProfileLivePhotoStory_AreRejected(string unsupportedUrl)
    {
        Assert.Equal(PlatformType.TikTok, _platformService.DetectPlatform(unsupportedUrl));
        Assert.False(_platformService.IsSupportedUrl(unsupportedUrl));
    }

    // 4. Identificação da plataforma e chaves canônicas sem colisão
    [Fact]
    public void PlatformIdentification_PropertiesAndCanonicalKeys_AreDistinctAndAccurate()
    {
        Assert.Equal("TikTok", _platformService.GetPlatformDisplayName(PlatformType.TikTok));
        Assert.Equal("#25F4EE", _platformService.GetPlatformBadgeColor(PlatformType.TikTok));
        Assert.Equal("#112A30", _platformService.GetPlatformBackgroundColor(PlatformType.TikTok));

        var sameId = "77070632154";
        var ytKey = _platformService.GetCanonicalKey($"https://www.youtube.com/watch?v={sameId}", PlatformType.YouTube);
        var igKey = _platformService.GetCanonicalKey($"https://www.instagram.com/reel/{sameId}/", PlatformType.Instagram);
        var xKey = _platformService.GetCanonicalKey($"https://x.com/user/status/{sameId}", PlatformType.Twitter);
        var ttKey = _platformService.GetCanonicalKey($"https://www.tiktok.com/@user/video/{sameId}", PlatformType.TikTok);

        Assert.Equal($"tt:{sameId}", ttKey);
        Assert.NotEqual(ttKey, ytKey);
        Assert.NotEqual(ttKey, igKey);
        Assert.NotEqual(ttKey, xKey);
    }

    // 5. Extração dos metadados e resolução de link curto para chave canônica definitiva
    [Fact]
    public void ParseVideoInfo_ExtractsTikTokMetadataAndResolvesShortUrlCanonicalKey()
    {
        var jsonPath = GetFixturePath("tiktok_video_multi_res.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));

        // Simula análise iniciada a partir de um link curto vm.tiktok.com
        var info = _formatService.ParseVideoInfo(doc, "https://vm.tiktok.com/ZMS6nWQFF/");

        Assert.Equal("7707063215498399006", info.Id);
        Assert.Equal(PlatformType.TikTok, info.Platform);
        Assert.Equal("tt:7707063215498399006", info.CanonicalKey);
        Assert.Equal("@tiktok", info.Channel);
        Assert.Contains("that song you just discovered on TikTok", info.Title);
        Assert.Contains("Kenny Loggins", info.Description);
        Assert.Equal(38, info.DurationSeconds);
        Assert.Equal("0:38", info.FormattedDuration);
        Assert.Equal("10/11/2026", info.UploadDate);
        Assert.Equal("1080p (Full HD) Vertical (1080x1920)", info.MaxResolution);
        Assert.Equal(WatermarkStatus.NoWatermark, info.WatermarkStatus);
        Assert.True(info.HasCleanSource);
        Assert.True(info.HasWatermarkBadge);
        Assert.Equal("✓ Sem marca-d'água", info.WatermarkStatusText);
    }

    // 6 & 7 & 10. Seleção de fonte sem marca-d'água, exclusão de watermarked/bytevc2 e preservação de áudio e vídeo
    [Fact]
    public void BuildFormatOptions_WhenCleanSourcesExist_PrioritizesNoWatermarkAndExcludesWatermarkedAndUnplayable()
    {
        var jsonPath = GetFixturePath("tiktok_video_multi_res.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var info = _formatService.ParseVideoInfo(doc, "https://www.tiktok.com/@tiktok/video/7707063215498399006");

        // Confirma classificação individual dos formatos brutos
        var downloadFmt = info.Formats.Single(f => f.FormatId == "download");
        Assert.Equal(WatermarkStatus.Watermarked, downloadFmt.WatermarkStatus);

        var bytevc2Fmt = info.Formats.Single(f => f.FormatId.StartsWith("bytevc2_"));
        Assert.True(bytevc2Fmt.IsUnplayable);
        Assert.False(bytevc2Fmt.HasVideo);

        var clean1080 = info.Formats.Single(f => f.FormatId.StartsWith("bytevc1_1080p_"));
        Assert.Equal(WatermarkStatus.NoWatermark, clean1080.WatermarkStatus);
        Assert.True(clean1080.HasVideo);
        Assert.True(clean1080.HasAudio);

        var options = _formatService.BuildFormatOptions(info);

        // Deve conter: Melhor qualidade + 3 resoluções (1920, 1280, 1024) + Somente Áudio = 5 opções
        Assert.Equal(5, options.Count);

        var bestOpt = options.First();
        Assert.True(bestOpt.IsBestQuality);
        Assert.Equal(WatermarkStatus.NoWatermark, bestOpt.WatermarkStatus);
        Assert.False(bestOpt.RequiresWatermarkConsent);
        Assert.Contains("Sem marca-d'água", bestOpt.Label);
        Assert.Contains("1080x1920", bestOpt.Label);

        // Verifica que todas as opções de vídeo excluem download, watermark e bytevc2 e não possuem '/best' solto
        var videoOptions = options.Where(o => !o.IsAudioOnly).ToList();
        Assert.Equal(4, videoOptions.Count);
        foreach (var opt in videoOptions)
        {
            Assert.Equal(WatermarkStatus.NoWatermark, opt.WatermarkStatus);
            Assert.Contains("format_id!=download", opt.FormatSelector);
            Assert.Contains("format_note!*=?watermark", opt.FormatSelector);
            Assert.Contains("vcodec!=bytevc2", opt.FormatSelector);
            Assert.Contains("acodec!=none", opt.FormatSelector);
            Assert.Contains("+bestaudio", opt.FormatSelector);
            Assert.DoesNotContain("/best", opt.FormatSelector.Replace("/bestvideo", "").Replace("/best[", ""));
        }

        // Verifica ordem decrescente de resoluções (1080x1920 -> 720x1280 -> 576x1024)
        Assert.Equal(1920, videoOptions[1].Height);
        Assert.Equal(1280, videoOptions[2].Height);
        Assert.Equal(1024, videoOptions[3].Height);
    }

    // 8. Comportamento quando não existe fonte limpa (apenas watermarked disponível)
    [Fact]
    public async Task WhenOnlyWatermarkedSourceExists_DoesNotAutoSelectNorSilentFallback_AndRequiresExplicitConsent()
    {
        var jsonPath = GetFixturePath("tiktok_video_only_watermarked.json");
        var rawJson = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(rawJson);

        var info = _formatService.ParseVideoInfo(doc, "https://www.tiktok.com/@patroxofficial/video/6742501081818877190");

        Assert.Equal(WatermarkStatus.Watermarked, info.WatermarkStatus);
        Assert.False(info.HasCleanSource);
        Assert.True(info.HasOnlyWatermarkedSource);
        Assert.Equal("⚠ Apenas com marca-d'água", info.WatermarkStatusText);
        Assert.Equal("Patrox (@patroxofficial)", info.Channel);

        var options = _formatService.BuildFormatOptions(info);

        // Nenhuma opção de vídeo pode ser marcada como IsBestQuality nem rotulada como "Sem marca-d'água"
        Assert.DoesNotContain(options, o => o.IsBestQuality);
        Assert.DoesNotContain(options, o => o.Label.Contains("Sem marca-d'água", StringComparison.OrdinalIgnoreCase));

        var wmOption = options.Single(o => !o.IsAudioOnly);
        Assert.Equal(WatermarkStatus.Watermarked, wmOption.WatermarkStatus);
        Assert.True(wmOption.RequiresWatermarkConsent);
        Assert.Contains("com marca-d'água", wmOption.Label, StringComparison.OrdinalIgnoreCase);

        // Testa o fluxo completo na MainViewModel: não seleciona automaticamente e bloqueia download sem consentimento
        var tempDir = Path.Combine(Path.GetTempPath(), $"baixall_tt_wm_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var mockYtDlp = new FakeTikTokYtDlpService(rawJson, tempDir);
            var settingsSvc = new FakeTikTokSettingsService(tempDir);
            var historySvc = new HistoryService(null, Path.Combine(tempDir, "history.json"));
            var dispatcher = new SyncDispatcherService();
            var downloadSvc = new DownloadService(mockYtDlp, historySvc, settingsSvc, dispatcher);
            var analysisSvc = new MediaAnalysisService(mockYtDlp, _formatService, _platformService);
            var depMgr = new FakeTikTokDependencyManager();
            var updateSvc = new FakeTikTokUpdateService();

            var vm = new MainViewModel(
                analysisSvc,
                _formatService,
                downloadSvc,
                settingsSvc,
                depMgr,
                updateSvc,
                historySvc,
                dispatcher,
                null,
                _platformService,
                analysisSvc)
            {
                DependenciesReady = true,
                DestinationFolder = tempDir,
                UrlInput = "https://www.tiktok.com/@patroxofficial/video/6742501081818877190"
            };

            await vm.AnalyzeAsync();

            Assert.True(vm.HasVideoInfo);
            Assert.Null(vm.SelectedFormat); // Não selecionou automaticamente formato com marca-d'água!
            Assert.True(vm.ShowWatermarkWarning);
            Assert.Equal("Warning", vm.StatusType);

            // Tentar baixar sem consentimento explícito deve bloquear e NÃO enfileirar
            vm.StartDownloadCommand.Execute(null);
            Assert.Empty(downloadSvc.QueueItems);

            // Mesmo selecionando a opção manualmente no dropdown sem marcar AllowWatermarkedDownload deve bloquear
            vm.SelectedFormat = wmOption;
            vm.StartDownloadCommand.Execute(null);
            Assert.Empty(downloadSvc.QueueItems);

            // Quando o usuário autoriza explicitamente AllowWatermarkedDownload = true, o download é permitido
            vm.AllowWatermarkedDownload = true;
            vm.StartDownloadCommand.Execute(null);
            Assert.Single(downloadSvc.QueueItems);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    // 9. Comportamento quando o estado da marca é desconhecido
    [Fact]
    public void BuildFormatOptions_WhenWatermarkStatusIsUnknown_ClassifiesAsUnknownWithoutPromisingCleanVideo()
    {
        var jsonPath = GetFixturePath("tiktok_video_unknown_watermark.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));

        var info = _formatService.ParseVideoInfo(doc, "https://www.tiktok.com/@creator_unknown_wm/video/7059698374567611694");

        Assert.Equal(WatermarkStatus.Unknown, info.WatermarkStatus);
        Assert.False(info.HasCleanSource);
        Assert.False(info.HasOnlyWatermarkedSource);
        Assert.Equal("• Marca-d'água não verificada", info.WatermarkStatusText);

        var options = _formatService.BuildFormatOptions(info);
        var videoOptions = options.Where(o => !o.IsAudioOnly).ToList();

        Assert.NotEmpty(videoOptions);
        foreach (var opt in videoOptions)
        {
            Assert.Equal(WatermarkStatus.Unknown, opt.WatermarkStatus);
            Assert.Contains("Marca-d'água não verificada", opt.Label);
            Assert.DoesNotContain("Sem marca-d'água", opt.Label, StringComparison.OrdinalIgnoreCase);
        }
    }

    // 11 & 12. Registro no histórico, fila, cancelamento e concorrência
    [Fact]
    public async Task DownloadAndHistory_RegistersTikTokCreatorPlatformAndSupportsConcurrencyAndCancellation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"baixall_tt_queue_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var jsonPath = GetFixturePath("tiktok_video_multi_res.json");
            var rawJson = File.ReadAllText(jsonPath);

            var mockYtDlp = new FakeTikTokYtDlpService(rawJson, tempDir);
            var settingsSvc = new FakeTikTokSettingsService(tempDir) { MaxConcurrent = 2 };
            var historyPath = Path.Combine(tempDir, "history.json");
            var historySvc = new HistoryService(null, historyPath);
            var dispatcher = new SyncDispatcherService();
            var downloadSvc = new DownloadService(mockYtDlp, historySvc, settingsSvc, dispatcher);

            using var doc = JsonDocument.Parse(rawJson);
            var info = _formatService.ParseVideoInfo(doc, "https://www.tiktok.com/@tiktok/video/7707063215498399006");
            var bestFormat = _formatService.BuildFormatOptions(info).First(o => o.IsBestQuality);

            var req1 = new DownloadRequest
            {
                VideoId = info.Id,
                VideoUrl = info.OriginalUrl,
                VideoTitle = info.Title,
                Channel = info.Channel,
                DestinationFolder = tempDir,
                Format = bestFormat,
                Platform = PlatformType.TikTok,
                CanonicalKey = info.CanonicalKey
            };

            var item1 = downloadSvc.EnqueueDownload(req1, info.ThumbnailUrl);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && !item1.IsCompleted)
            {
                await Task.Delay(20);
            }

            Assert.True(item1.IsCompleted);
            Assert.Equal("TikTok", item1.PlatformDisplayName);
            Assert.Equal("#25F4EE", item1.PlatformBadgeColor);

            var historyItems = historySvc.GetHistory();
            Assert.Single(historyItems);
            var recorded = historyItems[0];
            Assert.Equal("TikTok", recorded.Platform);
            Assert.Equal("7707063215498399006", recorded.VideoId);
            Assert.Equal("@tiktok", recorded.Channel);
            Assert.Equal("tt:7707063215498399006", recorded.CanonicalKey);
            Assert.Equal("MP4", recorded.Format);
            Assert.True(File.Exists(recorded.FinalFilePath));

            // Verifica inferência legada de histórico para TikTok
            var legacyByKey = new HistoryItem { CanonicalKey = "tt:7707063215498399006" };
            Assert.Equal("TikTok", HistoryService.InferPlatform(legacyByKey));

            var legacyByThumb = new HistoryItem { ThumbnailUrl = "https://p16-common-sign.tiktokcdn.com/cover.jpeg" };
            Assert.Equal("TikTok", HistoryService.InferPlatform(legacyByThumb));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task TikTokQueue_DeduplicatesShortAndCanonicalUrls_AndRespectsConcurrencyAndCancellation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"baixall_tt_conc_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var jsonPath = GetFixturePath("tiktok_video_multi_res.json");
            var rawJson = File.ReadAllText(jsonPath);

            var mockYtDlp = new FakeTikTokYtDlpService(rawJson, tempDir) { SimulatedDelayMs = 400 };
            var settingsSvc = new FakeTikTokSettingsService(tempDir);
            settingsSvc.Settings.MaxConcurrentDownloads = 1;

            var historySvc = new HistoryService(null, Path.Combine(tempDir, "history.json"));
            var dispatcher = new SyncDispatcherService();
            var downloadSvc = new DownloadService(mockYtDlp, historySvc, settingsSvc, dispatcher);
            var analysisSvc = new MediaAnalysisService(mockYtDlp, _formatService, _platformService);
            var depMgr = new FakeTikTokDependencyManager();
            var updateSvc = new FakeTikTokUpdateService();

            var vm = new MainViewModel(
                analysisSvc,
                _formatService,
                downloadSvc,
                settingsSvc,
                depMgr,
                updateSvc,
                historySvc,
                dispatcher,
                null,
                _platformService,
                analysisSvc)
            {
                DependenciesReady = true,
                DestinationFolder = tempDir
            };

            // 1. Analisa pela URL canônica e adiciona à fila
            vm.UrlInput = "https://www.tiktok.com/@tiktok/video/7707063215498399006";
            await vm.AnalyzeAsync();
            vm.StartDownloadCommand.Execute(null);
            Assert.Single(downloadSvc.QueueItems);

            // 2. Analisa o MESMO vídeo por link curto vm.tiktok.com e tenta enfileirar: deve detectar duplicata tt:7707063215498399006
            vm.UrlInput = "https://vm.tiktok.com/ZMS6nWQFF/";
            await vm.AnalyzeAsync();
            Assert.Equal("tt:7707063215498399006", vm.VideoInfo!.CanonicalKey);
            vm.StartDownloadCommand.Execute(null);
            Assert.Single(downloadSvc.QueueItems); // Permanece 1 item (duplicata bloqueada)

            // 3. Enfileira um segundo vídeo distinto do TikTok com concorrência = 1 e cancela o primeiro
            var item2 = downloadSvc.EnqueueDownload(new DownloadRequest
            {
                VideoId = "7547418159247117575",
                VideoUrl = "https://www.tiktok.com/@user2/video/7547418159247117575",
                VideoTitle = "Segundo Vídeo TikTok",
                DestinationFolder = tempDir,
                Platform = PlatformType.TikTok,
                CanonicalKey = "tt:7547418159247117575"
            }, "");

            var item1 = downloadSvc.QueueItems.Last();
            downloadSvc.CancelDownload(item1.Id);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && !item2.IsCompleted)
            {
                await Task.Delay(20);
            }

            Assert.True(item1.IsCanceled);
            Assert.True(item2.IsCompleted);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    // 13. Tratamento amigável de vídeos privados, removidos, bloqueados por IP, links expirados e slideshows de fotos
    [Fact]
    public async Task MediaAnalysisService_WhenTikTokPostIsPhotoSlideshow_ThrowsFriendlyException()
    {
        var jsonPath = GetFixturePath("tiktok_slideshow_audio_only.json");
        var rawJson = File.ReadAllText(jsonPath);
        var mockYtDlp = new FakeTikTokYtDlpService(rawJson, Path.GetTempPath());
        var analysisSvc = new MediaAnalysisService(mockYtDlp, _formatService, _platformService);

        // 1. Pela estrutura retornada (apenas áudio de slideshow sem vídeo)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            analysisSvc.AnalyzeAsync("https://www.tiktok.com/@photo_user/video/7200000000000000001"));
        Assert.Contains("fotos/slideshows", ex.Message, StringComparison.OrdinalIgnoreCase);

        // 2. Pela URL explícita /photo/
        var argEx = await Assert.ThrowsAsync<ArgumentException>(() =>
            analysisSvc.AnalyzeAsync("https://www.tiktok.com/@photo_user/photo/7200000000000000001"));
        Assert.Contains("fotos/slideshows", argEx.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("ERROR: [TikTok] 7548009887255170311: Your IP address is blocked from accessing this post (status code 10204)", "bloqueou o acesso")]
    [InlineData("ERROR: [TikTok] 123456: You do not have permission to view this post (status code 10216)", "privado")]
    [InlineData("ERROR: [TikTok] 123456: This video is requiring login", "login")]
    [InlineData("ERROR: [TikTok] 123456: Video not available (status code 10202)", "removido ou não está mais disponível")]
    [InlineData("ERROR: Unsupported URL: https://www.tiktok.com/?_r=1", "link curto do TikTok expirou")]
    public void ParseYtDlpError_MapsTikTokErrorsToFriendlyPortugueseMessages(string rawStderr, string expectedSubstring)
    {
        var friendly = YtDlpService.ParseYtDlpError(rawStderr);
        Assert.Contains(expectedSubstring, friendly, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeTikTokYtDlpService : IYtDlpService
    {
        private readonly string _rawJson;
        private readonly string _outputFolder;
        public int SimulatedDelayMs { get; set; } = 10;

        public FakeTikTokYtDlpService(string rawJson, string outputFolder)
        {
            _rawJson = rawJson;
            _outputFolder = outputFolder;
        }

        public Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse(_rawJson));
        }

        public Task<JsonDocument> GetPlaylistMetadataJsonAsync(string playlistUrl, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse(_rawJson));
        }

        public async Task<string> DownloadAsync(DownloadRequest request, IProgress<DownloadProgressReport> progress, CancellationToken ct = default)
        {
            await Task.Delay(SimulatedDelayMs, ct);
            var outPath = FileHelper.GetUniqueFilePath(_outputFolder, request.VideoTitle, "mp4");
            await File.WriteAllBytesAsync(outPath, new byte[] { 0x00, 0x01, 0x02, 0x03 }, ct);
            return outPath;
        }
    }

    private sealed class FakeTikTokSettingsService : ISettingsService
    {
        public int MaxConcurrent { get; set; } = 2;
        public AppSettings Settings { get; }

        public FakeTikTokSettingsService(string downloadFolder)
        {
            Settings = new AppSettings
            {
                DownloadFolder = downloadFolder,
                MaxConcurrentDownloads = 2
            };
        }

        public void SaveSettings() { }
        public void ReloadSettings() { }
    }

    private sealed class FakeTikTokDependencyManager : IDependencyManager
    {
        public IReadOnlyList<DependencyItem> GetDependencies() => Array.Empty<DependencyItem>();
        public Task<bool> CheckDependenciesAsync() => Task.FromResult(true);
        public bool AreAllDependenciesInstalled() => true;
        public Task<bool> EnsureAllDependenciesAsync(IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> InstallDependencyByNameAsync(string toolName, IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> UpdateDependencyAsync(string toolName, IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateDependencyAsync(string toolName) => Task.FromResult(true);
        public string GetYtDlpPath() => "yt-dlp.exe";
        public string GetFFmpegPath() => "ffmpeg.exe";
        public string GetFFprobePath() => "ffprobe.exe";
        public string GetDenoPath() => "deno.exe";
    }

    private sealed class FakeTikTokUpdateService : IUpdateService
    {
        public Task<UpdateCheckResult> CheckYtDlpUpdateAsync(CancellationToken ct = default) =>
            Task.FromResult(new UpdateCheckResult("yt-dlp", "2026.10.26", "2026.10.26", false, ""));
        public Task<UpdateCheckResult> CheckDenoUpdateAsync(CancellationToken ct = default) =>
            Task.FromResult(new UpdateCheckResult("deno", "2.0.0", "2.0.0", false, ""));
        public Task<bool> UpdateToolAsync(string toolName, CancellationToken ct = default) =>
            Task.FromResult(true);
    }

    private sealed class SyncDispatcherService : IDispatcherService
    {
        public bool CheckAccess() => true;
        public void Invoke(Action action) => action();
        public Task InvokeAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }
        public Task<T> InvokeAsync<T>(Func<T> func) => Task.FromResult(func());
    }
}
