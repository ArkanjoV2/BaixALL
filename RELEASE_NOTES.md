# BaixALL 1.0.0 — Versão Estável Oficial (Definitiva)

Temos o prazer de anunciar o lançamento do **BaixALL 1.0.0**, a primeira versão estável e definitiva do aplicativo desktop para Windows projetado para baixar vídeos e áudios do YouTube com qualidade máxima, segurança e privacidade absoluta.

Esta versão foi exaustivamente validada em testes automatizados e aprovada em instalação em máquina virtual Windows 11 limpa.

---

## 📦 Artefatos de Distribuição (1.0.0)

| Arquivo | Descrição |
| :--- | :--- |
| `BaixALL-Setup-1.0.0.exe` | Instalador oficial para Windows 10/11 (x64) com assistente de instalação |
| `BaixALL-1.0.0-win-x64.zip` | Pacote portátil descompactável (execução direta de `BaixALL.exe`) |
| `checksums.txt` | Hashes de integridade criptográfica SHA-256 de todos os arquivos |

---

## ✨ Principais Funcionalidades

- **Análise Rápida e Não Invasiva:** Extração assíncrona de metadados via saída JSON estruturada do yt-dlp (`-J`), exibindo miniatura, canal, duração e formatos disponíveis.
- **Suporte a Altas Resoluções e 60 FPS:** Reconhecimento e seleção de resoluções de 360p até 4K (2160p) a 60 FPS.
- **Modo "Melhor Qualidade Disponível":** Download transparente das faixas separadas de vídeo e áudio em bitrate máximo com mesclagem via FFmpeg.
- **Modo Somente Áudio:** Extração direta e preservação dos fluxos originais (M4A/AAC, Opus) ou conversão em alta qualidade para MP3.
- **Fila com Controle de Concorrência:** Motor assíncrono com suporte a 1, 2 ou 3 downloads simultâneos, com transição automática ao liberar vaga.
- **Isolamento por Job (`DownloadContext`):** Previne colisões de arquivos temporários e concorrência indesejada.
- **Cancelamento Seletivo Seguro:** Interrupção de processos (`Kill(entireProcessTree: true)`) e faxina de resíduos parciais (`.part`, `.ytdl`) sem interferir nos demais downloads ativos.
- **Histórico Persistente e Concorrente:** Gravação segura em JSON (`history.json`) com atalhos para abrir o arquivo ou localizá-lo na pasta.
- **DependencyManager Autônomo:** Detecção, download e atualização das ferramentas oficiais (`yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`, `deno.exe`) diretamente das fontes oficiais no GitHub.
- **Design Fluent Windows 11:** Interface moderna nos temas Claro e Escuro, com feedback em tempo real de progresso e velocidade.

---

## 💻 Requisitos do Sistema

- **Sistema Operacional:** Windows 10 (versão 1809 ou superior) ou Windows 11.
- **Arquitetura:** 64-bit (x64 / AMD64).
- **Runtimes:** Nenhum runtime adicional é necessário. O BaixALL é distribuído em modo *self-contained* (.NET 10 LTS embutido), não exigindo instalação prévia de .NET SDK, Python ou Node.js.
- **Espaço em Disco:** Mínimo de 300 MB livres para a aplicação e ferramentas, além do espaço necessário para os vídeos baixados.
- **Conexão:** Acesso à internet para análise e download de vídeos e ferramentas.

---

## 🚀 Como Instalar e Utilizar

### Instalação via Instalador
1. Baixe `BaixALL-Setup-1.0.0.exe`.
2. Execute o assistente de instalação e selecione se deseja criar atalhos na Área de Trabalho e no Menu Iniciar.
3. Conclua e abra o BaixALL.

### Versão Portátil
1. Baixe `BaixALL-1.0.0-win-x64.zip`.
2. Extraia o conteúdo para uma pasta de sua preferência.
3. Execute diretamente `BaixALL.exe`.

### Uso Básico
1. Na aba **Downloader**, cole o link de um vídeo do YouTube e clique em **Analisar**.
2. Escolha a **Qualidade do Vídeo**, o **Formato / Container** ou ative **Somente Áudio**.
3. Confirme ou altere a **Pasta de Destino**.
4. Clique em **Baixar Vídeo** / **Baixar Áudio**. O item entrará na fila e o progresso poderá ser acompanhado em tempo real.

---

## ⚠️ Limitações Conhecidas e Esclarecimentos

- **Disponibilidade de Mídias:** O BaixALL depende da disponibilidade pública dos vídeos no YouTube e da capacidade do motor `yt-dlp`. Vídeos privados, restritos por região, protegidos por DRM comercial ou com exigência de login/captcha podem não ser passíveis de download. O projeto **não promete suporte universal a todo e qualquer vídeo**.
- **Atualizações de Ferramentas:** Como o YouTube frequentemente altera seus algoritmos internos de entrega de vídeo, recomendamos utilizar a aba **Ferramentas** para manter o `yt-dlp` sempre atualizado na versão mais recente.
- **Aviso de Reputação do Windows SmartScreen:** Por ser um aplicativo de código aberto recém-compilado e distribuído de forma independente, o Windows Defender SmartScreen pode exibir um alerta (*"O Windows protegeu o seu computador"*). Isso decorre da ausência de histórico estatístico de downloads no serviço da Microsoft e não indica malware. Para abrir, clique em **"Mais informações"** e em seguida em **"Executar assim mesmo"**. A assinatura com certificado comercial Authenticode é tratada como uma etapa opcional de distribuição pública futura.
- **Desinstalação Segura:** A desinstalação do BaixALL remove exclusivamente os binários da aplicação. Seus vídeos salvos na pasta Downloads e os dados em `%LOCALAPPDATA%\BaixALL` permanecem preservados.
