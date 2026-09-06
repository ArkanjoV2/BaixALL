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
}
