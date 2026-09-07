# BaixALL 1.0.0 — Versão Estável Oficial

Temos o prazer de apresentar o **BaixALL 1.0.0**, a primeira versão estável do aplicativo desktop para Windows projetado para o download de vídeos e extração de áudios do YouTube na qualidade selecionada, com processamento local e foco em privacidade.

Esta versão foi validada através de suíte de testes automatizados e verificada em ambiente limpo de máquina virtual Windows 11.

---

## 📦 Artefatos Oficiais de Distribuição

| Arquivo | Descrição | Tamanho | SHA-256 |
| :--- | :--- | :--- | :--- |
| `BaixALL-Setup-1.0.0.exe` | Instalador oficial para Windows (x64) com assistente de instalação | 44.039.008 bytes | `aa8936531dc763cad319b29f5dcc7e317a7c97b5e7e7f72e761e0e4867179979` |
| `BaixALL-1.0.0-win-x64.zip` | Pacote portátil descompactável (execução direta de `BaixALL.exe`) | 61.057.663 bytes | `7e7c63be4d72b15e75361d3b6a06ddce2d88b34c55158207a9260b02888ff0dd` |
| `checksums.txt` | Hashes de integridade criptográfica SHA-256 de todos os arquivos | 1.250 bytes | *(ver arquivo)* |

---

## ✨ Recursos Implementados

- **Seleção de Resoluções e FPS:** Suporte de 360p até 4K (2160p), com identificação de taxas a 60 FPS ou 30 FPS quando disponibilizadas pela plataforma.
- **Download com Mesclagem Automática:** Seleção da melhor combinação de faixas de vídeo e áudio disponíveis pelo provedor e mesclagem nos formatos **MP4** ou **MKV** via FFmpeg.
- **Modos de Áudio Dedicados:** Extração direta mantendo o formato original (M4A/AAC ou Opus) ou conversão para **MP3** (320 kbps estéreo) através do FFmpeg.
- **Fila com Downloads Simultâneos:** Motor assíncrono com suporte configurável para 1, 2 ou 3 downloads concorrentes e transição automática ao desocupar vaga na fila.
- **Cancelamento Individual:** Interrupção controlada de download específico em andamento com remoção de arquivos temporários parciais (`.part`, `.ytdl`), sem afetar os outros itens da fila.
- **Histórico Local:** Armazenamento das conclusões em arquivo JSON local (`%LOCALAPPDATA%\BaixALL\history.json`), com atalhos para abrir o arquivo de mídia ou destacá-lo no Windows Explorer.
- **Gerenciamento Automático de Ferramentas:** Detecção e atualização sob demanda de `yt-dlp`, `FFmpeg`, `ffprobe` e `Deno` a partir dos lançamentos oficiais no GitHub, mantidos em pasta isolada do usuário sem alteração do `PATH` global.
- **Distribuição Autônoma (Self-Contained):** Runtime .NET 10 LTS embutido. Não requer .NET SDK, Visual Studio, Python ou Node.js instalados na máquina do usuário.
- **Interface em Português:** Estilo visual moderno inspirado no Fluent Design com suporte aos temas Claro e Escuro.

---

## 💻 Requisitos do Sistema

- **Sistema Operacional:** Windows 10 (versão 1809 ou superior) ou Windows 11.
- **Arquitetura:** 64-bit (x64 / AMD64).
- **Runtimes:** Nenhum runtime adicional necessário.
- **Espaço Livre em Disco:** Pelo menos 300 MB livres para a aplicação e ferramentas auxiliares, além do espaço livre necessário para as mídias baixadas.
- **Rede:** Conexão ativa com a internet.

---

## ⚠️ Limitações e Avisos aos Usuários

- **Disponibilidade no YouTube:** O BaixALL depende da disponibilidade dos vídeos e da capacidade técnica do motor `yt-dlp`. Vídeos privados, restritos por região geográfica, conteúdos com proteção comercial por DRM ou com exigência de login com desafios severos de verificação humana não são suportados. O projeto **não promete compatibilidade universal com todo e qualquer vídeo**.
- **Atualização Contínua das Ferramentas:** Como a plataforma de vídeos atualiza periodicamente seus formatos e desafios, recomendamos utilizar a aba **Ferramentas** do BaixALL para verificar e atualizar o `yt-dlp` caso alguma análise falhe.
- **Aviso de Reputação do Windows SmartScreen:**
  O Windows Defender SmartScreen pode exibir um alerta de reputação (*"O Windows protegeu o seu computador"*) para novos binários de código aberto recém-lançados. Isso ocorre em virtude da ausência de histórico acumulado na nuvem da Microsoft e não representa ameaça.
  - Para prosseguir: confirme a origem oficial da release, clique em **"Mais informações"** e no botão **"Executar assim mesmo"**.
  - **Sobre Assinatura Digital:** Aplicativos novos podem apresentar avisos de reputação do SmartScreen mesmo quando assinados digitalmente (inclusive com certificados comerciais OV ou EV), uma vez que a reputação no ecossistema Windows é construída progressivamente com a adoção e telemetria ao longo do tempo. O BaixALL não desabilita proteções do sistema operacional e não utiliza certificados falsos/autoassinados. A assinatura com certificado comercial válido poderá ser avaliada futuramente.
- **Desinstalação Segura:** O assistente de desinstalação remove estritamente os executáveis do programa. Seus vídeos salvos na pasta Downloads e os dados em `%LOCALAPPDATA%\BaixALL` permanecem preservados.
