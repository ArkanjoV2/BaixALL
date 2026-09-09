# Planejamento Arquitetural — BaixALL 1.3.0

## 🎯 Visão Geral e Objetivos da Versão 1.3.0

O objetivo primordial do **BaixALL 1.3.0** é expandir a atuação do aplicativo, transformando-o de um downloader exclusivo do YouTube em um **downloader multiplataforma unificado**, adicionando suporte nativo a vídeos do **Instagram** e do **X (antigo Twitter)**.

Em alinhamento com a diretriz do projeto:
- A persistência de fila pós-reinicialização, recuperação automática de lotes e pausa/retomada deixam de ser o foco desta versão e poderão ser estudadas para versões posteriores (linha 1.4+).
- O motor de download continua estritamente **único**: uma única fila global, um único semáforo de concorrência (1 a 3 downloads simultâneos) e o mesmo gerenciamento centralizado de ferramentas (`yt-dlp`, `FFmpeg`, `ffprobe` e `Deno`).
- Não serão utilizados serviços externos intermediários, scraping não oficial, APIs pagas ou bibliotecas de terceiros adicionais. Toda a extração se apoia exclusivamente nas capacidades dos extratores oficiais do executável `yt-dlp` mantido pelo aplicativo.

---

## 🏗️ 1. Auditoria da Arquitetura Atual (v1.2.0)

A inspeção detalhada do código-fonte do BaixALL 1.2.0 identificou as seguintes fronteiras entre componentes acoplados e componentes reutilizáveis:

### 1.1. Componentes Acoplados ao YouTube (Necessitam de Adaptação)

1. **`UrlValidator.cs` (`src/BaixALL.App/Helpers/UrlValidator.cs`):**
   - Utiliza exclusivamente expressões regulares de YouTube (`YouTubeVideoRegex`, `YouTubePlaylistRegex`, `YouTubeDomainRegex`).
   - Métodos públicos como `IsValidYouTubeUrl`, `ExtractVideoId`, `NormalizeYouTubeUrl` pressupõem a semântica do YouTube.
2. **`IYoutubeService` / `YoutubeService.cs` (`src/BaixALL.App/Services/`):**
   - Interface e implementação explicitamente nomeadas para YouTube.
   - O método `AnalyzeVideoAsync` invoca diretamente `UrlValidator.IsValidYouTubeUrl` e retorna erro específico caso a URL não seja do YouTube.
3. **`FormatSelectionService.cs` (`src/BaixALL.App/Services/FormatSelectionService.cs`):**
   - **Formato sem codec explícito:** O método `ParseVideoInfo` filtra formatos de vídeo via `f.HasVideo`, que exigia `!string.IsNullOrEmpty(VCodec) && VCodec != "none"`. No entanto, em extrações do Twitter/X, os fluxos progressivos HTTP (`http-320`, `http-832`, `http-2176`) retornam com `vcodec` nulo no JSON do yt-dlp, fazendo com que fossem descartados erroneamente.
   - **Geração de URLs de itens de coleção:** Em `ParsePlaylistInfo`, as URLs dos itens filhos sem `entryUrl` explícito utilizam fallback hardcoded: `$"https://www.youtube.com/watch?v={entryId}"` e miniaturas em `https://i.ytimg.com/vi/...`.
4. **`MainViewModel.cs` (`src/BaixALL.App/ViewModels/MainViewModel.cs`):**
   - Invocação de `UrlValidator.IsValidYouTubeUrl(UrlInput)` no comando `AnalyzeAsync` e na rotina `TryAutoPasteFromClipboard`.
   - Mensagens de erro de validação fixas para YouTube ("Por favor, cole a URL de um vídeo ou playlist do YouTube").
5. **Deduplicação de Fila (`GetCanonicalVideoKey`):**
   - Extrai apenas ID do YouTube, com fallback para a URL pura. Sem namespace de plataforma, há risco teórico de colisão entre IDs numéricos do Twitter/X e IDs do YouTube ou do Instagram.

