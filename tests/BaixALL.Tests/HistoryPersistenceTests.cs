using System;
using System.IO;
using System.Threading.Tasks;
using BaixALL.App.Models;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using Xunit;

namespace BaixALL.Tests;

public class HistoryPersistenceTests
{
    private class TestDispatcherService : IDispatcherService
    {
        public void Invoke(Action action) => action();
        public async Task InvokeAsync(Action action) { action(); await Task.CompletedTask; }
        public async Task<T> InvokeAsync<T>(Func<T> func) => await Task.FromResult(func());
        public bool CheckAccess() => true;
    }

    [Fact]
    public void HistoryService_PersistsToJsonFile_AndRestoresOnNewInstance()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"history_persist_{Guid.NewGuid()}.json");

        try
        {
            var service1 = new HistoryService(historyFilePath: tempFile);
            service1.AddItem(new HistoryItem
            {
                Title = "Primeiro Vídeo",
                Quality = "1080p (Full HD)",
                Format = "MP4",
                FinalFilePath = @"C:\Videos\video1.mp4",
                FileSizeBytes = 50000000,
                Status = "Concluído",
                ThumbnailUrl = "https://thumb.url/1.jpg"
            });

            service1.AddItem(new HistoryItem
            {
                Title = "Segundo Vídeo",
                Quality = "4K (2160p)",
                Format = "MKV",
                FinalFilePath = @"C:\Videos\video2.mkv",
                FileSizeBytes = 200000000,
                Status = "Concluído",
                ThumbnailUrl = "https://thumb.url/2.jpg"
            });

            Assert.Equal(2, service1.GetHistory().Count);

            // Simula reinicialização do aplicativo instanciando um novo HistoryService lendo o mesmo arquivo
            var service2 = new HistoryService(historyFilePath: tempFile);
            var restored = service2.GetHistory();

            Assert.Equal(2, restored.Count);
            Assert.Equal("Segundo Vídeo", restored[0].Title); // Mais recente primeiro
            Assert.Equal("Primeiro Vídeo", restored[1].Title);
            Assert.Equal("MKV", restored[0].Format);
            Assert.Equal(200000000, restored[0].FileSizeBytes);
            Assert.Equal(@"C:\Videos\video2.mkv", restored[0].FinalFilePath);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ClearHistory_ShouldOnlyClearJsonRecords_AndNotDeleteDownloadedVideoFiles()
    {
        var tempHistFile = Path.Combine(Path.GetTempPath(), $"history_clear_{Guid.NewGuid()}.json");
        var tempVideoFile = Path.Combine(Path.GetTempPath(), $"real_downloaded_video_{Guid.NewGuid()}.mp4");
        File.WriteAllText(tempVideoFile, "Important downloaded video content that must never be deleted on history clear");

        try
        {
            var historyService = new HistoryService(historyFilePath: tempHistFile);
            var dispatcher = new TestDispatcherService();
            var vm = new HistoryViewModel(historyService, dispatcher);

            historyService.AddItem(new HistoryItem
            {
                Title = "Vídeo Baixado Importante",
                FinalFilePath = tempVideoFile,
                FileSizeBytes = 12345,
                Status = "Concluído"
            });

            Assert.Single(vm.Items);
            Assert.True(vm.HasItems);
            Assert.True(File.Exists(tempVideoFile));

            // Executa o comando "Limpar Histórico"
            vm.ClearAllCommand.Execute(null);

            // Verifica que a interface e o JSON foram limpos
            Assert.Empty(vm.Items);
            Assert.False(vm.HasItems);
            Assert.Empty(historyService.GetHistory());

            // REGRA CRÍTICA: O arquivo de vídeo físico NÃO pode ser apagado!
            Assert.True(File.Exists(tempVideoFile), "O arquivo de vídeo deve permanecer intocado no disco após limpar o histórico.");
        }
        finally
        {
            if (File.Exists(tempHistFile)) File.Delete(tempHistFile);
            if (File.Exists(tempVideoFile)) File.Delete(tempVideoFile);
        }
    }

    [Fact]
    public void HistoryViewModel_RemoveSingleItem_ShouldUpdateBothCollectionAndPersistence()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"history_rem_{Guid.NewGuid()}.json");

        try
        {
            var service = new HistoryService(historyFilePath: tempFile);
            var dispatcher = new TestDispatcherService();
            var vm = new HistoryViewModel(service, dispatcher);

            var item1 = new HistoryItem { Title = "Vídeo A", FinalFilePath = @"C:\a.mp4" };
            var item2 = new HistoryItem { Title = "Vídeo B", FinalFilePath = @"C:\b.mp4" };

            service.AddItem(item1);
            service.AddItem(item2);

            Assert.Equal(2, vm.Items.Count);

            // Remove o primeiro item
            vm.RemoveItemCommand.Execute(vm.Items[0]);

            Assert.Single(vm.Items);
            Assert.Single(service.GetHistory());
            Assert.Equal("Vídeo A", vm.Items[0].Title);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void HistoryService_LoadsLegacyV12History_DefaultsPlatformToYouTube_WithoutDataLoss()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"history_legacy_{Guid.NewGuid():N}.json");

        // Simula exatamente o JSON gerado pela versão 1.2.0 (sem Platform e sem CanonicalKey)
        var legacyJson = """
        [
          {
            "Id": "11111111-1111-1111-1111-111111111111",
            "VideoId": "dQw4w9WgXcQ",
            "Title": "Never Gonna Give You Up (v1.2.0 Legacy)",
            "Channel": "Rick Astley",
            "Quality": "1080p (Full HD)",
            "Format": "MP4",
            "FinalFilePath": "C:\\Downloads\\rick.mp4",
            "DownloadDate": "2026-08-15T10:30:00",
            "FileSizeBytes": 75000000,
            "Status": "Concluído",
            "ThumbnailUrl": "https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg"
          }
        ]
        """;

        try
        {
            File.WriteAllText(tempFile, legacyJson);

            // Carrega com o HistoryService da 1.3.0
            var service = new HistoryService(historyFilePath: tempFile);
            var items = service.GetHistory();

            Assert.Single(items);
            var item = items[0];

            // Verifica que dados originais foram preservados intactos
            Assert.Equal("Never Gonna Give You Up (v1.2.0 Legacy)", item.Title);
            Assert.Equal("Rick Astley", item.Channel);
            Assert.Equal("1080p (Full HD)", item.Quality);
            Assert.Equal("MP4", item.Format);
            Assert.Equal("C:\\Downloads\\rick.mp4", item.FinalFilePath);
            Assert.Equal(75000000, item.FileSizeBytes);

            // Verifica que a plataforma recebeu fallback seguro para "YouTube"
            Assert.Equal("YouTube", item.Platform);
            Assert.Equal("YouTube", item.PlatformDisplayName);
            Assert.Equal("#FF4444", item.PlatformBadgeColor);
            Assert.Equal("#331414", item.PlatformBackgroundColor);

            // Adiciona novo item da 1.3.0 com Instagram e CanonicalKey
            service.AddItem(new HistoryItem
            {
                Title = "Instagram Reel Moderno",
                Platform = "Instagram",
                CanonicalKey = "ig:Chunk8-jurw",
                FinalFilePath = @"C:\Downloads\reel.mp4",
                FileSizeBytes = 1800000
            });

            // Recarrega em uma nova instância para verificar persistência completa
            var service2 = new HistoryService(historyFilePath: tempFile);
            var reloaded = service2.GetHistory();

            Assert.Equal(2, reloaded.Count);
            Assert.Equal("Instagram Reel Moderno", reloaded[0].Title);
            Assert.Equal("Instagram", reloaded[0].Platform);
            Assert.Equal("ig:Chunk8-jurw", reloaded[0].CanonicalKey);

            Assert.Equal("Never Gonna Give You Up (v1.2.0 Legacy)", reloaded[1].Title);
            Assert.Equal("YouTube", reloaded[1].Platform);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData("YouTube", "YouTube", "#FF4444", "#331414")]
    [InlineData("Instagram", "Instagram", "#F472B6", "#331424")]
    [InlineData("X / Twitter", "X / Twitter", "#38BDF8", "#0C2538")]
    [InlineData("Twitter", "Twitter", "#38BDF8", "#0C2538")]
    [InlineData("", "Desconhecido", "#94A3B8", "#1E293B")]
    [InlineData(null, "Desconhecido", "#94A3B8", "#1E293B")]
    public void HistoryItem_UIProperties_ReturnExpectedBadgesAndColors(
        string? platform,
        string expectedDisplayName,
        string expectedBadgeColor,
        string expectedBgColor)
    {
        var item = new HistoryItem
        {
            Platform = platform!
        };

        Assert.Equal(expectedDisplayName, item.PlatformDisplayName);
        Assert.Equal(expectedBadgeColor, item.PlatformBadgeColor);
        Assert.Equal(expectedBgColor, item.PlatformBackgroundColor);
    }

    [Fact]
    public void HistoryService_InferPlatform_IdentifiesPlatformsFromEvidenceOrReturnsDesconhecido()
    {
        var itemYt = new HistoryItem { VideoId = "dQw4w9WgXcQ" };
        var itemIg = new HistoryItem { ThumbnailUrl = "https://instagram.fbcdn.net/p/xyz.jpg" };
        var itemX = new HistoryItem { CanonicalKey = "x:12345678" };
        var itemUnknown = new HistoryItem { Title = "Arquivo Local Sem Plataforma", VideoId = "" };

        Assert.Equal("YouTube", HistoryService.InferPlatform(itemYt));
        Assert.Equal("Instagram", HistoryService.InferPlatform(itemIg));
        Assert.Equal("X / Twitter", HistoryService.InferPlatform(itemX));
        Assert.Equal("Desconhecido", HistoryService.InferPlatform(itemUnknown));
    }
}
