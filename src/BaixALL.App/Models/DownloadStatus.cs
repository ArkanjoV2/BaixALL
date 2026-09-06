namespace BaixALL.App.Models;

public enum DownloadStatus
{
    Queued,              // Na fila
    Preparing,           // Preparando
    DownloadingVideo,    // Baixando vídeo
    DownloadingAudio,    // Baixando áudio
    Merging,             // Mesclando áudio e vídeo
    Converting,          // Convertendo
    Finalizing,          // Finalizando
    Completed,           // Concluído
    Canceled,            // Cancelado
    Error                // Erro
}
