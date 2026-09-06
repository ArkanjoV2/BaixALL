using System;
using System.IO;
using BaixALL.App.Helpers;
using BaixALL.App.Models;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using Xunit;

namespace BaixALL.Tests;

public class DownloadQueueTests
{
    [Fact]
    public void DownloadItemViewModel_ShouldUpdateProgressCorrectly()
    {
        var item = new DownloadItemViewModel
        {
            Title = "Vídeo Teste"
        };

        Assert.True(item.IsActive);
        Assert.False(item.IsCompleted);

        item.UpdateProgress(new DownloadProgressReport
        {
            Status = DownloadStatus.DownloadingVideo,
            StatusMessage = "Baixando vídeo...",
            DownloadedBytes = 10485760, // 10 MB
            TotalBytes = 20971520,      // 20 MB
            SpeedBytesPerSec = 2097152, // 2 MB/s
            EtaSeconds = 5,
            Percentage = 50.0
        });

        Assert.Equal(50.0, item.ProgressPercentage);
        Assert.Equal("10 MB", item.DownloadedSizeText);
        Assert.Equal("20 MB", item.TotalSizeText);
        Assert.Equal("2 MB/s", item.SpeedText);
        Assert.Equal("5s", item.EtaText);
        Assert.True(item.IsActive);

        item.MarkCompleted(@"C:\Downloads\video.mp4");
        Assert.True(item.IsCompleted);
        Assert.False(item.IsActive);
        Assert.Equal(100, item.ProgressPercentage);
        Assert.Equal(@"C:\Downloads\video.mp4", item.DestinationPath);
    }

    [Fact]
    public void ByteSizeFormatter_ShouldFormatUnitsProperly()
    {
        Assert.Equal("0 B", ByteSizeFormatter.Format(0));
        Assert.Equal("500 B", ByteSizeFormatter.Format(500));
        Assert.Equal("1 KB", ByteSizeFormatter.Format(1024));
        Assert.Equal("1.5 MB", ByteSizeFormatter.Format(1572864));
        Assert.Equal("1 GB", ByteSizeFormatter.Format(1073741824));
    }

    [Fact]
    public void HistoryService_ShouldAddAndClearItems()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"baixall_history_test_{Guid.NewGuid()}.json");

        try
        {
            var service = new HistoryService(historyFilePath: tempFile);
            Assert.Empty(service.GetHistory());

            service.AddItem(new HistoryItem
            {
                Title = "Vídeo Teste",
                FinalFilePath = @"C:\Test\video.mp4"
            });

            Assert.Single(service.GetHistory());

            service.ClearHistory();
            Assert.Empty(service.GetHistory());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
