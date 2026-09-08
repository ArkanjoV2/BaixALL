# Planejamento Arquitetural e Técnico — BaixALL 1.2.0
## Suporte a Playlists do YouTube e Downloads em Lote

---

## 1. Contexto e Preparação do Repositório

* **Versão Atual Estável:** BaixALL 1.1.0 (Publicada como `Latest` em 08/09/2026).
* **Auditoria de PR e Branch:**
  * Pull Request **#13** (*docs: atualizar documentação, links de download e screenshots para v1.1.0*) confirmado como **MERGED** no commit `071cc13fa810d9fe507e6269d981ede5c95dfa13`.
  * Branch principal `main` sincronizada com `origin/main`, com árvore de trabalho limpa.
  * Status do GitHub Actions CI na `main`: **PASS** (Run 34177728517 aprovado com 122 testes).
* **Branch de Trabalho Criada:** `feat/playlist-batch-downloads`.
* **Garantias de Integridade:** As tags `v1.0.0` e `v1.1.0` e seus respectivos binários publicados e hashes SHA-256 permanecem 100% inalterados e protegidos.

---

## 2. Diagnóstico Detalhado da Arquitetura Atual

A inspeção do código-fonte do BaixALL (v1.1.0) mapeou com exatidão os seguintes serviços, padrões e limites operacionais:

### 2.1. `YtDlpService` / `IYtDlpService`
* **Localização:** `src/BaixALL.App/Services/YtDlpService.cs`.
* **Métodos Atuais:**
  * `GetMetadataJsonAsync(string url, CancellationToken ct)`: Executa `yt-dlp` com `-J --no-playlist --skip-download --no-warnings --no-check-certificates`, além de `--ffmpeg-location`. Faz o parse da saída padrão como `JsonDocument`.
  * `DownloadAsync(DownloadRequest request, IProgress<DownloadProgressReport> progress, CancellationToken ct)`: Cria um diretório temporário exclusivo por job (`%TEMP%\BaixALL_Jobs\{jobId}`), executa `yt-dlp` com `--progress-template baixall_prog:[...]`, interpreta o progresso em tempo real e, ao final, move o arquivo para `request.DestinationFolder` via `FileHelper.GetUniqueFilePath`.
* **Ponto de Extensão:** Adicionar suporte à extração estruturada de playlists com `--flat-playlist -J`.

### 2.2. `YoutubeService` / `IYoutubeService`
* **Localização:** `src/BaixALL.App/Services/YoutubeService.cs`.
* **Responsabilidade:** Valida a URL com `UrlValidator`, normaliza-a, invoca `IYtDlpService.GetMetadataJsonAsync` e utiliza `IFormatSelectionService.ParseVideoInfo`.
* **Ponto de Extensão:** Adicionar método de análise de playlist (`AnalyzePlaylistAsync`), retornando um modelo agregado (`PlaylistInfo`).

### 2.3. `DownloadService` / `IDownloadService`
* **Localização:** `src/BaixALL.App/Services/DownloadService.cs`.
* **Estrutura de Fila:**
  * `QueueItems`: `ObservableCollection<DownloadItemViewModel>` exposta para a interface WPF.
  * `_pendingQueue`: `List<DownloadItemViewModel>` interna protegida por `lock (_syncLock)`.
  * `_activeCount` e `_maxConcurrent`: Controle rigoroso de concorrência (1 a 3 slots) através do método `TryStartNextDownloads()`.
  * `EnqueueDownload(DownloadRequest request, string thumbnailUrl)`: Insere o item na UI (`QueueItems.Insert(0, item)`) e na fila pendente.
  * `CancelDownload(Guid id)` / `CancelAllDownloads()`: Cancela o `CancellationTokenSource` individual do item e limpa a pasta temporária do job.
* **Ponto de Extensão:** A fila atual é **o motor ideal** para receber os itens da playlist. Cada vídeo do lote será um job individual gerenciado pela mesma fila, respeitando os slots globais sem abrir concorrência paralela desgovernada. Adicionar método de cancelamento por lote (`CancelBatch(Guid batchId)`).

