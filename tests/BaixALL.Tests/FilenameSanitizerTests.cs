using System.IO;
using BaixALL.App.Helpers;
using Xunit;

namespace BaixALL.Tests;

public class FilenameSanitizerTests
{
    [Theory]
    [InlineData("Video: Title? *Special* <Characters> | \"Test\" / Back\\slash", "Video_ Title_ _Special_ _Characters_ _ _Test_ _ Back_slash")]
    [InlineData("Vídeo com Acentuação e Espaços", "Vídeo com Acentuação e Espaços")]
    [InlineData("nome.com.pontos....", "nome.com.pontos")]
    [InlineData("   espaços no inicio e fim   ", "espaços no inicio e fim")]
    [InlineData("", "video")]
    [InlineData(null, "video")]
    public void SanitizeFileName_ShouldSanitizeIllegalWindowsCharacters(string? input, string expected)
    {
        var result = FileHelper.SanitizeFileName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("CON", "CON_file")]
    [InlineData("PRN", "PRN_file")]
    [InlineData("AUX", "AUX_file")]
    [InlineData("NUL", "NUL_file")]
    [InlineData("COM1", "COM1_file")]
    [InlineData("LPT1", "LPT1_file")]
    public void SanitizeFileName_ShouldProtectReservedWindowsNames(string input, string expected)
    {
        var result = FileHelper.SanitizeFileName(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeFileName_ShouldTruncateExtremelyLongNames()
    {
        var extremelyLong = new string('A', 300);
        var result = FileHelper.SanitizeFileName(extremelyLong);
        Assert.True(result.Length <= 180);
    }

    [Fact]
    public void GetUniqueFilePath_ShouldGenerateNumberedSuffixIfFileExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "BaixALL_Test_" + System.Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var file1 = Path.Combine(tempDir, "video.mp4");
            File.WriteAllText(file1, "dummy");

            var unique1 = FileHelper.GetUniqueFilePath(tempDir, "video", "mp4");
            Assert.Equal(Path.Combine(tempDir, "video (1).mp4"), unique1);

            File.WriteAllText(unique1, "dummy");
            var unique2 = FileHelper.GetUniqueFilePath(tempDir, "video", "mp4");
            Assert.Equal(Path.Combine(tempDir, "video (2).mp4"), unique2);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
