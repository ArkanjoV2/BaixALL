using System.IO;
using System.Linq;
using System.Text.Json;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class PlaylistParsingTests
{
    private static JsonDocument LoadSampleJson()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "real_youtube_flat_playlist_sample.json");
        if (!File.Exists(path))
        {
            // Tenta caminho relativo a partir do projeto
            path = Path.Combine("TestData", "real_youtube_flat_playlist_sample.json");
        }
        var jsonContent = File.ReadAllText(path);
        return JsonDocument.Parse(jsonContent);
    }

    [Fact]
    public void ParsePlaylistInfo_ShouldExtractMetadataCorrectly()
    {
        using var jsonDoc = LoadSampleJson();
        var service = new FormatSelectionService();

        var playlist = service.ParsePlaylistInfo(jsonDoc, "https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9");

        Assert.Equal("PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9", playlist.Id);
        Assert.Equal("Curso de C# e .NET 10 Completo", playlist.Title);
        Assert.Equal("DevTech Brasil", playlist.Channel);
        Assert.Equal(5, playlist.TotalVideosCount);
        Assert.Equal(5, playlist.Items.Count);
        Assert.NotEmpty(playlist.ThumbnailUrl);
    }

    [Fact]
    public void ParsePlaylistInfo_ShouldHandleAvailableAndUnavailableItems()
    {
        using var jsonDoc = LoadSampleJson();
        var service = new FormatSelectionService();

        var playlist = service.ParsePlaylistInfo(jsonDoc, "https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GzKtS_gO_Yj8d_6bV9");

        // Itens 1, 2 e 3 são disponíveis
        Assert.True(playlist.Items[0].IsAvailable);
        Assert.True(playlist.Items[0].IsSelected);
        Assert.Equal("01 - Introdução ao .NET 10", playlist.Items[0].Title);
        Assert.Equal(322.0, playlist.Items[0].DurationSeconds);
        Assert.Equal("5:22", playlist.Items[0].FormattedDuration);
        Assert.Equal("#01", playlist.Items[0].FormattedIndex);

        Assert.True(playlist.Items[1].IsAvailable);
        Assert.Equal(754.0, playlist.Items[1].DurationSeconds);
        Assert.Equal("12:34", playlist.Items[1].FormattedDuration);

        // Item 3 é live stream com duração nula
        Assert.True(playlist.Items[2].IsAvailable);
        Assert.Null(playlist.Items[2].DurationSeconds);
        Assert.Equal("—", playlist.Items[2].FormattedDuration);

        // Item 4 é vídeo privado
        Assert.False(playlist.Items[3].IsAvailable);
        Assert.False(playlist.Items[3].IsSelected);
        Assert.Equal("Vídeo indisponível ou privado", playlist.Items[3].AvailabilityNotice);

        // Item 5 é vídeo excluído
        Assert.False(playlist.Items[4].IsAvailable);
        Assert.False(playlist.Items[4].IsSelected);

        // Total de selecionados por padrão deve ser 3 (apenas os disponíveis)
        Assert.Equal(3, playlist.SelectedVideosCount);
    }

    [Fact]
    public void BuildBatchFormatOptions_ShouldContainExpectedFormatsWithFallbackSelectors()
    {
        var service = new FormatSelectionService();
        var options = service.BuildBatchFormatOptions();

        Assert.NotNull(options);
        Assert.True(options.Count >= 5);

        // Opção padrão
        var best = options.First();
        Assert.True(best.IsBestQuality);
        Assert.Equal("bestvideo+bestaudio/best", best.FormatSelector);

        // Opção 1080p com fallback
        var opt1080 = options.FirstOrDefault(x => x.Height == 1080);
        Assert.NotNull(opt1080);
        Assert.Contains("bestvideo[height<=1080]+bestaudio", opt1080.FormatSelector);

        // Opção somente áudio
        var audioOpt = options.FirstOrDefault(x => x.IsAudioOnly);
        Assert.NotNull(audioOpt);
        Assert.Equal("bestaudio/best", audioOpt.FormatSelector);
    }
}