### 2.4. `FormatSelectionService` / `IFormatSelectionService`
* **Localização:** `src/BaixALL.App/Services/FormatSelectionService.cs`.
* **Responsabilidade:** Extrai metadados de vídeo (`ParseVideoInfo`) e cria as opções de download (`BuildFormatOptions`). Utiliza seletores nativos do yt-dlp como:
  * `bestvideo+bestaudio/best` (Melhor qualidade)
  * `bestvideo[height<=1080]+bestaudio/best[height<=1080]/best` (Até 1080p)
  * `bestaudio/best` (Somente áudio)
* **Ponto de Extensão:** Construir o parser de metadados da playlist (`ParsePlaylistInfo`) e seletores de formato com fallback transparente para lotes.

### 2.5. `UrlValidator`
* **Localização:** `src/BaixALL.App/Helpers/UrlValidator.cs`.
* **Estado Atual:** A regex atual (`YouTubeRegex`) valida apenas vídeos (`watch?v=`, `shorts/`, `embed/`, `youtu.be/`).
* **Limitação Identificada:** Não valida links de playlist puros (`playlist?list=PL...`) e o método `NormalizeYouTubeUrl` descarta o parâmetro `&list=` se houver `watch?v=`.
* **Ponto de Extensão:** Atualizar a regex para suportar URLs de playlist, extrair `PlaylistId` e detectar URLs híbridas (`v=...&list=...`).

### 2.6. `HistoryService` / `IHistoryService`
* **Localização:** `src/BaixALL.App/Services/HistoryService.cs`.
* **Persistência:** Arquivo JSON em `%LOCALAPPDATA%\BaixALL\history.json`.
* **Aproveitamento:** 100% reutilizável. Cada vídeo concluído de um lote é gravado normalmente no histórico, permitindo que o usuário localize ou abra o arquivo concluído com os mesmos botões da v1.1.0.

### 2.7. `SettingsService` / `ISettingsService`
* **Localização:** `src/BaixALL.App/Services/SettingsService.cs`.
* **Persistência:** `%LOCALAPPDATA%\BaixALL\settings.json`.
* **Aproveitamento:** 100% compatível. As preferências de pasta padrão e limite de concorrência (1 a 3) permanecem válidas para os downloads em lote.

### 2.8. `DependencyManager` / `IDependencyManager`
* **Localização:** `src/BaixALL.App/Services/DependencyManager.cs`.
* **Ferramentas:** `yt-dlp` (v2026.08.19), `FFmpeg`, `ffprobe` e `Deno`.
* **Aproveitamento:** O `yt-dlp` instalado já possui suporte nativo integral aos comandos de playlist (`--flat-playlist`, `-J`, `--dump-single-json`). Nenhuma ferramenta nova é necessária.

### 2.9. `MainViewModel` e `MainWindow.xaml`
* **Localização:** `src/BaixALL.App/ViewModels/MainViewModel.cs` e `src/BaixALL.App/Views/MainWindow.xaml`.
* **Padrão:** MVVM com CommunityToolkit.Mvvm.
* **Ponto de Extensão:** Integrar a visualização da playlist diretamente na aba **Início (Downloader)**, alternando dinamicamente entre o card de vídeo individual e o card de playlist quando uma playlist for analisada.

---

## 3. Matriz de Reutilização de Componentes

