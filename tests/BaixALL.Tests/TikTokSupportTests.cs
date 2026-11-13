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

        // Verifica que todas as opções de vídeo usam exclusivamente os format_id exatos classificados como NoWatermark, sem seletores genéricos ou fallbacks
        var expectedCleanIds = info.Formats
            .Where(f => f.WatermarkStatus == WatermarkStatus.NoWatermark)
            .Select(f => f.FormatId)
            .ToHashSet(StringComparer.Ordinal);

        var videoOptions = options.Where(o => !o.IsAudioOnly).ToList();
        Assert.Equal(4, videoOptions.Count);
        foreach (var opt in videoOptions)
        {
            Assert.Equal(WatermarkStatus.NoWatermark, opt.WatermarkStatus);
            Assert.DoesNotContain("best[", opt.FormatSelector, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("format_id^=", opt.FormatSelector, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("format_note", opt.FormatSelector, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("download", opt.FormatSelector, StringComparison.OrdinalIgnoreCase);

            var selectorTokens = opt.FormatSelector.Split('/', StringSplitOptions.RemoveEmptyEntries);
            Assert.NotEmpty(selectorTokens);
            foreach (var token in selectorTokens)
            {
                var videoFormatId = token.EndsWith("+bestaudio", StringComparison.Ordinal)
                    ? token.Substring(0, token.Length - "+bestaudio".Length)
                    : token;
                Assert.Contains(videoFormatId, expectedCleanIds);
            }
        }

        // Verifica ordem decrescente de resoluções (1080x1920 -> 720x1280 -> 576x1024) e respeito estrito ao limite de altura
        Assert.Equal(1920, videoOptions[1].Height);
        Assert.Equal("bytevc1_1080p_1360255-0/bytevc1_720p_770993-0/bytevc1_540p_587209-0/h264_540p_1337673-0", videoOptions[1].FormatSelector);

        Assert.Equal(1280, videoOptions[2].Height);
        Assert.Equal("bytevc1_720p_770993-0/bytevc1_540p_587209-0/h264_540p_1337673-0", videoOptions[2].FormatSelector);
        Assert.DoesNotContain("bytevc1_1080p_1360255-0", videoOptions[2].FormatSelector);

        Assert.Equal(1024, videoOptions[3].Height);
        Assert.Equal("bytevc1_540p_587209-0/h264_540p_1337673-0", videoOptions[3].FormatSelector);
        Assert.DoesNotContain("bytevc1_1080p_1360255-0", videoOptions[3].FormatSelector);
        Assert.DoesNotContain("bytevc1_720p_770993-0", videoOptions[3].FormatSelector);
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

    // 9. Comportamento quando o estado da marca é desconhecido (só existem formatos Unknown)
    [Fact]
    public async Task BuildFormatOptions_WhenWatermarkStatusIsUnknown_ClassifiesAsUnknownWithoutPromisingCleanVideoOrAutoSelecting()
    {
        var jsonPath = GetFixturePath("tiktok_video_unknown_watermark.json");
        var rawJson = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(rawJson);

        var info = _formatService.ParseVideoInfo(doc, "https://www.tiktok.com/@creator_unknown_wm/video/7059698374567611694");

        Assert.Equal(WatermarkStatus.Unknown, info.WatermarkStatus);
        Assert.False(info.HasCleanSource);
        Assert.False(info.HasOnlyWatermarkedSource);
        Assert.True(info.HasUnknownWatermarkSource);
        Assert.Equal("• Marca-d'água não verificada", info.WatermarkStatusText);

        var options = _formatService.BuildFormatOptions(info);
        var videoOptions = options.Where(o => !o.IsAudioOnly).ToList();

        Assert.NotEmpty(videoOptions);
        // Nenhuma opção desconhecida pode ser marcada como IsBestQuality (recomendada limpa) nem prometer ausência de marca-d'água
        Assert.DoesNotContain(options, o => o.IsBestQuality);
        foreach (var opt in videoOptions)
        {
            Assert.Equal(WatermarkStatus.Unknown, opt.WatermarkStatus);
            Assert.False(opt.IsBestQuality);
            Assert.False(opt.RequiresWatermarkConsent); // Não confunde Unknown com Watermarked
            Assert.Contains("Marca-d'água não verificada", opt.Label);
            Assert.DoesNotContain("Sem marca-d'água", opt.Label, StringComparison.OrdinalIgnoreCase);
        }

        // Valida comportamento na MainViewModel: não seleciona automaticamente como fonte limpa, exibe aviso claro e permite seleção manual
        var tempDir = Path.Combine(Path.GetTempPath(), $"baixall_tt_unk_{Guid.NewGuid():N}");
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
                UrlInput = "https://www.tiktok.com/@creator_unknown_wm/video/7059698374567611694"
            };

            await vm.AnalyzeAsync();

            Assert.True(vm.HasVideoInfo);
            Assert.Null(vm.SelectedFormat); // Não selecionou automaticamente formato desconhecido!
            Assert.True(vm.ShowUnknownWatermarkNotice);
            Assert.False(vm.ShowWatermarkWarning); // Não confunde com o estado comprovadamente com marca-d'água
            Assert.Equal("Warning", vm.StatusType);
            Assert.DoesNotContain("Sem marca-d'água confirmada", vm.StatusNotification, StringComparison.OrdinalIgnoreCase);

            // Clicar em Baixar sem escolher formato avisa que é necessário selecionar manualmente
            vm.StartDownloadCommand.Execute(null);
            Assert.Empty(downloadSvc.QueueItems);

            // Ao selecionar manualmente uma opção Unknown, permite baixar sem exigir o checkbox de watermarked
            vm.SelectedFormat = videoOptions.First();
            vm.StartDownloadCommand.Execute(null);
            Assert.Single(downloadSvc.QueueItems);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    // Testes obrigatórios da Revisão Crítica (Seção 4)
    [Theory]
    [InlineData("generic_stream_0", "")]
    [InlineData("http_mp4_1080", "Custom video")]
    [InlineData("download_addr", "Download video")]
    [InlineData("download", "")]
    [InlineData("play", "")]
    public void ClassifyTikTokFormatWatermark_FormatWithPreferenceMinusOneWithoutCleanEvidence_RemainsUnknown(string formatId, string formatNote)
    {
        var fmt = new VideoFormatRaw
        {
            FormatId = formatId,
            FormatNote = formatNote,
            Ext = "mp4",
            VCodec = "h264",
            ACodec = "aac",
            Width = 1080,
            Height = 1920,
            Preference = -1
        };

        var status = FormatSelectionService.ClassifyTikTokFormatWatermark(fmt);
        Assert.Equal(WatermarkStatus.Unknown, status);
    }

    [Fact]
    public void ClassifyTikTokFormatWatermark_Bytevc2WithPreferenceMinus100_RemainsUnavailable()
    {
        var fmt = new VideoFormatRaw
        {
            FormatId = "bytevc2_1080p_600000-0",
            FormatNote = "UNPLAYABLE",
            Ext = "mp4",
            VCodec = "bytevc2",
            ACodec = "aac",
            Width = 1080,
            Height = 1920,
            Preference = -100
        };

        Assert.True(fmt.IsUnplayable);
        Assert.False(fmt.HasVideo);
        Assert.Equal(WatermarkStatus.NotApplicable, FormatSelectionService.ClassifyTikTokFormatWatermark(fmt));
    }

    [Theory]
    [InlineData("play_addr", "Direct video", "h264")]
    [InlineData("play_addr_h264", "Direct video", "h264")]
    [InlineData("play_addr_bytevc1", "Direct video", "h265")]
    [InlineData("play_addr-0", "Direct video (API)", "h265")]
    [InlineData("normal_720_0", "Playback video", "h264")]
    [InlineData("bytevc1_1080p_1360255-0", "", "h265")]
    [InlineData("h264_720p_1173641-0", "", "h264")]
    public void ClassifyTikTokFormatWatermark_PlayAddrAndPlaybackFormats_AreClassifiedAsNoWatermark(string formatId, string formatNote, string vcodec)
    {
        var fmt = new VideoFormatRaw
        {
            FormatId = formatId,
            FormatNote = formatNote,
            Ext = "mp4",
            VCodec = vcodec,
            ACodec = "aac",
            Width = 1080,
            Height = 1920,
            Preference = -1
        };

        Assert.Equal(WatermarkStatus.NoWatermark, FormatSelectionService.ClassifyTikTokFormatWatermark(fmt));
    }

    [Fact]
    public void ClassifyTikTokFormatWatermark_MissingFormatNote_DoesNotAutomaticallyPassAsClean()
    {
        var fmtWithoutNote = new VideoFormatRaw
        {
            FormatId = "video_stream_hd",
            FormatNote = string.Empty,
            Ext = "mp4",
            VCodec = "h264",
            ACodec = "aac",
            Width = 1080,
            Height = 1920,
            Preference = -1
        };

        Assert.Equal(WatermarkStatus.Unknown, FormatSelectionService.ClassifyTikTokFormatWatermark(fmtWithoutNote));
        Assert.Throws<InvalidOperationException>(() =>
            FormatSelectionService.BuildVerifiedCleanTikTokSelector(new[] { fmtWithoutNote }));
    }

    [Theory]
    [InlineData("download", "watermarked")]
    [InlineData("download_addr", "Download video, watermarked")]
    [InlineData("video_wm", "watermarked")]
    public void ClassifyTikTokFormatWatermark_ExplicitlyWatermarkedFormat_IsClassifiedAsWatermarked(string formatId, string formatNote)
    {
        var fmt = new VideoFormatRaw
        {
            FormatId = formatId,
            FormatNote = formatNote,
            Ext = "mp4",
            VCodec = "h264",
            ACodec = "aac",
            Width = 1080,
            Height = 1920,
            Preference = -2
        };

        Assert.Equal(WatermarkStatus.Watermarked, FormatSelectionService.ClassifyTikTokFormatWatermark(fmt));
    }

    [Fact]
    public void BuildFormatOptions_WhenMixedCleanUnknownAndWatermarkedExist_RecommendedChoosesOnlyVerifiedCleanSource()
    {
        const string mixedJson = """
        {
          "id": "7707063215498399006",
          "title": "Vídeo com mistura de formatos limpos, desconhecidos e watermarked",
          "uploader": "tiktok",
          "channel": "TikTok",
          "duration": 30,
          "webpage_url": "https://www.tiktok.com/@tiktok/video/7707063215498399006",
          "extractor": "TikTok",
          "extractor_key": "TikTok",
          "formats": [
            {
              "format_id": "bytevc1_720p_770993-0",
              "format_note": "",
              "ext": "mp4",
              "width": 720,
              "height": 1280,
              "vcodec": "h265",
              "acodec": "aac",
              "tbr": 770.993,
              "preference": -1
            },
            {
              "format_id": "unknown_1080p",
              "format_note": "",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 2500.0,
              "preference": -1
            },
            {
              "format_id": "download_addr",
              "format_note": "Download video",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 2200.0,
              "preference": -1
            },
            {
              "format_id": "download",
              "format_note": "watermarked",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 2100.0,
              "preference": -2
            }
          ]
        }
        """;

        using var doc = JsonDocument.Parse(mixedJson);
        var info = _formatService.ParseVideoInfo(doc, "https://www.tiktok.com/@tiktok/video/7707063215498399006");

        // Confirma classificação individual dos 4 formatos na lista mista
        Assert.Equal(WatermarkStatus.NoWatermark, info.Formats.Single(f => f.FormatId == "bytevc1_720p_770993-0").WatermarkStatus);
        Assert.Equal(WatermarkStatus.Unknown, info.Formats.Single(f => f.FormatId == "unknown_1080p").WatermarkStatus);
        Assert.Equal(WatermarkStatus.Unknown, info.Formats.Single(f => f.FormatId == "download_addr").WatermarkStatus);
        Assert.Equal(WatermarkStatus.Watermarked, info.Formats.Single(f => f.FormatId == "download").WatermarkStatus);

        // A resolução máxima e o status do vídeo devem refletir SOMENTE a fonte comprovadamente limpa (720p), ignorando os 1080p Unknown/Watermarked
        Assert.Equal(WatermarkStatus.NoWatermark, info.WatermarkStatus);
        Assert.Equal("720p (HD) Vertical (720x1280)", info.MaxResolution);

        var options = _formatService.BuildFormatOptions(info);
        var bestOpt = options.Single(o => o.IsBestQuality);

        Assert.Equal(WatermarkStatus.NoWatermark, bestOpt.WatermarkStatus);
        Assert.Equal(1280, bestOpt.Height);
        Assert.Contains("720p (HD) Vertical (720x1280) • Sem marca-d'água", bestOpt.Label);
        Assert.Equal("bytevc1_720p_770993-0", bestOpt.FormatSelector);
        Assert.DoesNotContain("unknown_1080p", bestOpt.FormatSelector);
        Assert.DoesNotContain("download_addr", bestOpt.FormatSelector);
        Assert.DoesNotContain("best[", bestOpt.FormatSelector);

        // Nenhuma opção de vídeo 1080p (Unknown ou Watermarked) deve aparecer quando há fonte limpa
        var videoOptions = options.Where(o => !o.IsAudioOnly).ToList();
        Assert.All(videoOptions, o =>
        {
            Assert.Equal(WatermarkStatus.NoWatermark, o.WatermarkStatus);
            Assert.Equal(1280, o.Height);
        });
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
    [InlineData("ERROR: [TikTok] 7707063215498399006: Requested format is not available", "fonte sem marca-d'água selecionada deixou de estar disponível")]
    public void ParseYtDlpError_MapsTikTokErrorsToFriendlyPortugueseMessages(string rawStderr, string expectedSubstring)
    {
        var friendly = YtDlpService.ParseYtDlpError(rawStderr);
        Assert.Contains(expectedSubstring, friendly, StringComparison.OrdinalIgnoreCase);
    }

    // Revisão Final (Seção 2): Testes de regressão de segurança na seleção de formatos
    [Fact]
    public void BuildVerifiedCleanTikTokSelector_UsesOnlyExactVerifiedCleanIds_AndNeverIncludesPrefixOrGenericFallback()
    {
        var formats = new List<VideoFormatRaw>
        {
            new()
            {
                FormatId = "bytevc1_1080p_1423099-0",
                FormatNote = "",
                Ext = "mp4",
                Width = 1080,
                Height = 1920,
                VCodec = "h265",
                ACodec = "aac",
                Tbr = 1423.099,
                Preference = -1,
                WatermarkStatus = WatermarkStatus.NoWatermark
            },
            new()
            {
                FormatId = "h264_720p_1173641-0",
                FormatNote = "",
                Ext = "mp4",
                Width = 720,
                Height = 1280,
                VCodec = "h264",
                ACodec = "aac",
                Tbr = 1173.641,
                Preference = -1,
                WatermarkStatus = WatermarkStatus.NoWatermark
            },
            // Formato Unknown cujo ID começa com h264_ (mas não segue o padrão de reprodução limpa)
            new()
            {
                FormatId = "h264_unverified_custom",
                FormatNote = "",
                Ext = "mp4",
                Width = 1080,
                Height = 1920,
                VCodec = "h264",
                ACodec = "aac",
                Tbr = 3000.0,
                Preference = -1,
                WatermarkStatus = FormatSelectionService.ClassifyTikTokFormatWatermark(new VideoFormatRaw
                {
                    FormatId = "h264_unverified_custom",
                    VCodec = "h264",
                    ACodec = "aac",
                    Width = 1080,
                    Height = 1920
                })
            },
            // Formato Watermarked
            new()
            {
                FormatId = "download",
                FormatNote = "watermarked",
                Ext = "mp4",
                Width = 1080,
                Height = 1920,
                VCodec = "h264",
                ACodec = "aac",
                Tbr = 2500.0,
                Preference = -2,
                WatermarkStatus = WatermarkStatus.Watermarked
            }
        };

        Assert.Equal(WatermarkStatus.Unknown, formats[2].WatermarkStatus);

        var selector = FormatSelectionService.BuildVerifiedCleanTikTokSelector(formats);

        Assert.Equal("bytevc1_1080p_1423099-0/h264_720p_1173641-0", selector);
        Assert.DoesNotContain("h264_unverified_custom", selector, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("download", selector, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("best[", selector, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("format_id^=", selector, StringComparison.OrdinalIgnoreCase);

        var selector720 = FormatSelectionService.BuildCleanTikTokResolutionSelector(formats, 1280);
        Assert.Equal("h264_720p_1173641-0", selector720);
        Assert.DoesNotContain("bytevc1_1080p_1423099-0", selector720);
    }

    [Fact]
    public void BuildVerifiedCleanTikTokSelector_EnforcesResolutionLimitsAndCombinesAudioForVideoOnlyStreams()
    {
        var cleanFormats = new List<VideoFormatRaw>
        {
            // 1080p sem áudio próprio (requer combinação +bestaudio sem ampliar faixa de vídeo)
            new()
            {
                FormatId = "bytevc1_1080p_1423099-0",
                Ext = "mp4",
                Width = 1080,
                Height = 1920,
                VCodec = "h265",
                ACodec = "none",
                Tbr = 1423.099,
                WatermarkStatus = WatermarkStatus.NoWatermark
            },
            // 720p com áudio integrado
            new()
            {
                FormatId = "h264_720p_1173641-0",
                Ext = "mp4",
                Width = 720,
                Height = 1280,
                VCodec = "h264",
                ACodec = "aac",
                Tbr = 1173.641,
                WatermarkStatus = WatermarkStatus.NoWatermark
            },
            // 540p sem áudio próprio
            new()
            {
                FormatId = "h264_540p_608633-0",
                Ext = "mp4",
                Width = 576,
                Height = 1024,
                VCodec = "h264",
                ACodec = "none",
                Tbr = 608.633,
                WatermarkStatus = WatermarkStatus.NoWatermark
            },
            // Formato limpo sem altura conhecida (não deve entrar em opções limitadas por resolução)
            new()
            {
                FormatId = "play_addr",
                FormatNote = "Direct video",
                Ext = "mp4",
                Width = null,
                Height = null,
                VCodec = "h264",
                ACodec = "aac",
                WatermarkStatus = WatermarkStatus.NoWatermark
            }
        };

        var bestSelector = FormatSelectionService.BuildVerifiedCleanTikTokSelector(cleanFormats);
        Assert.Equal("bytevc1_1080p_1423099-0+bestaudio/h264_720p_1173641-0/h264_540p_608633-0+bestaudio/play_addr", bestSelector);

        var max1280Selector = FormatSelectionService.BuildCleanTikTokResolutionSelector(cleanFormats, 1280);
        Assert.Equal("h264_720p_1173641-0/h264_540p_608633-0+bestaudio", max1280Selector);
        Assert.DoesNotContain("bytevc1_1080p_1423099-0", max1280Selector);
        Assert.DoesNotContain("play_addr", max1280Selector);

        var max1024Selector = FormatSelectionService.BuildCleanTikTokResolutionSelector(cleanFormats, 1024);
        Assert.Equal("h264_540p_608633-0+bestaudio", max1024Selector);
    }

    [Fact]
    public async Task WhenCleanFormatDisappearsAtDownloadTime_NeverFallsBackToUnknownOrWatermarked_AndFailsFriendlily()
    {
        const string analysisJson = """
        {
          "id": "7707063215498399006",
          "title": "Vídeo onde a fonte limpa desaparece no momento do download",
          "uploader": "tiktok",
          "channel": "TikTok",
          "duration": 25,
          "webpage_url": "https://www.tiktok.com/@tiktok/video/7707063215498399006",
          "extractor": "TikTok",
          "extractor_key": "TikTok",
          "formats": [
            {
              "format_id": "bytevc1_720p_770993-0",
              "format_note": "",
              "ext": "mp4",
              "width": 720,
              "height": 1280,
              "vcodec": "h265",
              "acodec": "aac",
              "tbr": 770.993,
              "preference": -1
            },
            {
              "format_id": "h264_unverified_1080p",
              "format_note": "",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 2500.0,
              "preference": -1
            },
            {
              "format_id": "download",
              "format_note": "watermarked",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 2100.0,
              "preference": -2
            }
          ]
        }
        """;

        // Na execução do download, o formato limpo bytevc1_720p_770993-0 desapareceu; restam apenas Unknown e Watermarked
        var downloadTimeAvailableFormats = new List<VideoFormatRaw>
        {
            new()
            {
                FormatId = "h264_unverified_1080p",
                Ext = "mp4",
                Width = 1080,
                Height = 1920,
                VCodec = "h264",
                ACodec = "aac",
                WatermarkStatus = WatermarkStatus.Unknown
            },
            new()
            {
                FormatId = "download",
                FormatNote = "watermarked",
                Ext = "mp4",
                Width = 1080,
                Height = 1920,
                VCodec = "h264",
                ACodec = "aac",
                WatermarkStatus = WatermarkStatus.Watermarked
            }
        };

        using var doc = JsonDocument.Parse(analysisJson);
        var info = _formatService.ParseVideoInfo(doc, "https://www.tiktok.com/@tiktok/video/7707063215498399006");
        var bestCleanOption = _formatService.BuildFormatOptions(info).Single(o => o.IsBestQuality);

        Assert.Equal("bytevc1_720p_770993-0", bestCleanOption.FormatSelector);

        var tempDir = Path.Combine(Path.GetTempPath(), $"baixall_tt_missing_clean_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var mockYtDlp = new StrictSelectorTikTokYtDlpService(analysisJson, downloadTimeAvailableFormats, tempDir);
            var settingsSvc = new FakeTikTokSettingsService(tempDir);
            var historySvc = new HistoryService(null, Path.Combine(tempDir, "history.json"));
            var dispatcher = new SyncDispatcherService();
            var downloadSvc = new DownloadService(mockYtDlp, historySvc, settingsSvc, dispatcher);

            var item = downloadSvc.EnqueueDownload(new DownloadRequest
            {
                VideoId = info.Id,
                VideoUrl = info.OriginalUrl,
                VideoTitle = info.Title,
                Channel = info.Channel,
                DestinationFolder = tempDir,
                Format = bestCleanOption,
                Platform = PlatformType.TikTok,
                CanonicalKey = info.CanonicalKey
            }, info.ThumbnailUrl);

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && item.IsActive)
            {
                await Task.Delay(20);
            }

            Assert.False(item.IsActive);
            Assert.False(item.IsCompleted);
            Assert.True(item.IsFailed);
            Assert.Contains("fonte sem marca-d'água selecionada deixou de estar disponível", item.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(historySvc.GetHistory());
            Assert.Empty(Directory.GetFiles(tempDir, "*.mp4"));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task YtDlpInfoJsonSimulation_ValidatesExactSelectorResolutionAudioMergeAndNoFallbackOnMissingCleanFormat()
    {
        const string fullJson = """
        {
          "id": "7707063215498399006",
          "title": "Simulação offline de seletores com info-json",
          "uploader": "tiktok",
          "channel": "TikTok",
          "duration": 20,
          "webpage_url": "https://www.tiktok.com/@tiktok/video/7707063215498399006",
          "extractor": "TikTok",
          "extractor_key": "TikTok",
          "formats": [
            {
              "format_id": "audio",
              "format_note": "Audio",
              "ext": "m4a",
              "vcodec": "none",
              "acodec": "aac",
              "abr": 128.0,
              "url": "https://example.invalid/audio.m4a"
            },
            {
              "format_id": "bytevc1_1080p_1423099-0",
              "format_note": "",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h265",
              "acodec": "none",
              "tbr": 1423.099,
              "preference": -1,
              "url": "https://example.invalid/clean1080_videoonly.mp4"
            },
            {
              "format_id": "h264_720p_1173641-0",
              "format_note": "",
              "ext": "mp4",
              "width": 720,
              "height": 1280,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 1173.641,
              "preference": -1,
              "url": "https://example.invalid/clean720_muxed.mp4"
            },
            {
              "format_id": "h264_unverified_1080p",
              "format_note": "",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 2800.0,
              "preference": -1,
              "url": "https://example.invalid/unknown1080.mp4"
            },
            {
              "format_id": "download",
              "format_note": "watermarked",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 2400.0,
              "preference": -2,
              "url": "https://example.invalid/watermarked1080.mp4"
            }
          ]
        }
        """;

        const string missingCleanFormatsJson = """
        {
          "id": "7707063215498399006",
          "title": "Simulação offline quando formatos limpos desaparecem",
          "uploader": "tiktok",
          "channel": "TikTok",
          "duration": 20,
          "webpage_url": "https://www.tiktok.com/@tiktok/video/7707063215498399006",
          "extractor": "TikTok",
          "extractor_key": "TikTok",
          "formats": [
            {
              "format_id": "h264_unverified_1080p",
              "format_note": "",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 2800.0,
              "preference": -1,
              "url": "https://example.invalid/unknown1080.mp4"
            },
            {
              "format_id": "download",
              "format_note": "watermarked",
              "ext": "mp4",
              "width": 1080,
              "height": 1920,
              "vcodec": "h264",
              "acodec": "aac",
              "tbr": 2400.0,
              "preference": -2,
              "url": "https://example.invalid/watermarked1080.mp4"
            }
          ]
        }
        """;

        using var doc = JsonDocument.Parse(fullJson);
        var info = _formatService.ParseVideoInfo(doc, "https://www.tiktok.com/@tiktok/video/7707063215498399006");
        var options = _formatService.BuildFormatOptions(info);

        var bestOpt = options.Single(o => o.IsBestQuality);
        var opt720 = options.Single(o => !o.IsBestQuality && !o.IsAudioOnly && o.Height == 1280);

        Assert.Equal("bytevc1_1080p_1423099-0+bestaudio/h264_720p_1173641-0", bestOpt.FormatSelector);
        Assert.Equal("h264_720p_1173641-0", opt720.FormatSelector);

        // Avaliação determinística em memória e, quando yt-dlp estiver instalado localmente, validação real com --load-info-json
        var ytDlpExe = TryResolveLocalYtDlpExecutable();
        if (ytDlpExe != null)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"baixall_infojson_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var fullJsonFile = Path.Combine(tempDir, "full.json");
                var missingJsonFile = Path.Combine(tempDir, "missing.json");
                await File.WriteAllTextAsync(fullJsonFile, fullJson);
                await File.WriteAllTextAsync(missingJsonFile, missingCleanFormatsJson);

                // 1. Melhor opção (1080p sem áudio próprio -> deve combinar bytevc1_1080p_1423099-0+audio)
                var resBest = await ProcessRunner.RunAsync(
                    ytDlpExe,
                    new[] { "--load-info-json", fullJsonFile, "--simulate", "-f", bestOpt.FormatSelector, "-O", "%(format_id)s" });
                Assert.True(resBest.IsSuccess, resBest.StandardError);
                Assert.Equal("bytevc1_1080p_1423099-0+audio", resBest.StandardOutput.Trim());

                // 2. Opção 720p limitada -> deve escolher h264_720p_1173641-0
                var res720 = await ProcessRunner.RunAsync(
                    ytDlpExe,
                    new[] { "--load-info-json", fullJsonFile, "--simulate", "-f", opt720.FormatSelector, "-O", "%(format_id)s" });
                Assert.True(res720.IsSuccess, res720.StandardError);
                Assert.Equal("h264_720p_1173641-0", res720.StandardOutput.Trim());

                // 3. Se os formatos limpos desaparecerem na hora do download -> yt-dlp deve falhar sem escolher h264_unverified_1080p nem download
                var resMissing = await ProcessRunner.RunAsync(
                    ytDlpExe,
                    new[] { "--load-info-json", missingJsonFile, "--simulate", "-f", bestOpt.FormatSelector, "-O", "%(format_id)s" });
                Assert.False(resMissing.IsSuccess);
                Assert.Contains("Requested format is not available", resMissing.StandardError, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("h264_unverified_1080p", resMissing.StandardOutput, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("download", resMissing.StandardOutput, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }

    // Revisão Final (Seção 4): Garante que o histórico já está persistido no instante em que IsCompleted se torna true
    [Fact]
    public async Task DownloadService_RecordsHistoryBeforeSignalingIsCompleted_EliminatingCompletionRaceCondition()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"baixall_tt_hist_order_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var jsonPath = GetFixturePath("tiktok_video_multi_res.json");
            var rawJson = File.ReadAllText(jsonPath);

            var mockYtDlp = new FakeTikTokYtDlpService(rawJson, tempDir);
            var settingsSvc = new FakeTikTokSettingsService(tempDir);
            var historySvc = new HistoryService(null, Path.Combine(tempDir, "history.json"));
            var dispatcher = new SyncDispatcherService();
            var downloadSvc = new DownloadService(mockYtDlp, historySvc, settingsSvc, dispatcher);

            using var doc = JsonDocument.Parse(rawJson);
            var info = _formatService.ParseVideoInfo(doc, "https://www.tiktok.com/@tiktok/video/7707063215498399006");
            var bestFormat = _formatService.BuildFormatOptions(info).First(o => o.IsBestQuality);

            HistoryItem? snapshotAtCompletionInstant = null;

            var item = downloadSvc.EnqueueDownload(new DownloadRequest
            {
                VideoId = info.Id,
                VideoUrl = info.OriginalUrl,
                VideoTitle = info.Title,
                Channel = info.Channel,
                DestinationFolder = tempDir,
                Format = bestFormat,
                Platform = PlatformType.TikTok,
                CanonicalKey = info.CanonicalKey
            }, info.ThumbnailUrl);

            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(DownloadItemViewModel.IsCompleted) && item.IsCompleted)
                {
                    snapshotAtCompletionInstant = historySvc.GetHistory().FirstOrDefault();
                }
            };

            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout && !item.IsCompleted)
            {
                await Task.Delay(20);
            }

            Assert.True(item.IsCompleted);
            Assert.NotNull(snapshotAtCompletionInstant);
            Assert.Equal("7707063215498399006", snapshotAtCompletionInstant!.VideoId);
            Assert.Equal("@tiktok", snapshotAtCompletionInstant.Channel);
            Assert.Equal("TikTok", snapshotAtCompletionInstant.Platform);
            Assert.Equal("tt:7707063215498399006", snapshotAtCompletionInstant.CanonicalKey);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    private static string? TryResolveLocalYtDlpExecutable()
    {
        var managedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BaixALL", "tools", "yt-dlp", "yt-dlp.exe");

        if (File.Exists(managedPath))
        {
            return managedPath;
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), "yt-dlp.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch { }
        }

        return null;
    }

    private sealed class StrictSelectorTikTokYtDlpService : IYtDlpService
    {
        private readonly string _analysisJson;
        private readonly IReadOnlyList<VideoFormatRaw> _downloadTimeFormats;
        private readonly string _outputFolder;

        public StrictSelectorTikTokYtDlpService(
            string analysisJson,
            IReadOnlyList<VideoFormatRaw> downloadTimeFormats,
            string outputFolder)
        {
            _analysisJson = analysisJson;
            _downloadTimeFormats = downloadTimeFormats;
            _outputFolder = outputFolder;
        }

        public Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default) =>
            Task.FromResult(JsonDocument.Parse(_analysisJson));

        public Task<JsonDocument> GetPlaylistMetadataJsonAsync(string playlistUrl, CancellationToken ct = default) =>
            Task.FromResult(JsonDocument.Parse(_analysisJson));

        public async Task<string> DownloadAsync(DownloadRequest request, IProgress<DownloadProgressReport> progress, CancellationToken ct = default)
        {
            await Task.Delay(10, ct);

            var selectorTokens = (request.Format?.FormatSelector ?? string.Empty)
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            var matched = selectorTokens
                .Select(t => t.EndsWith("+bestaudio", StringComparison.Ordinal)
                    ? t.Substring(0, t.Length - "+bestaudio".Length)
                    : t)
                .FirstOrDefault(id => _downloadTimeFormats.Any(f => string.Equals(f.FormatId, id, StringComparison.Ordinal)));

            if (matched == null)
            {
                bool isCleanTikTok =
                    request.Platform == PlatformType.TikTok &&
                    !request.IsAudioOnly &&
                    request.Format?.WatermarkStatus == WatermarkStatus.NoWatermark;

                var rawError = $"ERROR: [TikTok] {request.VideoId}: Requested format is not available";
                throw new InvalidOperationException(YtDlpService.ParseYtDlpError(rawError, isCleanTikTok));
            }

            var outPath = FileHelper.GetUniqueFilePath(_outputFolder, request.VideoTitle, "mp4");
            await File.WriteAllBytesAsync(outPath, new byte[] { 0x00, 0x01, 0x02, 0x03 }, ct);
            return outPath;
        }
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
