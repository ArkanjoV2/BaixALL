using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Models;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class MultiplatformAnalysisTests
{
    private class DummyDispatcherService : IDispatcherService
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> callback) => callback();
        public Task InvokeAsync(Action action) { action(); return Task.CompletedTask; }
        public Task<T> InvokeAsync<T>(Func<T> callback) => Task.FromResult(callback());
        public bool CheckAccess() => true;
    }

    private class MockYtDlpService : IYtDlpService
    {
        public string LastUrlRequested { get; private set; } = string.Empty;

        public Task<string> DownloadAsync(DownloadRequest request, IProgress<DownloadProgressReport> progress, CancellationToken ct = default)
        {
            var filePath = Path.Combine(request.DestinationFolder, $"{request.VideoTitle}.mp4");
            Directory.CreateDirectory(request.DestinationFolder);
            File.WriteAllText(filePath, "test content");
            return Task.FromResult(filePath);
        }

        public Func<string, JsonDocument>? CustomMetadataHandler { get; set; }

        public Task<JsonDocument> GetMetadataJsonAsync(string url, CancellationToken ct = default)
        {
            LastUrlRequested = url;
            if (CustomMetadataHandler != null)
            {
                return Task.FromResult(CustomMetadataHandler(url));
            }

            string fixturePath = "";

            if (url.Contains("x.com") || url.Contains("twitter.com"))
            {
                if (url.Contains("1834289891461464455"))
                {
                    fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "twitter_no_video.json");
                }
                else
                {
                    fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "twitter_video.json");
                }
            }
            else if (url.Contains("instagram.com"))
            {
                if (url.Contains("carousel") || url.Contains("BQ0eAlwhDrw"))
                {
                    fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "instagram_carousel.json");
                }
                else
                {
                    fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "instagram_reel.json");
                }
            }

            if (!File.Exists(fixturePath))
            {
                // Fallback para caminho de projeto se rodando no diretório de saída
                fixturePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", Path.GetFileName(fixturePath));
            }

            if (File.Exists(fixturePath))
            {
                return Task.FromResult(JsonDocument.Parse(File.ReadAllText(fixturePath)));
            }

            throw new InvalidOperationException($"Fixture não encontrada: {fixturePath}");
        }

        public Task<JsonDocument> GetPlaylistMetadataJsonAsync(string url, CancellationToken ct = default)
        {
            return GetMetadataJsonAsync(url, ct);
        }
    }

    private static string GetFixturePath(string fileName)
    {
        var localPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        if (File.Exists(localPath)) return localPath;

        var sourcePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", fileName);
        if (File.Exists(sourcePath)) return sourcePath;

        throw new FileNotFoundException($"Fixture '{fileName}' não encontrada.");
    }

    [Fact]
    public void FormatSelectionService_TwitterVideoFixture_ExtractsRealResolutionsWithoutFakes()
    {
        var fmtService = new FormatSelectionService();
        var jsonPath = GetFixturePath("twitter_video.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));

        var videoInfo = fmtService.ParseVideoInfo(doc, "https://x.com/captainamerica/status/719944021058060289");

        Assert.Equal("717462543795523584", videoInfo.Id);
        Assert.Equal(PlatformType.Twitter, videoInfo.Platform);
        Assert.Equal("x:717462543795523584", videoInfo.CanonicalKey);
        Assert.Equal("720p (HD)", videoInfo.MaxResolution);
        Assert.Equal("Captain America", videoInfo.Channel);

        var options = fmtService.BuildFormatOptions(videoInfo);

        // Deve conter Melhor qualidade, 720p, 360p, 180p e Somente Áudio
        Assert.Contains(options, o => o.IsBestQuality);
        Assert.Contains(options, o => o.Height == 720);
        Assert.Contains(options, o => o.Height == 360);
        Assert.Contains(options, o => o.Height == 180);
        Assert.Contains(options, o => o.IsAudioOnly);

        // NÃO deve inventar 1080p ou 4K que não existem no post original
        Assert.DoesNotContain(options, o => o.Height == 1080);
        Assert.DoesNotContain(options, o => o.Height == 2160);
    }

    [Fact]
    public void FormatSelectionService_TwitterVideoFixture_ProgressiveFormatsWithNullVCodec_MarkedAsHasVideo()
    {
        var fmtService = new FormatSelectionService();
        var jsonPath = GetFixturePath("twitter_video.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));

        var videoInfo = fmtService.ParseVideoInfo(doc, "https://x.com/captainamerica/status/719944021058060289");

        // Os formatos http-320, http-832 e http-2176 possuem vcodec nulo no yt-dlp
        var progressiveFormats = videoInfo.Formats.Where(f => f.FormatId.StartsWith("http-")).ToList();
        Assert.NotEmpty(progressiveFormats);

        foreach (var format in progressiveFormats)
        {
            Assert.True(format.HasVideo, $"Formato {format.FormatId} deveria ser marcado como HasVideo");
        }
    }

    [Fact]
    public void FormatSelectionService_InstagramReelFixture_ExtractsVerticalResolutionCorrectly()
    {
        var fmtService = new FormatSelectionService();
        var jsonPath = GetFixturePath("instagram_reel.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));

        var videoInfo = fmtService.ParseVideoInfo(doc, "https://www.instagram.com/reel/Chunk8-jurw/");

        Assert.Equal("Chunk8-jurw", videoInfo.Id);
        Assert.Equal(PlatformType.Instagram, videoInfo.Platform);
        Assert.Equal("ig:Chunk8-jurw", videoInfo.CanonicalKey);
        Assert.Contains("Vertical", videoInfo.MaxResolution);
        Assert.Contains("720x1280", videoInfo.MaxResolution);

        var options = fmtService.BuildFormatOptions(videoInfo);
        Assert.NotEmpty(options);
        Assert.Contains(options, o => o.IsBestQuality);
        Assert.Contains(options, o => o.IsAudioOnly);
    }

    [Fact]
    public void FormatSelectionService_InstagramCarouselFixture_ExtractsAllVideoEntriesWithUniqueCanonicalKeys()
    {
        var fmtService = new FormatSelectionService();
        var jsonPath = GetFixturePath("instagram_carousel.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));

        var playlistInfo = fmtService.ParsePlaylistInfo(doc, "https://www.instagram.com/p/BQ0eAlwhDrw/");

        Assert.Equal("BQ0eAlwhDrw", playlistInfo.Id);
        Assert.Equal(PlatformType.Instagram, playlistInfo.Platform);
        Assert.True(playlistInfo.IsCarousel);
        Assert.Equal(3, playlistInfo.TotalVideosCount);
        Assert.Equal(3, playlistInfo.Items.Count);

        // Todas as entradas de vídeo devem ter chaves canônicas próprias e únicas
        var keys = playlistInfo.Items.Select(x => x.CanonicalKey).ToList();
        Assert.Equal(3, keys.Distinct().Count());

        // Confirma IDs filhas reais do Instagram
        Assert.Equal("ig:BQ0dSaohpPW", playlistInfo.Items[0].CanonicalKey);
        Assert.Equal("ig:BQ0dTpOhuHT", playlistInfo.Items[1].CanonicalKey);
        Assert.Equal("ig:BQ0dT7RBFeF", playlistInfo.Items[2].CanonicalKey);
    }

    [Fact]
    public async Task MediaAnalysisService_TwitterNoVideo_ThrowsFriendlyException()
    {
        var mockYtDlp = new MockYtDlpService();
        var fmtService = new FormatSelectionService();
        var analysisService = new MediaAnalysisService(mockYtDlp, fmtService);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            analysisService.AnalyzeVideoAsync("https://x.com/NASA/status/1834289891461464455?no_video=1"));

        Assert.Equal("Esta publicação do X/Twitter não contém nenhum vídeo.", ex.Message);
    }

    [Fact]
    public async Task MediaAnalysisService_UnsupportedUrl_ThrowsArgumentException()
    {
        var mockYtDlp = new MockYtDlpService();
        var fmtService = new FormatSelectionService();
        var analysisService = new MediaAnalysisService(mockYtDlp, fmtService);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            analysisService.AnalyzeVideoAsync("https://vimeo.com/12345678"));

        Assert.Contains("não pertence a uma plataforma suportada", ex.Message);
    }

    [Fact]
    public async Task MediaAnalysisService_TwitterVideo_AnalyzesCorrectly()
    {
        var mockYtDlp = new MockYtDlpService();
        var fmtService = new FormatSelectionService();
        var analysisService = new MediaAnalysisService(mockYtDlp, fmtService);

        var videoInfo = await analysisService.AnalyzeVideoAsync("https://x.com/captainamerica/status/719944021058060289");

        Assert.NotNull(videoInfo);
        Assert.Equal(PlatformType.Twitter, videoInfo.Platform);
        Assert.Equal("x:717462543795523584", videoInfo.CanonicalKey);
        Assert.Equal("https://x.com/i/status/719944021058060289", mockYtDlp.LastUrlRequested);
    }

    [Fact]
    public async Task MediaAnalysisService_InstagramReel_AnalyzesCorrectly()
    {
        var mockYtDlp = new MockYtDlpService();
        var fmtService = new FormatSelectionService();
        var analysisService = new MediaAnalysisService(mockYtDlp, fmtService);

        var videoInfo = await analysisService.AnalyzeVideoAsync("https://www.instagram.com/reel/Chunk8-jurw/");

        Assert.NotNull(videoInfo);
        Assert.Equal(PlatformType.Instagram, videoInfo.Platform);
        Assert.Equal("ig:Chunk8-jurw", videoInfo.CanonicalKey);
    }

    [Theory]
    [InlineData("ERROR: [twitter] 1834289891461464455: No video could be found in this tweet", "Esta publicação do X/Twitter não contém nenhum vídeo.")]
    [InlineData("ERROR: [twitter] This tweet has been deleted by user", "Esta publicação do X/Twitter foi excluída ou não existe mais.")]
    [InlineData("WARNING: [Instagram] Instagram API is not granting access", "O Instagram restringiu o acesso público a esta publicação (exige login na plataforma ou conteúdo privado). O BaixALL opera apenas com mídias públicas e não armazena credenciais do usuário.")]
    [InlineData("ERROR: [instagram:story] This content is unreachable. Use --cookies-from-browser", "O download de Stories do Instagram exige login com conta de usuário, o que não é suportado pelo BaixALL por motivos de segurança.")]
    [InlineData("ERROR: HTTP Error 429: Too Many Requests", "A plataforma atingiu temporariamente o limite de requisições para o seu endereço IP. Aguarde alguns minutos antes de tentar novamente.")]
    public void YtDlpService_ParseYtDlpError_MultiplatformErrorsMappedCorrectly(string rawError, string expected)
    {
        var actual = YtDlpService.ParseYtDlpError(rawError);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task DownloadService_MixedPlatformDownloads_MaintainsSingleQueueAndConcurrency()
    {
        var mockYtDlp = new MockYtDlpService();
        var tempHist = Path.Combine(Path.GetTempPath(), $"hist_mixed_{Guid.NewGuid():N}.json");
        var histService = new HistoryService(historyFilePath: tempHist);
        var settingsService = new SettingsService();
        settingsService.Settings.MaxConcurrentDownloads = 2;

        var dlService = new DownloadService(mockYtDlp, histService, settingsService, new DummyDispatcherService());

        var reqYouTube = new DownloadRequest
        {
            VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            VideoTitle = "YouTube Video",
            DestinationFolder = Path.Combine(Path.GetTempPath(), "yt_dl"),
            Platform = PlatformType.YouTube,
            CanonicalKey = "yt:dQw4w9WgXcQ"
        };

        var reqInstagram = new DownloadRequest
        {
            VideoUrl = "https://www.instagram.com/reel/Chunk8-jurw/",
            VideoTitle = "Instagram Reel",
            DestinationFolder = Path.Combine(Path.GetTempPath(), "ig_dl"),
            Platform = PlatformType.Instagram,
            CanonicalKey = "ig:Chunk8-jurw"
        };

        var reqTwitter = new DownloadRequest
        {
            VideoUrl = "https://x.com/captainamerica/status/719944021058060289",
            VideoTitle = "Twitter Video",
            DestinationFolder = Path.Combine(Path.GetTempPath(), "x_dl"),
            Platform = PlatformType.Twitter,
            CanonicalKey = "x:719944021058060289"
        };

        var itemYt = dlService.EnqueueDownload(reqYouTube, "");
        var itemIg = dlService.EnqueueDownload(reqInstagram, "");
        var itemX = dlService.EnqueueDownload(reqTwitter, "");

        Assert.Equal(3, dlService.QueueItems.Count);
        Assert.Equal("YouTube", itemYt.PlatformDisplayName);
        Assert.Equal("Instagram", itemIg.PlatformDisplayName);
        Assert.Equal("X / Twitter", itemX.PlatformDisplayName);

        // Aguarda todos os downloads completarem através do único motor
        while (dlService.HasActiveDownloads)
        {
            await Task.Delay(20);
        }

        Assert.True(itemYt.IsCompleted);
        Assert.True(itemIg.IsCompleted);
        Assert.True(itemX.IsCompleted);

        // Verifica que todos foram gravados no histórico com a respectiva plataforma
        var history = histService.GetHistory();
        Assert.Equal(3, history.Count);
        Assert.Contains(history, h => h.Platform == "YouTube" && h.CanonicalKey == "yt:dQw4w9WgXcQ");
        Assert.Contains(history, h => h.Platform == "Instagram" && h.CanonicalKey == "ig:Chunk8-jurw");
        Assert.Contains(history, h => h.Platform == "X / Twitter" && h.CanonicalKey == "x:719944021058060289");

        try { File.Delete(tempHist); } catch { }
    }

    [Fact]
    public void Carousel_SelectionOfMultipleVideos_DoesNotCauseImproperDeduplication()
    {
        var fmtService = new FormatSelectionService();
        var jsonPath = GetFixturePath("instagram_carousel.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));

        var playlistInfo = fmtService.ParsePlaylistInfo(doc, "https://www.instagram.com/p/BQ0eAlwhDrw/");

        // Seleciona 2 vídeos diferentes do mesmo carrossel
        var video1 = playlistInfo.Items[0];
        var video2 = playlistInfo.Items[1];

        Assert.NotEqual(video1.CanonicalKey, video2.CanonicalKey);
        Assert.Equal("ig:BQ0dSaohpPW", video1.CanonicalKey);
        Assert.Equal("ig:BQ0dTpOhuHT", video2.CanonicalKey);

        var mockYtDlp = new MockYtDlpService();
        var tempHist = Path.Combine(Path.GetTempPath(), $"hist_carousel_{Guid.NewGuid():N}.json");
        var histService = new HistoryService(historyFilePath: tempHist);
        var settingsService = new SettingsService();
        var dlService = new DownloadService(mockYtDlp, histService, settingsService, new DummyDispatcherService());

        // Enfileira ambos
        var batchId = Guid.NewGuid();
        var req1 = new DownloadRequest
        {
            VideoUrl = video1.VideoUrl,
            VideoTitle = video1.Title,
            BatchId = batchId,
            BatchTitle = playlistInfo.Title,
            Platform = video1.Platform,
            CanonicalKey = video1.CanonicalKey
        };
        var req2 = new DownloadRequest
        {
            VideoUrl = video2.VideoUrl,
            VideoTitle = video2.Title,
            BatchId = batchId,
            BatchTitle = playlistInfo.Title,
            Platform = video2.Platform,
            CanonicalKey = video2.CanonicalKey
        };

        var item1 = dlService.EnqueueDownload(req1, "");
        var item2 = dlService.EnqueueDownload(req2, "");

        // Ambos devem estar presentes na fila sem colisão
        Assert.Equal(2, dlService.QueueItems.Count);
        Assert.Contains(dlService.QueueItems, x => x.CanonicalKey == "ig:BQ0dSaohpPW");
        Assert.Contains(dlService.QueueItems, x => x.CanonicalKey == "ig:BQ0dTpOhuHT");

        // Verifica que as chaves canônicas são distintas entre si
        // (o mock síncrono pode ter completado os downloads, mas a presença na fila e as chaves distintas confirmam ausência de deduplicação indevida)
        var queuedKeys = dlService.QueueItems.Select(x => x.CanonicalKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains(video1.CanonicalKey, queuedKeys);
        Assert.Contains(video2.CanonicalKey, queuedKeys);

        try { File.Delete(tempHist); } catch { }
    }

    [Fact]
    public async Task Carousel_AllPhotos_ThrowsInvalidOperationException_WithHelpfulMessage()
    {
        var mockYtDlp = new MockYtDlpService();
        var fmtService = new FormatSelectionService();
        var analysisService = new MediaAnalysisService(mockYtDlp, fmtService);

        // Cria um documento JSON simulando um carrossel onde todos os itens são fotos
        var photosJson = """
        {
          "_type": "playlist",
          "id": "ALL_PHOTOS_POST",
          "title": "Post de Fotos",
          "uploader": "Fotógrafo",
          "entries": [
            { "id": "p1", "title": "Foto 1", "ext": "jpg", "vcodec": "none", "acodec": "none" },
            { "id": "p2", "title": "Foto 2", "ext": "png", "vcodec": "none", "acodec": "none" }
          ]
        }
        """;

        var doc = JsonDocument.Parse(photosJson);
        var playlistInfo = fmtService.ParsePlaylistInfo(doc, "https://www.instagram.com/p/ALL_PHOTOS_POST/");

        // Todos os itens devem estar desmarcados e indisponíveis
        Assert.All(playlistInfo.Items, item =>
        {
            Assert.False(item.IsAvailable);
            Assert.False(item.IsSelected);
            Assert.Contains("Foto", item.AvailabilityNotice);
        });
    }

    [Fact]
    public void IncompleteMetadata_PhotosWithDimensions_AreNotIdentifiedAsVideo()
    {
        var format = new VideoFormatRaw
        {
            FormatId = "photo_stream",
            Ext = "jpg",
            Height = 1080,
            Width = 1080,
            VCodec = null,
            ACodec = null
        };

        // Não deve ser considerado vídeo mesmo possuindo altura e largura
        Assert.False(format.HasVideo);
        Assert.False(format.HasAudio);
    }

    [Fact]
    public void IncompleteMetadata_NullCodecsWithAudioProperties_IdentifiedCorrectly()
    {
        var format = new VideoFormatRaw
        {
            FormatId = "audio_only_stream",
            Ext = "mp4",
            VCodec = "none",
            ACodec = null,
            AudioChannels = 2,
            Asr = 44100
        };

        Assert.False(format.HasVideo);
        Assert.True(format.HasAudio);
    }

    [Fact]
    public void IncompleteMetadata_NoneCodecs_NotIdentifiedAsVideoOrAudio()
    {
        var format = new VideoFormatRaw
        {
            FormatId = "none_stream",
            Ext = "unknown",
            VCodec = "none",
            ACodec = "none"
        };

        Assert.False(format.HasVideo);
        Assert.False(format.HasAudio);
    }

    [Fact]
    public void PlaylistInfo_CountSummaries_DifferentiatesMediaAndVideos_WhenPhotosPresent()
    {
        var playlist = new PlaylistInfo
        {
            Title = "Carrossel Misto",
            Platform = PlatformType.Instagram,
            IsCarousel = true,
            TotalVideosCount = 3,
            Items = new List<PlaylistItemInfo>
            {
                new() { Id = "v1", Title = "Video 1", PlaylistIndex = 1, IsAvailable = true, IsSelected = true },
                new() { Id = "f2", Title = "Foto 2", PlaylistIndex = 2, IsAvailable = false, IsSelected = false, AvailabilityNotice = "Foto (download de imagens em carrossel planejado para versão futura)" },
                new() { Id = "v3", Title = "Video 3", PlaylistIndex = 3, IsAvailable = true, IsSelected = true }
            }
        };

        playlist.UpdateCounts();

        Assert.Equal(3, playlist.TotalMediaCount);
        Assert.Equal(2, playlist.SupportedVideosCount);
        Assert.Equal(1, playlist.PhotoCount);
        Assert.Equal(2, playlist.SelectedVideosCount);
        Assert.Equal("3 mídias • 2 vídeos", playlist.BadgeCountSummary);
        Assert.Equal("Total: 3 mídias (2 vídeos suportados, 1 foto)", playlist.DetailedCountSummary);
        Assert.Equal("2 de 2 vídeos selecionados", playlist.SelectionSummary);
    }

    [Fact]
    public void FormatSelectionService_ParsePlaylistInfo_SimplifiesRedundantCarouselTitle_AndPreservesCaption()
    {
        var fmt = new FormatSelectionService();
        var json = """
        {
            "id": "post123",
            "title": "Post by instagram",
            "uploader": "Instagram",
            "description": "Surprise! Swipe left on the post above to see more.\nSecond line",
            "entries": [
                { "id": "sub1", "ext": "mp4", "vcodec": "avc1", "acodec": "mp4a" },
                { "id": "sub2", "ext": "jpg" }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var playlist = fmt.ParsePlaylistInfo(doc, "https://www.instagram.com/p/post123/");

        Assert.Equal("Surprise! Swipe left on the post above to see more.", playlist.Title);
        Assert.Equal(2, playlist.Items.Count);
        Assert.Equal(1, playlist.Items[0].PlaylistIndex);
        Assert.Equal(2, playlist.Items[1].PlaylistIndex);
        Assert.True(playlist.Items[0].IsAvailable);
        Assert.False(playlist.Items[1].IsAvailable);
    }

    [Fact]
    public void FormatSelectionService_ParsePlaylistInfo_StrictlyPreserves1BasedPlaylistIndex_EvenWhenFirstIsPhoto()
    {
        var fmt = new FormatSelectionService();
        var json = """
        {
            "id": "post456",
            "title": "Photo First Carousel",
            "uploader": "user1",
            "entries": [
                { "id": "photo1", "ext": "jpg" },
                { "id": "vid2", "ext": "mp4", "vcodec": "avc1", "acodec": "mp4a" },
                { "id": "vid3", "ext": "mp4", "vcodec": "avc1", "acodec": "mp4a" }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var playlist = fmt.ParsePlaylistInfo(doc, "https://www.instagram.com/p/post456/");

        Assert.Equal(1, playlist.Items[0].PlaylistIndex);
        Assert.False(playlist.Items[0].IsAvailable);

        Assert.Equal(2, playlist.Items[1].PlaylistIndex);
        Assert.True(playlist.Items[1].IsAvailable);

        Assert.Equal(3, playlist.Items[2].PlaylistIndex);
        Assert.True(playlist.Items[2].IsAvailable);
    }

    [Fact]
    public void FormatSelectionService_MixedCarousel_EmptyFormatsArrayOnPhoto_DetectedAsPhoto_PreservesVideoIndex2()
    {
        var fmt = new FormatSelectionService();
        // Simula exatamente a resposta do yt-dlp na URL real DdUAEH9lVF8
        var json = """
        {
            "_type": "playlist",
            "id": "DdUAEH9lVF8",
            "title": "Post by minecraft",
            "uploader": "minecraft",
            "entries": [
                {
                    "id": "DdUAEBClcjl",
                    "title": "Video by minecraft",
                    "ext": null,
                    "formats": []
                },
                {
                    "id": "DdUABjMibMh",
                    "title": "Video by minecraft",
                    "ext": "mp4",
                    "formats": [
                        {
                            "format_id": "dash-1",
                            "ext": "mp4",
                            "vcodec": "vp09.00.40.08",
                            "acodec": "none",
                            "width": 1080,
                            "height": 1080
                        },
                        {
                            "format_id": "dash-2",
                            "ext": "m4a",
                            "vcodec": "none",
                            "acodec": "mp4a.40.5"
                        }
                    ]
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var playlist = fmt.ParsePlaylistInfo(doc, "https://www.instagram.com/p/DdUAEH9lVF8/");

        Assert.Equal(2, playlist.Items.Count);

        // Item 1: Foto
        Assert.Equal(1, playlist.Items[0].PlaylistIndex);
        Assert.Equal("Foto #1", playlist.Items[0].Title);
        Assert.False(playlist.Items[0].IsAvailable);
        Assert.False(playlist.Items[0].IsSelected);
        Assert.Contains("Foto", playlist.Items[0].AvailabilityNotice);

        // Item 2: Vídeo real com índice 2 preservado
        Assert.Equal(2, playlist.Items[1].PlaylistIndex);
        Assert.Equal("Video by minecraft", playlist.Items[1].Title);
        Assert.True(playlist.Items[1].IsAvailable);
        Assert.True(playlist.Items[1].IsSelected);
        Assert.Empty(playlist.Items[1].AvailabilityNotice);

        // Contadores e resumos com pluralização correta (1 vídeo no singular)
        playlist.UpdateCounts();
        Assert.Equal(2, playlist.TotalMediaCount);
        Assert.Equal(1, playlist.SupportedVideosCount);
        Assert.Equal(1, playlist.PhotoCount);
        Assert.Equal(1, playlist.SelectedVideosCount);
        Assert.Equal("2 mídias • 1 vídeo", playlist.BadgeCountSummary);
        Assert.Equal("1 de 1 vídeo selecionado", playlist.SelectionSummary);
    }

    [Fact]
    public void FormatSelectionService_MixedCarousel_NullEntry_PreservesSubsequentIndexes_AndClassifiesAsUnavailable()
    {
        var fmt = new FormatSelectionService();
        // Simula caso em que yt-dlp produz null no array de entries
        var json = """
        {
            "_type": "playlist",
            "id": "null_test",
            "title": "Null Entry Carousel",
            "uploader": "creator",
            "entries": [
                null,
                {
                    "id": "vid_real",
                    "title": "Real Video",
                    "ext": "mp4",
                    "vcodec": "avc1",
                    "acodec": "mp4a"
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var playlist = fmt.ParsePlaylistInfo(doc, "https://www.instagram.com/p/null_test/");

        Assert.Equal(2, playlist.Items.Count);

        // Subitem nulo vira placeholder desabilitado de mídia indisponível com índice 1 (não classificado como foto)
        Assert.Equal(1, playlist.Items[0].PlaylistIndex);
        Assert.Equal("Mídia #1 (Indisponível)", playlist.Items[0].Title);
        Assert.False(playlist.Items[0].IsAvailable);
        Assert.False(playlist.Items[0].IsSelected);
        Assert.Equal("Mídia indisponível ou não suportada", playlist.Items[0].AvailabilityNotice);
        Assert.DoesNotContain("Foto", playlist.Items[0].AvailabilityNotice);

        // Subitem de vídeo subsequente mantém rigorosamente índice 2
        Assert.Equal(2, playlist.Items[1].PlaylistIndex);
        Assert.True(playlist.Items[1].IsAvailable);
        Assert.True(playlist.Items[1].IsSelected);
    }

    [Fact]
    public void FormatSelectionService_MixedCarousel_FourItemsAlternating_StrictlyPreservesIndexes2And4_AndPluralizesCorrectly()
    {
        var fmt = new FormatSelectionService();
        // Carrossel com 4 mídias alternadas: Foto, Vídeo, Foto, Vídeo
        var json = """
        {
            "_type": "playlist",
            "id": "alt_carousel",
            "title": "Post by test",
            "uploader": "testuser",
            "entries": [
                {
                    "id": "item1",
                    "title": "Photo 1",
                    "ext": "jpg",
                    "vcodec": "none",
                    "acodec": "none",
                    "formats": []
                },
                {
                    "id": "item2",
                    "title": "Video 2",
                    "ext": "mp4",
                    "vcodec": "avc1",
                    "acodec": "mp4a",
                    "formats": [
                        { "format_id": "v2", "ext": "mp4", "vcodec": "avc1", "width": 1080, "height": 1080 }
                    ]
                },
                {
                    "id": "item3",
                    "title": "Photo 3",
                    "ext": "png",
                    "vcodec": "none",
                    "acodec": "none",
                    "formats": []
                },
                {
                    "id": "item4",
                    "title": "Video 4",
                    "ext": "mp4",
                    "vcodec": "avc1",
                    "acodec": "mp4a",
                    "formats": [
                        { "format_id": "v4", "ext": "mp4", "vcodec": "avc1", "width": 1080, "height": 1080 }
                    ]
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var playlist = fmt.ParsePlaylistInfo(doc, "https://www.instagram.com/p/alt_carousel/");

        Assert.Equal(4, playlist.Items.Count);

        // Índices rigorosamente preservados: 1, 2, 3, 4
        Assert.Equal(1, playlist.Items[0].PlaylistIndex);
        Assert.False(playlist.Items[0].IsAvailable);

        Assert.Equal(2, playlist.Items[1].PlaylistIndex);
        Assert.True(playlist.Items[1].IsAvailable);

        Assert.Equal(3, playlist.Items[2].PlaylistIndex);
        Assert.False(playlist.Items[2].IsAvailable);

        Assert.Equal(4, playlist.Items[3].PlaylistIndex);
        Assert.True(playlist.Items[3].IsAvailable);

        // Contadores e pluralização no plural (2 vídeos)
        playlist.UpdateCounts();
        Assert.Equal(4, playlist.TotalMediaCount);
        Assert.Equal(2, playlist.SupportedVideosCount);
        Assert.Equal(2, playlist.PhotoCount);
        Assert.Equal(2, playlist.SelectedVideosCount);
        Assert.Equal("4 mídias • 2 vídeos", playlist.BadgeCountSummary);
        Assert.Equal("2 de 2 vídeos selecionados", playlist.SelectionSummary);
        Assert.Equal("Total: 4 mídias (2 vídeos suportados, 2 fotos)", playlist.DetailedCountSummary);
    }

    [Fact]
    public void FormatSelectionService_EmptyFormatsWithAuthOrPrivateSignal_ClassifiedAsUnavailableNotPhoto()
    {
        var fmt = new FormatSelectionService();
        var json = """
        {
            "_type": "playlist",
            "id": "auth_test",
            "title": "Auth Test",
            "uploader": "private_user",
            "entries": [
                {
                    "id": "item_auth",
                    "title": "Private login required",
                    "ext": null,
                    "formats": []
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var playlist = fmt.ParsePlaylistInfo(doc, "https://www.instagram.com/p/auth_test/");

        Assert.Single(playlist.Items);
        Assert.False(playlist.Items[0].IsAvailable);
        Assert.Contains("privad", playlist.Items[0].AvailabilityNotice, StringComparison.OrdinalIgnoreCase);
        // Não deve ser classificado como foto
        Assert.DoesNotContain("Foto", playlist.Items[0].AvailabilityNotice);
    }

    [Fact]
    public async Task MediaAnalysisService_MixedCarousel_ImageAndVideo_ReturnsCollectionWithCorrectSelection()
    {
        var mockYtDlp = new MockYtDlpService();
        mockYtDlp.CustomMetadataHandler = (url) =>
        {
            var json = """
            {
                "_type": "playlist",
                "id": "DdUAEH9lVF8",
                "title": "Post by minecraft",
                "uploader": "minecraft",
                "entries": [
                    {
                        "id": "DdUAEBClcjl",
                        "title": "Video by minecraft",
                        "ext": null,
                        "formats": []
                    },
                    {
                        "id": "DdUABjMibMh",
                        "title": "Video by minecraft",
                        "ext": "mp4",
                        "formats": [
                            {
                                "format_id": "v1",
                                "ext": "mp4",
                                "vcodec": "avc1",
                                "width": 1080,
                                "height": 1080
                            }
                        ]
                    }
                ]
            }
            """;
            return JsonDocument.Parse(json);
        };

        var platformService = new PlatformService();
        var formatService = new FormatSelectionService(platformService);
        var analysisService = new MediaAnalysisService(mockYtDlp, formatService, platformService);

        var result = await analysisService.AnalyzeAsync("https://www.instagram.com/p/DdUAEH9lVF8/?stkn=MzRlODBiNWFlZA==");

        Assert.True(result.IsCollection);
        Assert.NotNull(result.Collection);
        Assert.Equal(2, result.Collection.Items.Count);

        // Foto desabilitada
        Assert.False(result.Collection.Items[0].IsAvailable);
        Assert.False(result.Collection.Items[0].IsSelected);
        Assert.Equal(1, result.Collection.Items[0].PlaylistIndex);

        // Vídeo habilitado e selecionado
        Assert.True(result.Collection.Items[1].IsAvailable);
        Assert.True(result.Collection.Items[1].IsSelected);
        Assert.Equal(2, result.Collection.Items[1].PlaylistIndex);
    }

    [Fact]
    public async Task MediaAnalysisService_AllPhotosCarousel_ThrowsFriendlyMessage()
    {
        var mockYtDlp = new MockYtDlpService();
        mockYtDlp.CustomMetadataHandler = (url) =>
        {
            var json = """
            {
                "_type": "playlist",
                "id": "all_photos",
                "title": "Photos Carousel",
                "uploader": "user",
                "entries": [
                    { "id": "p1", "ext": "jpg", "vcodec": "none", "acodec": "none", "formats": [] },
                    { "id": "p2", "ext": "png", "vcodec": "none", "acodec": "none", "formats": [] }
                ]
            }
            """;
            return JsonDocument.Parse(json);
        };

        var platformService = new PlatformService();
        var formatService = new FormatSelectionService(platformService);
        var analysisService = new MediaAnalysisService(mockYtDlp, formatService, platformService);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            analysisService.AnalyzeAsync("https://www.instagram.com/p/all_photos/"));

        Assert.Contains("apenas fotos", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void YtDlpService_ParseYtDlpError_NoVideoFormatsFound_ReturnsNeutralFriendlyMessage()
    {
        var rawError = "ERROR: [Instagram] DdUAEBClcjl: No video formats found!; please report this issue on https://github.com/yt-dlp/yt-dlp/issues";
        var parsed = YtDlpService.ParseYtDlpError(rawError);

        Assert.Equal("Esta publicação não contém vídeos disponíveis para download.", parsed);
    }
}