| Componente | Status na v1.2.0 | Justificativa Técnica |
| :--- | :---: | :--- |
| **`ProcessRunner`** | **100% Reutilizado** | Execução assíncrona segura de processos com `ArgumentList` e encerramento de árvore de processos. |
| **`DependencyManager`** | **100% Reutilizado** | Caminhos e gerenciamento seguro de `yt-dlp`, `FFmpeg`, `ffprobe` e `Deno`. |
| **`HistoryService`** | **100% Reutilizado** | Registro individual de cada vídeo concluído do lote em `history.json`. |
| **`SettingsService`** | **100% Reutilizado** | Configurações de concorrência e pasta de download respeitadas integralmente. |
| **`FileHelper`** | **100% Reutilizado** | Higienização de nomes e geração de nomes únicos sem colisão (`GetUniqueFilePath`). |
| **`ByteSizeFormatter` / `TimeFormatter`** | **100% Reutilizado** | Formatação de tamanhos, velocidades e durações. |
| **`DownloadService`** | **Reutilizado com Extensão** | O motor de concorrência (1 a 3 slots) é preservado integralmente. Adiciona-se método para cancelar lote específico. |
| **`YtDlpService`** | **Reutilizado com Extensão** | Adiciona-se o método de extração rápida de metadados da playlist via `--flat-playlist -J`. |
| **`FormatSelectionService`** | **Reutilizado com Extensão** | Adiciona-se método para interpretar os itens da playlist e construir seletores com fallback de resolução. |
| **`UrlValidator`** | **Reutilizado com Extensão** | Adiciona-se validação de `playlist?list=` e detecção de links híbridos. |
| **`DownloadItemViewModel`** | **Reutilizado com Extensão** | Adiciona-se propriedade opcional `BatchId` e `PlaylistTitle` para agrupamento lógico. |

---

## 4. Novos Modelos e Serviços Propostos

> [!NOTE]
> Todas as classes abaixo são **propostas de engenharia** para a versão 1.2.0 e não alteram a assinatura das classes existentes da v1.1.0.

### 4.1. Modelo `PlaylistInfo` (Proposto)
Representa os metadados agregados da playlist extraídos pelo yt-dlp:
```csharp
namespace BaixALL.App.Models;

public class PlaylistInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public int TotalVideosCount { get; set; }
    public List<PlaylistItemInfo> Items { get; set; } = new();
}
```

### 4.2. Modelo `PlaylistItemInfo` (Proposto)
Representa um item individual descoberto na playlist:
```csharp
namespace BaixALL.App.Models;

public partial class PlaylistItemInfo : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public double? DurationSeconds { get; set; }
    public string FormattedDuration { get; set; } = "—";
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int PlaylistIndex { get; set; }

    [ObservableProperty]
    private bool _isSelected = true; // Marcado por padrão para download
}
```

### 4.3. Modelo `BatchDownloadInfo` (Proposto)
Representa o lote em execução na fila de downloads:
```csharp
namespace BaixALL.App.Models;

public class BatchDownloadInfo
{
    public Guid BatchId { get; } = Guid.NewGuid();
    public string PlaylistTitle { get; init; } = string.Empty;
    public int TotalSelectedItems { get; init; }
    public FormatOption SelectedFormat { get; init; } = new();
    public ContainerOption SelectedContainer { get; init; } = new();
    public AudioFormatOption SelectedAudioFormat { get; init; } = new();
    public bool IsAudioOnly { get; init; }
    public string DestinationFolder { get; init; } = string.Empty;
}
```

### 4.4. Modelo `BatchQualityOption` (Proposto)
Lista de resoluções de referência para o lote (funcionando como teto máximo com fallback):
1. **Melhor qualidade disponível (Padrão):** `bestvideo+bestaudio/best`
2. **Até 1080p (Full HD):** `bestvideo[height<=1080]+bestaudio/bestvideo[height<=1080]+bestaudio/best`
3. **Até 720p (HD):** `bestvideo[height<=720]+bestaudio/bestvideo[height<=720]+bestaudio/best`
4. **Até 480p:** `bestvideo[height<=480]+bestaudio/bestvideo[height<=480]+bestaudio/best`
5. **Até 360p:** `bestvideo[height<=360]+bestaudio/bestvideo[height<=360]+bestaudio/best`
6. **Somente Áudio:** Extração direta e conversão para MP3, M4A ou Opus.

---

## 5. Estratégia de Análise da Playlist (Duas Etapas)

A análise de playlists no YouTube apresenta dois desafios fundamentais:
1. Playlists com dezenas de itens podem levar vários minutos se forem extraídos todos os streams de mídia de cada vídeo antecipadamente.
2. O YouTube aplica *rate limiting* e desafios de bot quando há consultas excessivas de metadados em sequência rápida.

