using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class ProgressParsingTests
{
    [Fact]
    public void ParseProgressLine_ShouldExtractAllFieldsCorrectly()
    {
        var line = "baixall_prog:[10485760/41943040|2097152|15|25.0%|downloading]";

        var success = YtDlpService.ParseProgressLine(
            line,
            out var downloaded,
            out var total,
            out var speed,
            out var eta,
            out var percent,
            out var status);

        Assert.True(success);
        Assert.Equal(10485760, downloaded);
        Assert.Equal(41943040, total);
        Assert.Equal(2097152, speed);
        Assert.Equal(15, eta);
        Assert.Equal(25.0, percent);
        Assert.Equal("downloading", status);
    }

    [Fact]
    public void ParseProgressLine_ShouldHandleNAValues()
    {
        var line = "baixall_prog:[NA/NA|NA|NA|NA|finished]";

        var success = YtDlpService.ParseProgressLine(
            line,
            out var downloaded,
            out var total,
            out var speed,
            out var eta,
            out var percent,
            out var status);

        Assert.True(success);
        Assert.Equal(0, downloaded);
        Assert.Null(total);
        Assert.Equal(0, speed);
        Assert.Null(eta);
        Assert.Equal(0, percent);
        Assert.Equal("finished", status);
    }

    [Fact]
    public void ParseProgressLine_ShouldReturnFalseForNonProgressLines()
    {
        var line = "[download] 10% of 20MB at 2MB/s";

        var success = YtDlpService.ParseProgressLine(
            line,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _);

        Assert.False(success);
    }

    [Theory]
    [InlineData("ERROR: Private video. Sign in if you've been granted access", "Este vídeo é privado e não pode ser acessado publicamente.")]
    [InlineData("ERROR: Video unavailable. This video is not available", "Este vídeo não está disponível no YouTube.")]
    [InlineData("ERROR: This video has been removed by the uploader", "Este vídeo foi removido pelo YouTube ou pelo criador.")]
    [InlineData("ERROR: Sign in to confirm your age", "Este vídeo requer autenticação de idade e não pode ser baixado sem login.")]
    [InlineData("ERROR: [youtube] Incomplete YouTube ID", "A URL informada não é válida para o YouTube.")]
    [InlineData("ERROR: HTTP Error 403: Forbidden", "O YouTube recusou o acesso (HTTP 403). Tente atualizar o yt-dlp nas configurações.")]
    [InlineData("ERROR: unable to download video data: <urlopen error [Errno 11001] getaddrinfo failed>", "Falha de conexão com o YouTube. Verifique sua conexão com a internet.")]
    public void ParseYtDlpError_ShouldMapKnownErrorsToFriendlyPortugueseMessages(string rawError, string expectedSnippet)
    {
        var result = YtDlpService.ParseYtDlpError(rawError);
        Assert.Equal(expectedSnippet, result);
    }
}