### 1.2. Componentes 100% Reutilizáveis (Sem Alteração Estrutural)

1. **Motor de Download (`YtDlpService.cs`):**
   - O pipeline `DownloadAsync` é totalmente agnóstico de plataforma. Ele apenas recebe o executável do yt-dlp, os argumentos CLI (`-f`, `-o`, `--newline`, etc.), o ambiente com FFmpeg/Deno e executa no diretório temporário isolado (`jobTempDir`).
   - O template customizado de progresso (`ProgressTemplate = "baixall_prog:[...]"`) funciona para qualquer extrator do yt-dlp.
   - O cancelamento gracioso via `CancellationToken` e a rotina de exclusão de arquivos temporários (`.part`, `.ytdl`, `.tmp`) permanecem 100% funcionais.
2. **Fila e Concorrência Global (`DownloadService.cs`):**
   - O semáforo global `SemaphoreSlim` (1 a 3 slots de concorrência) controla a execução de qualquer item enfileirado, sem distinção de origem.
   - Itens de lote com `BatchId` compartilham o mesmo progresso agregado e contadores, seja um lote de playlist do YouTube ou um post com múltiplos vídeos do Instagram/X.
3. **Pós-processamento com FFmpeg:**
   - Mesclagem automática em containers MP4/MKV e conversão para MP3/M4A/Opus funcionam de forma idêntica para qualquer fluxo baixado.
4. **Histórico Local (`HistoryService.cs`):**
   - Estrutura JSON local totalmente reaproveitável, necessitando apenas da inclusão do campo identificador da plataforma.
5. **Gerenciador de Ferramentas (`DependencyManager.cs`):**
   - O yt-dlp é a ferramenta central responsável por todas as plataformas suportadas.

---

## 🔬 2. Investigação Técnica do yt-dlp (Versão 2026.07.04)

Os testes de viabilidade foram executados com a versão oficial do **yt-dlp 2026.07.04** instalada e disponível no sistema.

### 2.1. Mapeamento de Extratores Nativos

A inspeção do catálogo interno de extratores do yt-dlp revelou:
- **Instagram:**
  - `Instagram`: Extrator principal para publicações avulsas (`/p/`), Reels (`/reel/`) e IGTV (`/tv/`).
  - `InstagramIOS`: Extrator alternativo simulando cliente móvel iOS.
  - `instagram:story`: Extrator específico para Stories (exige autenticação obrigatória).
  - `instagram:user`: **CURRENTLY BROKEN** (marcado oficialmente como quebrado pela equipe do yt-dlp devido a bloqueios severos da Meta).
- **X / Twitter:**
  - `twitter`: Extrator principal para status e posts (`x.com` e `twitter.com`).
  - `twitter:card`: Processamento de cards embutidos.
  - `twitter:spaces`: Áudios de transmissões ao vivo.
  - `twitter:broadcast`: Transmissões ao vivo de vídeo.

### 2.2. Matriz de Compatibilidade por Plataforma

A tabela a seguir consolida o escopo formal da versão 1.3.0, distinguindo o que é suportado com segurança, o que é tratado como erro amigável e o que foi conscientemente postergado ou excluído por limitações de autenticação/segurança:

| Plataforma | Tipo de Conteúdo | Status na v1.3.0 | Extrator yt-dlp | Requer Login? | Observações Técnicas |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **YouTube** | Vídeo Individual | **Suportado (Estável)** | `youtube` | Não | Mantido integralmente da v1.0.0/v1.1.0/v1.2.0 |
| **YouTube** | Playlist Completa | **Suportado (Estável)** | `youtube:tab` | Não | Mantido da v1.2.0 com seleção de itens em lote |
| **YouTube** | URL Híbrida (`watch?v=...&list=...`) | **Suportado (Estável)** | `youtube` | Não | Detecção inteligente e opção de carregar playlist |
| **Instagram** | Reel Público (`/reel/<id>`) | **Suportado (Novo)** | `Instagram` | Não | Extração direta, formatos MP4 verticais até 720p/1080p |
| **Instagram** | Post com Vídeo (`/p/<id>`) | **Suportado (Novo)** | `Instagram` | Não | Processado pelo mesmo extrator unificado |
| **Instagram** | Carrossel c/ Vídeos (`/p/<id>`) | **Suportado (Novo)** | `Instagram` | Não | Detectado como coleção (`_type: playlist`), lote na UI |
| **Instagram** | Fotos em Carrossel | *Postergado (v1.4+)* | — | Não | Foco da v1.3.0 é vídeo. Pipeline de imagens requer novos handlers |
| **Instagram** | Stories e Destaques | *Fora de Escopo* | `instagram:story` | **Sim (Obrigatório)** | Exige cookies/sessão de usuário. Rejeitado por segurança |
| **Instagram** | Perfis Inteiros (`/<user>`) | *Fora de Escopo* | `instagram:user` | Sim | Extrator marcado como quebrado no upstream do yt-dlp |
| **X / Twitter** | Post c/ Vídeo (`x.com` / `twitter.com`) | **Suportado (Novo)** | `twitter` | Não | Formatos HTTP progressivos e HLS até 720p/1080p |
| **X / Twitter** | GIFs Animados | **Suportado (Novo)** | `twitter` | Não | Na infraestrutura do X são MP4 em loop |
| **X / Twitter** | Post sem Vídeo (Texto / Fotos) | *Tratamento Limpo* | `twitter` | Não | Detectado sem crash; exibe aviso informativo |
| **X / Twitter** | URLs com Parâmetros (`?s=20`, etc.) | **Suportado (Novo)** | `twitter` | Não | Higienização de parâmetros de compartilhamento |
| **X / Twitter** | Spaces / Transmissões de Áudio | *Fora de Escopo* | `twitter:spaces` | Não | Duração indeterminada e tokens altamente voláteis |
| **X / Twitter** | Perfis / Linhas do Tempo | *Fora de Escopo* | — | Sim | Rate limits severos e bloqueio em tokens de convidado |

---

## 📸 3. Instagram: Análise de Viabilidade e Escopo

### 3.1. Recursos de Alta Viabilidade (Suportados na v1.3.0)
- **Reels Públicos (`https://www.instagram.com/reel/<shortcode>/`):**
  - **Teste Real:** `https://www.instagram.com/reel/Chunk8-jurw/` (Reel oficial).
  - **Resultado:** Extração imediata sem necessidade de cookies para links públicos. Retornou JSON de 31,8 KB, 9 formatos DASH/MP4 disponíveis (resolução vertical até 720x1280 30 FPS). Download real concluído com sucesso gerando arquivo MP4 de 1.949.801 bytes.
- **Vídeos em Posts Comuns (`https://www.instagram.com/p/<shortcode>/`):**
  - Mapeado para o mesmo extrator do Reel.
- **Carrosséis de Múltiplos Vídeos (`_type: playlist`):**
  - **Teste Real:** `https://www.instagram.com/p/BQ0eAlwhDrw/` (Post carrossel com 3 vídeos).
  - **Resultado:** O yt-dlp identifica o carrossel e retorna um objeto estruturado de playlist contendo as 3 entradas individuais (`BQ0dSaohpPW`, `BQ0dTpOhuHT`, `BQ0dT7RBFeF`), cada uma com miniatura e formatos próprios. Cada vídeo pode ser baixado individualmente através de sua URL direta canônica.
- **Links com Parâmetros de Compartilhamento:**
  - URLs contendo `?igsh=...`, `?utm_source=...` são higienizadas pela camada de normalização sem perda do shortcode identificador.