Para garantir rapidez, confiabilidade e estabilidade, adotamos a **Estratégia em Duas Etapas**:

### Etapa 1: Análise Rápida de Metadados (UI)
* **Comando Executado:**
  ```text
  yt-dlp -J --flat-playlist --skip-download --no-warnings --no-check-certificates [ffmpeg-location] <URL_PLAYLIST>
  ```
* **Vantagens:**
  * Execução em **uma única chamada rápida** (normalmente entre 1 e 3 segundos para 50+ vídeos).
  * Retorna o array estruturado `entries` com `id`, `title`, `duration`, `uploader`, `thumbnails`, `playlist_index`.
  * Nenhum download é iniciado nesta fase.
  * O app não fica bloqueado e consome o mínimo de CPU e rede.

### Etapa 2: Resolução de Formatos e Execução sob Demanda (Fila)
* A análise aprofundada dos formatos de cada vídeo é postergada para o momento em que o vídeo obtém sua vaga na fila de concorrência do `DownloadService`.
* O `yt-dlp` recebe a instrução de qualidade com o teto estabelecido (ex: `bestvideo[height<=1080]+bestaudio/best`). Se o vídeo tiver 1080p, baixa em 1080p. Se o vídeo só existir em 720p ou 480p, ele baixa a melhor qualidade disponível sem falhar!

---

## 6. Proposta de Interface da Playlist

A interface mantém a identidade visual da v1.1.0 (Dark Theme, azul tech `#0098FF`, bordas retas, fontes limpas).

### 6.1. Fluxo na Aba Início (Home)
A Home continua sendo o ponto de entrada principal:
1. **Entrada de URL Unificada:** O usuário cola a URL no mesmo campo de entrada.
2. **Detecção Automática:**
   * Se for um vídeo individual: exibe o card de vídeo único existente da v1.1.0 (100% inalterado).
   * Se for uma playlist: oculta o card individual e exibe o **Card de Análise de Playlist**.
   * Se for uma URL híbrida (`watch?v=...&list=...`): exibe um diálogo/pergunta simples no banner: *"Deseja baixar apenas este vídeo ou analisar a playlist completa?"*.
3. **Card de Análise de Playlist (Componentes):**
   * **Cabeçalho:** Miniatura principal da playlist, título em destaque, canal do criador, contador total de vídeos identificados e tag indicativa `Playlist do YouTube`.
   * **Barra de Controle de Lote:**
     * Checkbox geral "Selecionar Todos" / Botão "Desmarcar Todos".
     * Contador de seleção em tempo real: `X de Y vídeos selecionados`.
   * **Lista de Vídeos com Virtualização WPF:**
     * `ListView` virtualizada com `VirtualizingStackPanel.IsVirtualizing="True"` para garantir rolagem suave a 60 FPS mesmo com 200+ vídeos.
     * Cada linha contém: checkbox individual, índice numérico (`#01`), miniatura reduzida, título do vídeo e duração (`⏱ 12:34`).
   * **Configurações Padrão do Lote:**
     * Combobox "Qualidade do Lote": *Melhor disponível*, *Até 1080p*, *Até 720p*, *Até 480p*, etc.
     * Combobox "Formato / Container": *Automático*, *MP4*, *MKV*.
     * Seção "Somente Áudio em Lote": Checkbox com seletor de formato de áudio (MP3 320kbps, M4A, Opus).
     * Pasta de destino com botão "Escolher pasta".
   * **Botões de Ação:**
     * Botão Secundário: *"← Limpar / Outra URL"*.
     * Botão Primário Destacado: *"Adicionar X Vídeos à Fila de Downloads"*.

### 6.2. Prévia Visual Interativa (Mockup)
Um wireframe interativo fiel à paleta do BaixALL 1.1.0 foi criado no artefato:
* Arquivo: `playlist_ui_mockup.html`

---

## 7. Fila e Controle de Concorrência

O BaixALL **não criará um motor de download paralelo**. O `DownloadService` existente será o único executor de downloads do aplicativo.

