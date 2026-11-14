# Changelog

Todas as alterações notáveis deste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/)
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

---

## [1.4.0] - 2026-11-13

### Integração de Suporte ao TikTok com Verificação de Marca-d'Água

Adiciona suporte nativo à análise e ao download de vídeos públicos do **TikTok**, priorizando fontes diretas de reprodução sem marca-d'água disponibilizadas pela plataforma e garantindo transparência total sobre o estado de cada formato:

### ✨ Funcionalidades Adicionadas
- **Reconhecimento e Normalização de URLs do TikTok:** Suporte a links canônicos (`tiktok.com/@usuario/video/ID`, `m.tiktok.com/v/ID.html`, `tiktok.com/share/video/ID`, `tiktok.com/embed/v2/ID`) e links curtos compartilhados (`vm.tiktok.com`, `vt.tiktok.com`, `tiktok.com/t/`), com resolução para chave canônica única (`tt:<videoId>`) sem colisão com YouTube (`yt:`), Instagram (`ig:`) e X/Twitter (`x:`).
- **Identificação Visual da Plataforma:** Badge turquesa `TikTok` (`#25F4EE` sobre `#112A30`) integrado à tela de análise, à fila de downloads e ao histórico persistente, com inferência retrocompatível para itens de histórico.
- **Classificação Estrita de Marca-d'Água (`WatermarkStatus`):**
  - `NoWatermark` (`✓ Sem marca-d'água`): atribuído exclusivamente quando há evidência positiva nos metadados do extrator (`play_addr`, nota `Direct video` / `Playback video` ou padrão de fluxo de reprodução `bytevc1_<res>_<bitrate>-<idx>` / `h264_<res>_<bitrate>-<idx>` sem marcador `watermark`).
  - `Unknown` (`• Marca-d'água não verificada`): atribuído quando os metadados não permitem confirmar ausência nem presença de sobreposição; não promete vídeo limpo e exige seleção manual explícita pelo usuário.
  - `Watermarked` (`⚠ Apenas com marca-d'água`): atribuído a formatos explicitamente identificados com marca-d'água (`download` / `download_addr` com nota `watermarked`); nunca é selecionado automaticamente e exige consentimento explícito na interface (`AllowWatermarkedDownload`).
- **Seletores Determinísticos por `format_id` Exatos:** Opções `NoWatermark` e `Unknown` utilizam exclusivamente os `format_id` exatos validados na análise (combinando `+bestaudio` apenas quando a faixa de vídeo limpa não possui áudio embutido), sem seletores genéricos por prefixo ou fallback `best[...]`, descartando fluxos `bytevc2` incompatíveis (`UNPLAYABLE`) e respeitando rigorosamente os limites de resolução (`height <= maxHeight`).
- **Proteção contra Fallback Silencioso em Tempo de Download:** Caso uma fonte limpa selecionada deixe de estar disponível no momento da execução do download, o processo falha de forma segura e informa o usuário, sem baixar silenciosamente formatos `Unknown` ou `Watermarked`.
- **Rejeição Amigável de Escopos Não Suportados:** Publicações de fotos/slideshows (`/photo/` ou entradas apenas de áudio de slideshow), transmissões ao vivo (`/live`), Stories (`/story/`), perfis inteiros (`/@usuario`), vídeos privados (`10216`), removidos (`10202`), bloqueados por IP (`10204`) e links curtos expirados retornam mensagens claras em português.

### 🛡️ Qualidade, Concorrência e Testes
- **Persistência Determinística no Histórico:** O registro no `HistoryService` ocorre antes da sinalização de `IsCompleted = true` no item da fila, eliminando condição de corrida na leitura imediata do histórico após conclusão.
- **Suíte de Testes Expandida:** Adicionados 65 testes dedicados ao TikTok (incluindo simulação real e obrigatória do `yt-dlp` com `--load-info-json` local e no GitHub Actions), totalizando **307 testes automatizados** 100% aprovados.

---

## [1.0.0] - 2026-09-07

### Versão Estável Oficial (Definitiva)

Primeira versão estável oficial do **BaixALL** para Windows 10 e Windows 11 (x64), totalmente validada e aprovada em ambiente limpo de máquina virtual:

- **Validação Completa em VM Windows Limpa:** Confirmada a inicialização perfeita, resolução canônica de recursos BAML (pack URIs), carregamento do ícone oficial e ausência de dependências de desenvolvimento externas no sistema hospedeiro.
- **Resiliência e Concorrência:** Motor de fila assíncrono com suporte a downloads concorrentes configuráveis (1, 2 ou 3), cancelamento seletivo de árvore de processos e isolamento estrito por job (`DownloadContext`).
- **Blindagem Operacional:** Despachante WPF com fallback síncrono automático contra deadlocks e desligamentos, e sistema de logs detalhado com inspeção recursiva de toda a cadeia de `InnerException`.
- **Suíte de Testes 100% Aprovada:** 122 testes automatizados cobrindo parsing de formatos, merge com FFmpeg, concorrência, histórico persistente e ciclo de vida da interface.
- **Distribuição Segura:** Instalador Inno Setup e pacote portátil com integridade verificada via SHA-256 e desinstalador seguro que preserva vídeos e dados do usuário.

