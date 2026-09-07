# BaixALL 1.0.0 — Versão Estável Oficial (Definitiva)

Temos o prazer de anunciar o lançamento do **BaixALL 1.0.0**, a primeira versão estável e definitiva do aplicativo desktop para Windows projetado para baixar vídeos e áudios do YouTube com qualidade máxima, alta velocidade, estabilidade e total respeito à privacidade.

Esta versão foi exaustivamente testada em 122 testes automatizados e formalmente validada em instalação limpa em máquina virtual Windows 11.

---

## 📦 Artefatos Oficiais de Distribuição

| Arquivo | Descrição | Tamanho | SHA-256 |
| :--- | :--- | :--- | :--- |
| `BaixALL-Setup-1.0.0.exe` | Instalador oficial para Windows (x64) com assistente e atalhos | 44.039.008 bytes | `aa8936531dc763cad319b29f5dcc7e317a7c97b5e7e7f72e761e0e4867179979` |
| `BaixALL-1.0.0-win-x64.zip` | Pacote portátil descompactável (execução direta de `BaixALL.exe`) | 61.057.663 bytes | `7e7c63be4d72b15e75361d3b6a06ddce2d88b34c55158207a9260b02888ff0dd` |
| `checksums.txt` | Hashes de integridade criptográfica SHA-256 de todos os arquivos | 1.250 bytes | *(ver arquivo)* |

---

## ✨ Funcionalidades em Destaque

- **Download na Melhor Qualidade Disponível:** Identifica e baixa automaticamente a melhor combinação de faixas de vídeo e áudio em taxa máxima de bits.
- **Seleção de Resoluções e FPS:** Suporte completo de 360p até 4K (2160p), com detecção e seleção precisa de taxas a 60 FPS ou 30 FPS.
- **Suporte a Vídeo e Áudio Separados:** Baixa fluxos de vídeo e áudio de alta fidelidade disponibilizados separadamente pelos servidores.
- **Mesclagem com FFmpeg:** Integração transparente com o FFmpeg para mesclagem sem perda de sincronia e sem recodificação desnecessária nos formatos **MP4** e **MKV**.
- **Modos de Áudio Dedicados:** Extração direta mantendo a faixa original (M4A/AAC ou Opus) ou conversão em alta qualidade para **MP3** (320 kbps estéreo) através do FFmpeg.
- **Fila com Downloads Simultâneos:** Motor assíncrono com suporte a 1, 2 ou 3 downloads concorrentes e transição automática ao desocupar vaga na fila.
- **Cancelamento Individual Seguro:** Interrupção imediata de download específico sem afetar outros downloads ativos, com limpeza automática de arquivos residuais (`.part`, `.ytdl`).
- **Histórico Persistente:** Armazenamento seguro e concorrente em `%LOCALAPPDATA%\BaixALL\history.json`, com opções para abrir o arquivo de mídia ou localizá-lo na pasta.
- **Gerenciamento Automático das Ferramentas:** Instalação e atualização transparente de `yt-dlp`, `FFmpeg`, `ffprobe` e `Deno` direto dos canais oficiais do GitHub, mantidos em diretório isolado sem poluir o `PATH` do sistema.
- **Instalador e Versão Portátil:** Opção de instalação tradicional no perfil do usuário (sem privilégios administrativos obrigatórios) ou execução portátil direta via arquivo `.zip`.
- **Interface 100% em Português:** Interface moderna inspirada no Fluent Design do Windows 11, com suporte nativo a temas Claro e Escuro.

---

## 💻 Requisitos do Sistema

- **Sistema Operacional:** Windows 10 (versão 1809 ou superior) ou Windows 11.
- **Arquitetura:** 64-bit (x64 / AMD64).
- **Runtimes:** Nenhum runtime adicional é necessário (.NET 10 LTS embutido em modo *self-contained*). Não requer .NET SDK, Visual Studio, Python ou Node.js instalados.
- **Espaço Livre em Disco:** Pelo menos 300 MB livres para a aplicação e ferramentas auxiliares, além do espaço livre necessário para as mídias baixadas.
- **Rede:** Conexão estável com a internet.

---

## ⚠️ Limitações Conhecidas e Esclarecimentos

- **Disponibilidade no YouTube:** O BaixALL depende da disponibilidade dos vídeos e da capacidade técnica do motor `yt-dlp`. Vídeos privados, restritos por região, conteúdos com proteção comercial por DRM ou que exijam autenticação/login com desafios severos de verificação humana podem não ser passíveis de download. O projeto **não promete suporte universal a todo e qualquer vídeo**.
- **Atualização Contínua das Ferramentas:** Como a plataforma de vídeos atualiza frequentemente seus formatos e algoritmos de entrega, utilize a aba **Ferramentas** do BaixALL para manter o `yt-dlp` atualizado na versão estável mais recente.
- **Aviso de Reputação do Windows SmartScreen:**
  - Por se tratar de um aplicativo independente de código aberto recém-compilado, o Windows Defender SmartScreen pode exibir o alerta *"O Windows protegeu o seu computador"*.
  - Isso decorre exclusivamente da ausência de histórico estatístico de downloads acumulado nos servidores da Microsoft e não representa ameaça nem malware.
  - Para executar: clique em **"Mais informações"** e depois no botão **"Executar assim mesmo"**.
  - **Nota sobre Assinatura:** Aplicativos novos podem apresentar avisos de reputação do SmartScreen **mesmo quando assinados digitalmente** (inclusive com certificados comerciais OV ou EV), uma vez que a reputação no ecossistema Windows é construída progressivamente com o volume de adoção e tempo. O BaixALL não desabilita proteções do sistema operacional e não utiliza certificados falsos/autoassinados. A assinatura com certificado comercial válido poderá ser tratada futuramente caso o mantenedor decida contratar um serviço.
- **Desinstalação Segura:** O assistente de desinstalação remove estritamente os executáveis do programa. Seus vídeos salvos na pasta Downloads e os dados em `%LOCALAPPDATA%\BaixALL` permanecem 100% preservados.