### 7.1. Concorrência Global Estrita
* O limite de downloads concorrentes configurado pelo usuário (1, 2 ou 3) no `SettingsService` é absoluto.
* Se o usuário enfileirar 1 vídeo avulso e uma playlist de 20 vídeos com limite 2:
  * O vídeo avulso e o primeiro vídeo da playlist executam simultaneamente.
  * Os outros 19 vídeos da playlist aguardam na fila.
  * Nenhum processo adicional é iniciado fora dos slots configurados.

### 7.2. Identificação e Rastreamento de Lote
* Cada item do lote adicionado à fila recebe um `BatchId` (Guid único) e o título da playlist.
* A aba **Fila de Downloads** exibe os itens normalmente, mas adiciona no topo um **Banner de Progresso Agregado** caso haja itens de lote ativos:
  ```text
  [Playlist: Curso de .NET 10] Concluídos: 5/12 | Baixando: 2 | Aguardando: 5 | Falhas: 0
  [ Barra de Progresso Agregada: 41% ]    [ Botão: Cancelar Lote ]
  ```

### 7.3. Política de Cancelamento e Limpeza
* **Cancelamento Individual:** Clicar em "Cancelar" em um item específico interrompe apenas o processo daquele vídeo, remove os arquivos temporários parciais (`.part`, `.ytdl` no diretório `BaixALL_Jobs/{jobId}`) e libera imediatamente o slot para o próximo item da fila. O restante do lote continua rodando normalmente.
* **Cancelamento do Lote Inteiro:** Clicar em "Cancelar Lote" cancela todos os downloads ativos pertencentes àquele `BatchId`, descarta os itens que ainda estavam aguardando na fila e limpa todas as pastas temporárias associadas. Vídeos avulsos ou de outros lotes não são afetados.

### 7.4. Honestidade no Progresso Agregado
* O progresso agregado do lote não simulará porcentagens com base em tamanhos de arquivo desconhecidos.
* A porcentagem agregada do lote será calculada estritamente com base nos **itens finalizados** em relação ao **total de itens do lote**:
  $$\text{Progresso do Lote} = \frac{\text{Itens Concluídos}}{\text{Total de Itens Selecionados}} \times 100\%$$
* Para o item que está baixando no momento, a barra individual do card exibe a taxa e porcentagem exata de bytes transferidos via `yt-dlp`.

---

## 8. Segurança, Duplicados e Persistência

### 8.1. Tratamento de URLs Híbridas (`watch?v=...&list=...`)
* Quando o usuário copia o link de um vídeo assistido dentro de uma playlist no navegador, a URL contém tanto o ID do vídeo (`v=...`) quanto o ID da playlist (`list=...`).
* **Comportamento Proposto:** O BaixALL identifica a presença dos dois parâmetros e exibe no banner duas ações claras:
  * **[Baixar apenas este vídeo]** (analisa como vídeo avulso, descartando a playlist).
  * **[Analisar playlist completa]** (carrega todos os vídeos da playlist).

### 8.2. Vídeos Removidos, Privados ou Bloqueados
* Playlists frequentemente contêm vídeos deletados ("Vídeo indisponível") ou restritos por região.
* **Comportamento:**
  * Na análise rápida, itens indisponíveis que o yt-dlp não consegue ler metadados são desmarcados ou sinalizados como `Indisponível`.
  * Se um vídeo falhar durante o download na fila, o `DownloadService` captura o erro, marca o card como `Erro` com mensagem amigável, grava o status e prossegue imediatamente para o próximo item da fila.

### 8.3. Política de Deduplicação e Nomes de Arquivos
* Não haverá sobrescrita silenciosa nem exclusão de arquivos existentes.
* Reutilização estrita de `FileHelper.SanitizeFileName` para remover caracteres proibidos no Windows (`\ / : * ? " < > |`).
* Reutilização estrita de `FileHelper.GetUniqueFilePath`, que gera automaticamente sufixos numéricos (`Video (1).mp4`, `Video (2).mp4`) caso o arquivo já exista na pasta de destino.
* Tratamento de caminhos excessivamente longos no Windows (limite MAX_PATH de 260 caracteres truncando o nome com segurança sem perder a extensão).