---

## [1.0.0-rc.2] - 2026-09-07

### Release Candidate 2

Segunda versão candidata à distribuição oficial do **BaixALL** para Windows 10 e Windows 11 (x64), contendo a correção crítica para carregamento de recursos XAML em instalações limpas e blindagem de logs:

- **Correção de XAML / BAML Resource:** Embutido o arquivo de ícone `Resources/icon.ico` como recurso compilado da aplicação (`<Resource Include="Resources\icon.ico" />`) no projeto e atualizado o apontamento em `MainWindow.xaml` para o Pack URI canônico (`pack://application:,,,/Resources/icon.ico`). Isso elimina a exceção `XamlParseException` / `IOException` (`Não é possível localizar o recurso 'resources/icon.ico'`) que ocorria ao executar o aplicativo fora do ambiente de desenvolvimento.
- **Blindagem do Sistema de Logs (`LoggerService`):** O formatador de erros agora percorre recursivamente toda a cadeia de `InnerException`, registrando `Tipo`, `Mensagem`, `HResult`, `Source`, `StackTrace` de cada nível e o despejo integral de `ToString()`.
- **Experiência de Erro Amigável (`App.xaml.cs`):** Diálogos de falha fatal agora indicam o caminho da pasta de logs (`%LOCALAPPDATA%\BaixALL\logs`) sem expor stack traces confusas na interface do usuário.
- **Resiliência do Dispatcher (`DispatcherService`):** O serviço de despacho para a UI agora verifica se o despachante WPF está ativo, se a thread está viva e captura com segurança `TaskCanceledException`/`OperationCanceledException` durante o encerramento da aplicação ou em ambientes de testes, executando fallback síncrono imediato para eliminar deadlocks e travamentos.
- **Testes Automatizados de Regressão:** Adicionado teste que valida a instanciação e carregamento BAML da `MainWindow` em thread STA isolada, e aprimorada a verificação determinística de liberação de slots na concorrência de fila (totalizando 122 testes automatizados 100% aprovados).

---

## [1.0.0-rc.1] - 2026-09-07

### Release Candidate 1

Primeira versão candidata à distribuição oficial do **BaixALL** para Windows 10 e Windows 11 (x64).

### ✨ Funcionalidades Adicionadas
- **Análise Inteligente de Mídias:** Extração assíncrona de metadados via saída JSON estruturada do yt-dlp (`-J`), exibindo miniatura em alta resolução, canal, duração e formatos disponíveis.
- **Seleção Avançada de Resoluções:** Reconhecimento automático de taxas de quadros elevadas (60 FPS) e resoluções desde 4K (2160p), 1440p, 1080p, 720p até resoluções menores.
- **Modo "Melhor qualidade disponível":** Download das faixas de vídeo e áudio em qualidade máxima com mesclagem transparente pelo FFmpeg.
- **Modo Somente Áudio:** Extração direta e preservação dos fluxos originais (M4A/AAC, Opus) e conversão para MP3 em alta qualidade via FFmpeg.
- **Fila com Controle de Concorrência:** Motor de fila assíncrono com suporte a 1, 2 ou 3 downloads simultâneos configuráveis, com transição automática ao liberar vaga.
- **Isolamento de Downloads:** Contexto de execução exclusivo por job (`DownloadContext`), eliminando colisões de arquivos temporários e variáveis compartilhadas.
- **Cancelamento Individual Seguro:** Interrupção seletiva da árvore de processos (`Kill(entireProcessTree: true)`) e remoção de resíduos parciais (`.part`, `.ytdl`), sem afetar os demais itens ativos na fila.
- **Histórico Persistente e Thread-Safe:** Gravação atômica em JSON (`history.json`) com proteção contra race conditions e ações para abrir o arquivo ou localizá-lo na pasta.
- **DependencyManager Autônomo:** Detecção, download e atualização das ferramentas oficiais (`yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`, `deno.exe`) direto de fontes oficiais no GitHub.
- **Proteção em Tempo de Execução:** Bloqueio inteligente de atualizações de ferramentas enquanto houver downloads em andamento.
- **Design Fluent Windows 11:** Interface moderna com temas Claro e Escuro, cantos retos nas seções estruturais e cantos arredondados no campo de URL.
- **Ícone de Marca Oficial:** Ícone nativo multi-resolução (.ico) integrado ao executável, janelas e instalador.
- **Instalador Inno Setup:** Empacotamento profissional de 64 bits com desinstalador seguro que preserva intactos os vídeos do usuário.
- **Testes Automatizados:** Suíte com 121 testes cobrindo validação de URLs, parsing de progresso, seleção de formatos, concorrência e isolamento.

### 🛡️ Segurança e Integridade
- Execução estrita via `ProcessStartInfo.ArgumentList` em todas as invocações de linha de comando para prevenção de injeção.
- Sanitização rigorosa de nomes de arquivo contra caracteres proibidos no Windows (`:`, `*`, `?`, `"`, `<`, `>`, `|`) e nomes reservados do DOS (`CON`, `PRN`, `AUX`, etc.).
- Comunicação 100% local: sem telemetria, sem servidores intermediários e sem envio de dados de usuário.