### 3.2. Limitações Críticas da Plataforma e Segurança
- **Bloqueio de Guest IP e Exigência de Login:**
  - A Meta aplica políticas dinâmicas e agressivas de limitação por IP para requisições anônimas. Quando a Meta restringe o IP ou quando a publicação exige login (ex: idade, restrição regional ou conta privada), o yt-dlp retorna:
    `WARNING: [Instagram] ... Instagram API is not granting access`
    `ERROR: [Instagram] ... Instagram sent an empty media response. Check if this post is accessible in your browser without being logged-in.`
  - **Diretriz de Segurança:** O BaixALL não solicitará, não capturará e não armazenará senhas nem cookies do Instagram. Ao detectar esse erro, o aplicativo exibirá uma notificação honesta e transparente: *"O Instagram restringiu o acesso público a esta publicação (exige login na plataforma ou bloqueio preventivo da Meta). O BaixALL opera apenas com mídias públicas e não armazena credenciais do usuário."*
- **Stories e Destaques:**
  - Exigem autenticação de usuário 100% do tempo. **Totalmente fora de escopo para a v1.3.0.**
- **Imagens em Carrosséis:**
  - O pipeline do BaixALL é estritamente de áudio e vídeo com FFmpeg. O yt-dlp descarta imagens estáticas de carrosséis a menos que sejam extraídas como miniaturas. Fotos de carrosséis ficam **postergadas para futuras versões (v1.4+)**.

---

## 🐦 4. X / Twitter: Análise de Viabilidade e Escopo

### 4.1. Recursos de Alta Viabilidade (Suportados na v1.3.0)
- **Vídeos de Posts Públicos:**
  - **Teste Real:** `https://x.com/captainamerica/status/719944021058060289`.
  - **Resultado:** Extração bem-sucedida de metadados em JSON (7,6 KB). 4 formatos identificados (`http-320` 180p, `http-832` 360p, `http-2176` 720p e `hls-2176` 720p). Download real executado com sucesso gerando arquivo MP4 íntegro com áudio e vídeo de 539.284 bytes.
- **Interoperabilidade de Domínios (`x.com` e `twitter.com`):**
  - Ambos os domínios são processados de forma equivalente pelo extrator nativo.
- **URLs com Parâmetros de Compartilhamento:**
  - URLs com `?s=20`, `?t=...` validadas e higienizadas com sucesso.
- **Posts sem Vídeo (Detecção Limpa):**
  - **Teste Real:** `https://x.com/NASA/status/1834289891461464455` (post textual/imagem).
  - **Resultado:** Retorna erro explícito: `ERROR: [twitter] ... No video could be found in this tweet`. Mapeado para aviso amigável na interface.
- **GIFs Animados:**
  - Na infraestrutura do X, os GIFs são armazenados e transmitidos como vídeos MP4 em loop. O download ocorre normalmente como arquivo MP4.

### 4.2. Recursos Fora do Escopo Inicial
- **Spaces e Transmissões ao Vivo (Broadcasts):**
  - Áudios de Spaces e transmissões contínuas possuem tokens altamente voláteis e duração indeterminada, incompatíveis com a fila estática. Fora de escopo.
- **Perfis Inteiros / Linhas do Tempo:**
  - O endpoint de linha do tempo do X aplica bloqueios imediatos de rate limit em tokens de convidado (*guest tokens*). Fora de escopo.

---

## 🏛️ 5. Arquitetura Multiplataforma Proposta

Para evitar a proliferação de `if (url.Contains("youtube"))` espalhados pelo código, propõe-se uma arquitetura baseada em **provedores e adaptadores desacoplados**:

### 5.1. Enum `PlatformType`
```csharp
public enum PlatformType
{
    Unknown = 0,
    YouTube = 1,
    Instagram = 2,
    Twitter = 3 // Exibido como "X / Twitter"
}
```