### 8.4. Persistência e Fechamento do Aplicativo
* O `history.json` registra cada vídeo concluído do lote individualmente.
* Se o usuário fechar o aplicativo enquanto houver downloads de um lote em andamento:
  * O método `OnExit` do `App.xaml.cs` encerra com segurança os processos filhos via `ProcessRunner.Kill` e limpa os arquivos parciais temporários em `%TEMP%\BaixALL_Jobs`.
  * Os arquivos já concluídos permanecem salvos na pasta de destino e registrados no histórico.
  * **Não será implementada retomada automática arriscada de estado parcial**: na v1.2.0, o foco é a estabilidade; tentar restaurar filas interrompidas após fechar o aplicativo sem garantias pode gerar duplicação de processos, vazamento de semáforos ou arquivos corrompidos. O usuário pode simplesmente reenfileirar os itens pendentes com 1 clique se desejar.

---

## 9. Desempenho e Limites

1. **Virtualização de UI:** A listagem de vídeos no card de playlist utilizará virtualização WPF (`VirtualizingStackPanel`). Apenas os itens visíveis no visor da tela são renderizados na árvore visual do WPF, mantendo o consumo de memória baixo e rolagem fluida mesmo para 200 vídeos.
2. **Carregamento Otimizado de Miniaturas:** As imagens das miniaturas serão carregadas de forma assíncrona (`BitmapImage` com `DecodePixelWidth` limitado a 120px e `CreateOptions = DelayCreation`), evitando alocação excessiva de memória bitmap na UI thread.
3. **Limite Preventivo de Lote:** Para evitar bloqueio de IP pelo YouTube ou sobrecarga extrema de disco, recomenda-se um aviso amigável caso uma playlist contenha mais de 250 vídeos, orientando o usuário a baixar em partes caso necessário.

---

## 10. Matriz de Testes (Automatizados e Manuais)

### 10.1. Testes Automatizados Unitários (Determinísticos com Fixtures Locais)
* Sem chamadas de rede externas no CI (fixtures em `tests/BaixALL.Tests/TestData/`).
1. **`UrlValidatorPlaylistTests`:**
   * URL de playlist pura (`https://www.youtube.com/playlist?list=PL...`).
   * URL híbrida (`https://www.youtube.com/watch?v=...&list=PL...`).
   * URL com parâmetros extras (`&index=3`, `&t=40s`).
   * URLs inválidas ou com caracteres malformados.
2. **`PlaylistParsingTests`:**
   * Parse de JSON de `--flat-playlist` simulando playlist normal de 10 vídeos.
   * Parse com vídeos removidos / durações nulas (transmissões ao vivo).
   * Parse de títulos com emojis, aspas e caracteres especiais.
   * Parse de playlist vazia (0 entradas).
3. **`BatchFormatSelectionTests`:**
   * Geração correta dos seletores de qualidade para lote (`bestvideo[height<=1080]+bestaudio/best`).
   * Modo somente áudio em lote (MP3, M4A, Opus).
4. **`BatchQueueConcurrencyTests`:**
   * Enfileiramento de lote simulado de 10 itens com concorrência = 2.
   * Verificação de que nunca há mais de 2 downloads ativos simultaneamente.
   * Cancelamento individual de 1 item sem afetar os outros 9.
   * Cancelamento de lote cancelando todos os itens associados ao `BatchId`.
   * Continuação automática da fila após falha simulada de um item.

### 10.2. Testes de Integração Reais (Opcionais / Locais)
* Marcados com `[Trait("Category", "Integration")]` para não depender de rede externa no CI.
* Validação real de extração leve de playlist pública do YouTube via `yt-dlp.exe`.

