# Planejamento Arquitetural — BaixALL 1.1.0
## Suporte a Playlists e Downloads em Lote

Este documento descreve a proposta técnica, arquitetura, interface, riscos e etapas de implementação para a versão **1.1.0** do BaixALL.

---

## 1. Objetivo e Diretrizes

O objetivo da versão 1.1.0 é permitir que o usuário insira links de **playlists do YouTube**, visualize os vídeos pertencentes à lista, selecione quais deseja baixar e enfileire o lote com uma única configuração de qualidade e formato.

### Diretrizes Fundamentais
- **Preservação do Motor:** Não reescrever o motor de download.
- **Reaproveitamento Máximo:** Utilizar as classes existentes (`YtDlpService`, `DownloadService`, `HistoryService`, `DependencyManager`).
- **Respeito à Concorrência:** O lote de vídeos deve entrar na fila existente, respeitando a configuração de 1, 2 ou 3 downloads simultâneos.
- **Isolamento de Falhas:** A falha no download de um vídeo da playlist não deve interromper o download dos demais.

---

## 2. Análise de Reaproveitamento da Arquitetura Existente

| Componente | Estado Atual (v1.0.0) | Adaptação para Playlists (v1.1.0) |
| :--- | :--- | :--- |
| **`YtDlpService`** | Executa `-J --no-playlist` para metadados de 1 vídeo. | Adicionar método `GetPlaylistMetadataJsonAsync` com `--flat-playlist -J --skip-download`. Obtém a lista completa de itens (ID, título, canal, duração) em uma única chamada leve. |
| **`DownloadService`** | Gerencia slots de concorrência (1 a 3), fila reativa e cancelamento por item. | **Reaproveitamento de 100% da lógica.** Cada vídeo selecionado é transformado em um `DownloadRequest` individual e enfileirado via `EnqueueDownload`. A fila já cuida da execução sequencial e dos slots. |
| **Fila de Downloads** | Exibe itens ativos com barra de progresso, velocidade e botão cancelar. | Reutiliza a mesma UI (`QueueView.xaml`) e `DownloadItemViewModel`. O usuário acompanha cada vídeo do lote com seu progresso individual. |
| **Seleção de Qualidade** | Usuário escolhe formato de um vídeo específico. | Na playlist, a escolha de qualidade aplica uma regra global (ex.: *"Melhor qualidade disponível"*, *"1080p se disponível"*, *"Áudio MP3"*) para todos os vídeos marcados do lote. |
| **Cancelamento Individual** | Cada download possui seu próprio `CancellationTokenSource` e pasta temporária isolada (`BaixALL_Jobs/{guid}`). | Mantido integralmente. O usuário pode cancelar um vídeo específico da playlist sem afetar os outros. |
| **Histórico** | Grava em `history.json` após o término de cada download. | Mantido integralmente. Cada item concluído da playlist gera seu registro no histórico com opções para abrir o arquivo ou pasta. |
| **Gerenciador de Ferramentas** | Gerencia `yt-dlp`, `FFmpeg`, `ffprobe` e `Deno`. | **Inalterado.** Nenhuma dependência externa adicional é necessária. |

---

## 3. Escopo Proposto

### Incluído no Escopo da v1.1.0
1. **Detecção Inteligente de URLs:**
   - Suporte a URLs puras de playlist (`https://www.youtube.com/playlist?list=PL...`).
   - Suporte a URLs compostas de vídeo dentro de playlist (`https://www.youtube.com/watch?v=...&list=PL...`). O usuário poderá escolher entre *"Baixar apenas este vídeo"* ou *"Carregar a playlist inteira"*.
2. **Carregamento Otimizado de Metadados:**
   - Leitura rápida dos itens da playlist sem extrair os fluxos de mídia detalhados antecipadamente.
3. **Interface de Seleção da Playlist:**
   - Exibição de título da playlist, canal e quantidade total de itens.
   - Lista com caixas de seleção (*checkboxes*) para cada item.
   - Ações rápidas: *"Selecionar Todos"*, *"Desmarcar Todos"*, contador visual (*"X de Y selecionados"*).
4. **Enfileiramento em Lote:**
   - Botão *"Baixar Selecionados"*, que injeta todos os itens marcados na fila de downloads com o formato/qualidade configurado.

### Fora do Escopo (Não Recomendado)
- Download de canais inteiros (alto risco de bloqueio de IP/rate limiting pelo YouTube).
- Edição individual de formatos diferentes para cada vídeo da mesma lista.
- Agendamento de downloads com temporizador.

---

## 4. Análise de Riscos e Mitigações

1. **Risco: Bloqueio de IP por excesso de requisições (*Rate Limiting* / HTTP 429)**
   - *Causa:* Playlists muito longas (100+ vídeos) podem disparar requisições em excesso se cada item for analisado simultaneamente.
   - *Mitigação:* O uso de `--flat-playlist` realiza uma única consulta JSON para listar todos os vídeos. Durante o download, o `DownloadService` já limita a concorrência a 1, 2 ou 3 conexões ativas.
2. **Risco: Vídeos indisponíveis ou privados dentro da playlist**
   - *Causa:* Vídeos excluídos, com restrição de idade severa ou privados na playlist.
   - *Mitigação:* O `DownloadService` já isola erros por item; se um vídeo falhar, seu card exibe a mensagem de erro amigável e a fila prossegue automaticamente para o próximo item.
3. **Risco: Degradação de desempenho na interface WPF com listas grandes**
   - *Causa:* Renderizar dezenas de elementos visuais na tela.
   - *Mitigação:* Utilização de virtualização de interface (`VirtualizingStackPanel.IsVirtualizing="True"`) na listagem da playlist.

---

## 5. Estratégia de Testes Automatizados

1. **Testes Unitários:**
   - `UrlValidatorTests`: Validar regex de playlists (`playlist?list=`, `watch?v=...&list=`).
   - `PlaylistParsingTests`: Validar parsing do JSON retornado pelo `--flat-playlist`, incluindo títulos com caracteres especiais, durações nulas (transmissões ao vivo na playlist) e vídeos indisponíveis.
2. **Testes de Concorrência e Fila:**
   - Enfileiramento de lote simulado de 10 itens com limite de 2 slots: verificar se exatamente 2 executam simultaneamente e todos os 10 completam sem vazamento de slots.
   - Cancelamento de 1 item dentro de um lote ativo: verificar se os demais prosseguem.
3. **Testes de Regressão:**
   - Garantir que a análise de vídeos avulsos permaneça idêntica e rápida como na v1.0.0.

---

## 6. Etapas Recomendadas para Implementação Futura

1. **Etapa 1 — Modelos e Validação:** Adicionar `PlaylistInfo`, `PlaylistItemInfo` e suporte a URLs de playlist em `UrlValidator`.
2. **Etapa 2 — Motor de Extração:** Implementar método de extração leve em `YtDlpService` e `YoutubeService`.
3. **Etapa 3 — Testes Automatizados:** Adicionar testes de unidade e integração para o parser de playlists.
4. **Etapa 4 — Interface Gráfica:** Criar a seção visual para seleção de itens da playlist na aba Downloader (WPF / XAML).
5. **Etapa 5 — Conexão com a Fila:** Implementar o comando de enfileiramento em lote no `MainViewModel`.
6. **Etapa 6 — Validação e Homologação:** Executar testes manuais, suíte automatizada e validação no CI.