### 5.2. Serviço de Plataforma (`IPlatformService`)
Substitui o papel estático do `UrlValidator` para detecção de entrada:
- `PlatformType DetectPlatform(string? url)`: Identifica a plataforma instantaneamente via regex de domínio.
- `bool IsSupportedUrl(string? url)`: Confirma se a URL pertence a um padrão suportado.
- `string NormalizeUrl(string url, PlatformType platform)`: Remove parâmetros supérfluos e unifica formatos canônicos.
- `string GetCanonicalKey(string url, PlatformType platform, string? mediaId = null)`:
  - YouTube: `"yt:" + videoId`
  - Instagram: `"ig:" + shortcode`
  - Twitter/X: `"x:" + tweetId`
  *Garante deduplicação 100% imune a colisões na fila de download.*

### 5.3. Serviço Unificado de Análise (`IMediaAnalysisService`)
Evolução do antigo `IYoutubeService`:
```csharp
public interface IMediaAnalysisService
{
    Task<MediaAnalysisResult> AnalyzeAsync(string url, CancellationToken ct = default);
}

public class MediaAnalysisResult
{
    public PlatformType Platform { get; init; }
    public bool IsCollection { get; init; } // Playlist ou Carrossel
    public bool IsHybrid { get; init; }     // YouTube Watch + List
    public VideoInfo? Video { get; init; }
    public PlaylistInfo? Collection { get; init; }
}
```

### 5.4. Ajustes no `FormatSelectionService`
- **Tolerância a `vcodec` nulo:** Identifica formatos de vídeo válidos se `Height > 0` ou `Width > 0` ou `VCodec != "none"`, suportando os streams progressivos do Twitter/X.
- **Geração honesta de opções de resolução:** Apenas resoluções com altura comprovadamente presente no JSON são exibidas na lista de qualidades.
- **Geração de URLs filhas em coleções:** Constrói URLs conforme a plataforma de origem do carrossel/playlist.

---

## 🎨 6. Proposta de Interface e Experiência do Usuário (UX)

A interface preservará 100% da identidade visual da versão 1.2.0 (tema escuro Fluent, tipografia Segoe UI Variable, paleta com azul tech `#0098FF` e botões de ação consistentes).

### 6.1. Fluxo de Entrada Unificado
O usuário simplesmente cola o link na caixa de entrada da tela inicial. O aplicativo detecta a plataforma em segundo plano sem exigir que o usuário selecione um botão de rádio ou menu suspenso.

### 6.2. Badges de Identificação da Plataforma
Pills discretos e refinados serão exibidos:
- **YouTube:** Badge com acento vermelho suave (`#FF0000` / fundo `#2A1515`).
- **Instagram:** Badge com acento degradê/magenta suave (`#E1306C` / fundo `#2A1522`).
- **X / Twitter:** Badge com acento azul Twitter / monocromático (`#1DA1F2` / fundo `#0F2232`).

Estes badges estarão presentes em:
1. **Card de análise da Home** (ao lado do título e da resolução máxima).
2. **Card de download na Fila** (ao lado da tag de lote ou qualidade).
3. **Registro no Histórico** (coluna de mídia com identificação clara da origem).

### 6.3. Carrosséis de Múltiplos Vídeos
Quando um post do Instagram ou do X contiver múltiplos vídeos, a tela reutilizará o painel de seleção em lote introduzido na v1.2.0:
- Título: *"Carrossel do Instagram • X vídeos encontrados"* ou *"Publicação do X • X vídeos encontrados"*.
- Controles em massa: *Selecionar Todos*, *Desmarcar Todos*, *Inverter Seleção*.
- Opções de lote: Qualidade padrão, container, opção Somente Áudio e criação de subpasta automática.

---

## 🛡️ 7. Tratamento de Erros e Diretrizes de Segurança

Mapeamento amigável das saídas de erro do motor:

