# Changelog

Todas as alterações notáveis deste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/)
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

---

## [1.0.0] - 2026-09-07

### Versão Estável Oficial (Lançamento Oficial)

Primeiro lançamento estável oficial do **BaixALL** para Windows 10 e Windows 11 (x64), após ciclo completo de testes unitários (121 testes aprovados), testes de concorrência, validação com ffprobe de merges em 4K 60fps, áudio MP3, cancelamento limpo e conformidade legal estrita (GPLv3).

- Todas as funcionalidades da Release Candidate foram consolidadas e promovidas para a versão estável 1.0.0.
- Preservação comprovada de dados do usuário e downloads na desinstalação.
- Empacotamento profissional em modo self-contained (win-x64) sem dependências externas de .NET ou runtimes adicionais.

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