### 10.3. Checklist de Validação Manual (Windows 11)
* [ ] Análise de playlist pública de 5 a 15 vídeos.
* [ ] Marcar/desmarcar todos com 1 clique e seleção manual de 3 vídeos específicos.
* [ ] Download do lote selecionado em vídeo até 1080p (verificar mesclagem via FFmpeg).
* [ ] Download do lote em Somente Áudio (MP3).
* [ ] Cancelamento de um item no meio do lote e conferência da limpeza de arquivos temporários.
* [ ] Cancelamento do lote inteiro.
* [ ] Teste com URL híbrida (`watch?v=...&list=...`) escolhendo vídeo único vs. playlist inteira.
* [ ] Conferência de que os downloads individuais da v1.1.0 continuam 100% inalterados.
* [ ] Verificação nos temas Dark e Light.

---

## 11. Fases de Implementação Propostas

O desenvolvimento será executado de maneira estritamente modular em 6 fases:

* **Fase A:** Modelos, Validação de URLs e Extração de Metadados da Playlist.
* **Fase B:** Interface de Análise e Seleção de Playlist na Home (WPF).
* **Fase C:** Integração com a Fila Existente e Concorrência Global.
* **Fase D:** Cancelamento de Lote, Progresso Agregado e Histórico.
* **Fase E:** Testes de Regressão, Integração e Refinamento de Interface.
* **Fase F:** Preparação da Versão 1.2.0-rc.1 para Validação na VM.

---

## 12. Estimativa de Complexidade e Riscos Técnicos

| Área | Complexidade | Risco Principal | Mitigação Arquitetural |
| :--- | :---: | :--- | :--- |
| **Extração de Metadados** | Baixa / Média | Lentidão ou bloqueio por excesso de requisições. | Uso estrito de `--flat-playlist -J`, garantindo retorno em 1 chamada rápida sem extrair streams pesados na análise. |
| **Interface WPF (Home)** | Média | Travamento da UI com playlists grandes (100+ itens). | Virtualização nativa da lista (`VirtualizingStackPanel`) e carregamento sob demanda de miniaturas. |
| **Fila e Concorrência** | Média | Concorrência paralela descontrolada violando limite global. | Reutilização do mesmo semáforo e fila do `DownloadService`, enfileirando cada vídeo como job individual com `BatchId`. |
| **Seleção de Qualidade** | Baixa / Média | Vídeos da playlist que não oferecem a qualidade escolhida. | Política de fallback transparente (`bestvideo[height<=X]+bestaudio/best`) informada claramente ao usuário. |
| **Persistência / Fechamento** | Baixa | Arquivos corrompidos ou orfãos se fechar durante o download. | Cancelamento limpo no `OnExit`, exclusão de arquivos temporários e registro individual no histórico. |

---

## 13. Decisões que Precisam da sua Aprovação

Antes de iniciar a **Fase A**, solicito sua decisão sobre os seguintes pontos de produto e engenharia:

1. **Tratamento de URLs Híbridas (`watch?v=...&list=...`):**
   * *Opção Recomendada:* Ao colar uma URL que contém tanto vídeo quanto playlist, o BaixALL exibe um diálogo/banner na tela: *"Deseja baixar apenas este vídeo ou analisar a playlist completa?"*.
2. **Localização da Interface de Playlist:**
   * *Opção Recomendada:* Integrar diretamente na aba **Início (Downloader)**. Quando uma playlist for detectada, o card de vídeo único é substituído pelo card de playlist com um botão *"← Limpar / Outra URL"*, sem poluir a barra de navegação com uma nova aba principal.
3. **Política de Qualidade do Lote:**
   * *Opção Recomendada:* Definir uma qualidade padrão máxima de referência (ex.: *"Até 1080p"* ou *"Melhor qualidade"*). Se um vídeo não possuir 1080p, ele baixa automaticamente a melhor resolução disponível abaixo disso, garantindo que o lote seja concluído sem erros artificiais.
4. **Comportamento ao Fechar o Aplicativo:**
   * *Opção Recomendada:* Cancelamento seguro de processos ativos, descarte de arquivos parciais temporários e preservação dos itens já concluídos no disco e no histórico, sem tentar restauração automática parcial arriscada ao reabrir.