| Erro Retornado pelo Motor | Mensagem Apresentada ao Usuário |
| :--- | :--- |
| `No video could be found in this tweet` | Esta publicação do X/Twitter não contém nenhum vídeo. |
| `Instagram API is not granting access` / `empty media response` | O Instagram restringiu o acesso público a esta publicação (exige login ou conteúdo privado). O BaixALL não armazena credenciais e opera somente com conteúdos públicos. |
| `This content is unreachable. Use --cookies-from-browser` | O download de Stories do Instagram exige login com conta de usuário, o que não é suportado pelo BaixALL por motivos de segurança. |
| `HTTP Error 429: Too Many Requests` | A plataforma atingiu temporariamente o limite de requisições para o seu endereço IP. Aguarde alguns minutos antes de tentar novamente. |
| `This tweet has been deleted` / `Tweet not found` | Esta publicação do X/Twitter foi excluída ou não existe mais. |
| `Post has been removed` / `Video removed` | A publicação foi removida pelo autor ou pela plataforma. |
| `Private video` / `Account is private` | Esta publicação é privada e não pode ser acessada sem autorização do autor. |
| `is not a valid URL` / `unsupported URL` | A URL informada não pertence a uma plataforma suportada (YouTube, Instagram ou X/Twitter) ou possui formato inválido. |

---

## 📅 8. Fases de Implementação Propostas

- **Fase A: Auditoria e Viabilidade dos Extratores** *(Concluída com testes reais nesta etapa)*.
- **Fase B: Camada de Plataformas e Modelagem Agnóstica**
  - Criação do enum `PlatformType` e serviço `PlatformService`.
  - Normalização de URLs e deduplicação canônica com prefixo de namespace.
  - Testes unitários com suíte de URLs válidas e inválidas de YouTube, Instagram e X.
- **Fase C: Análise e Download Individual do X / Twitter**
  - Implementação do suporte a posts com vídeo no `MediaAnalysisService`.
  - Adaptação do `FormatSelectionService` para aceitar formatos progressivos HTTP.
  - Testes unitários com fixtures JSON locais gravadas de tweets com vídeo.
- **Fase D: Análise e Download Individual do Instagram**
  - Implementação do suporte a Reels e posts em `/p/`.
  - Tratamento de mensagens amigáveis para restrições da Meta.
  - Testes unitários com fixtures JSON locais gravadas de Reels públicos.
- **Fase E: Carrosséis e Publicações com Múltiplas Mídias**
  - Tratamento de `_type: playlist` em publicações do Instagram/X.
  - Reutilização transparente da interface de seleção em lote e enfileiramento universal.
  - Testes unitários com fixtures locais de carrosséis de 3 vídeos.
- **Fase F: Interface, Badges Visuais, Histórico e Regressões**
  - Adição de badges discretos de plataforma na Home, Fila e Histórico.
  - Validação estrita de não regressão do YouTube (garantindo 170+ testes unitários aprovados).
- **Fase G: Homologação Real, Testes em VM e Release Candidate 1.3.0-rc.1**
  - Testes do instalador e pacote portátil em ambiente limpo de máquina virtual.
  - Downloads reais de demonstração das 3 plataformas na mesma fila.

---

## ✅ 9. Critérios de Aceitação da Versão 1.3.0

1. **Interoperabilidade:** Capacidade de colar e baixar com sucesso vídeos públicos de YouTube, Instagram (Reels/Posts) e X/Twitter a partir da mesma tela inicial sem intervenção manual de configuração de plataforma.
2. **Motor e Concorrência Únicos:** Downloads de diferentes plataformas coexistindo na mesma fila e respeitando estritamente o limite de 1, 2 ou 3 downloads simultâneos.
3. **Transparência de Erros:** Mensagens amigáveis e explicativas para posts sem vídeo, conteúdos privados ou exigência de login, sem travamento da aplicação.
4. **Segurança Absoluta:** Nenhuma captura de senha, scraping de cookies de navegadores ou violação de controles de acesso.
5. **Estabilidade e Não Regressão:** Todos os 170 testes automatizados existentes mantidos verdes, acompanhados de novos testes unitários com fixtures estáticas sem chamadas de rede no CI.
6. **Preservação de Versões Anteriores:** As versões v1.0.0, v1.1.0 e v1.2.0 permanecem 100% inalteradas no GitHub.
