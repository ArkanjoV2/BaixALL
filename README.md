# BaixALL — Aplicativo Desktop para Download de Vídeos

**BaixALL** é um aplicativo desktop nativo para Windows desenvolvido em **C#**, **.NET 10 LTS** e **WPF** com o padrão arquitetural **MVVM**. Ele foi projetado para baixar vídeos e áudios do YouTube utilizando **yt-dlp**, **FFmpeg** e **Deno**, priorizando preservação da qualidade original, interface moderna estilo Windows 11 Fluent e privacidade absoluta (100% local, sem telemetria ou servidores externos).

---

## 🎯 Características Principais

- **Preservação Máxima da Qualidade:** O modo padrão *"Melhor qualidade disponível"* seleciona as faixas originais de vídeo e áudio em resolução máxima (4K, 1080p, 60 FPS, etc.) e utiliza o FFmpeg para remux ou mesclagem direta sem recodificação desnecessária.
- **Interface Windows 11 Nativa:** Design limpo, tipografia Segoe UI Variable, temas Claro e Escuro dinâmicos, cantos retos em todos os controles estruturais e cantos arredondados exclusivos na caixa de entrada da URL.
- **Independência Total de Runtimes Externos:** Não exige Python, Node.js ou Visual Studio no computador do usuário final.
- **DependencyManager Autônomo:** Detecta, baixa e atualiza atômica e seguramente os binários oficiais de `yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe` e `deno.exe`.
- **Comunicação Estruturada:** Uso exclusivo de saídas estruturadas do yt-dlp (`-J` / `--dump-single-json` e `--progress-template`) com leitura assíncrona de stdout e stderr.
- **Segurança contra Injeção de Comandos:** Uso estrito de `ProcessStartInfo.ArgumentList` em todas as invocações de processos externos.
- **Fila com Limite de Concorrência:** Suporte a múltiplos downloads com controle de concorrência configurável (1 a 3 simultâneos).
- **Cancelamento Limpo:** Finalização da árvore de processos (`Kill(entireProcessTree: true)`) e exclusão de arquivos parciais (`.part`, `.ytdl`), preservando arquivos íntegros do usuário.
- **Modo Somente Áudio:** Extração direta de áudio (Original, M4A, Opus) e conversão para MP3 em alta qualidade através do FFmpeg.
- **Histórico e Logs Locais:** Histórico de downloads persistido em JSON e logs rotativos protegidos contra dados sensíveis.

---

## 🏗️ Arquitetura da Aplicação

```
BaixALL/
├── src/
│   └── BaixALL.App/
│       ├── App.xaml / App.xaml.cs            # Injeção de dependência e tratamento global de erros
│       ├── Infrastructure/
│       │   ├── AppConstants.cs               # Nome da aplicação e caminhos locais
│       │   └── ProcessRunner.cs              # Execução segura com ArgumentList e cancelamento
│       ├── Models/
│       │   ├── VideoInfo.cs                  # Metadados de vídeos do YouTube
│       │   ├── FormatOption.cs               # Resoluções amigáveis (4K 60fps, 1080p, etc.)
│       │   ├── DownloadRequest.cs            # Requisição e progresso de download
│       │   ├── DownloadStatus.cs             # Ciclo de vida do download
│       │   ├── AppSettings.cs                # Modelo de preferências salvas
│       │   └── DependencyItem.cs             # Estado das ferramentas gerenciadas
│       ├── Services/
│       │   ├── IYtDlpService.cs / YtDlpService.cs                 # Execução do yt-dlp com JSON e progresso
│       │   ├── IYoutubeService.cs / YoutubeService.cs             # Validação e orquestração de metadados
│       │   ├── IFormatSelectionService.cs / FormatSelectionService.cs # Agrupamento de resoluções e FPS
│       │   ├── IDownloadService.cs / DownloadService.cs           # Fila com semáforo de concorrência
│       │   ├── IDependencyManager.cs / DependencyManager.cs       # Gerenciamento de ferramentas oficiais
│       │   ├── ISettingsService.cs / SettingsService.cs           # Persistência de preferências em JSON
│       │   ├── IHistoryService.cs / HistoryService.cs             # Histórico local de downloads
│       │   ├── ILoggerService.cs / LoggerService.cs               # Logging rotativo sanitizado
│       │   └── IUpdateService.cs / UpdateService.cs               # Verificação de atualizações no GitHub
│       ├── Helpers/
│       │   ├── UrlValidator.cs               # Validação e normalização de URLs
│       │   ├── FileHelper.cs                 # Sanitização de nomes de arquivos para Windows
│       │   ├── ByteSizeFormatter.cs          # Formatação legível de bytes e velocidade
│       │   └── TimeFormatter.cs              # Formatação de duração e tempo restante (ETA)
│       ├── ViewModels/
│       │   ├── MainViewModel.cs              # Orquestrador da tela principal e abas
│       │   ├── SettingsViewModel.cs          # Gerenciamento de preferências
│       │   ├── HistoryViewModel.cs           # Gerenciamento do histórico
│       │   ├── DependenciesViewModel.cs      # Instalação inicial e status das ferramentas
│       │   └── DownloadItemViewModel.cs      # Estado reativo de cada item em download
│       ├── Views/
│       │   ├── MainWindow.xaml / .cs         # Janela principal com design Fluent Windows 11
│       │   ├── SettingsView.xaml / .cs       # Visualização de preferências e atualizações
│       │   ├── HistoryView.xaml / .cs        # Visualização do histórico
│       │   └── DependenciesView.xaml / .cs   # Visualização e download de dependências
│       └── Resources/
│           ├── Icons.xaml                    # Geometrias vetoriais XAML de ícones
│           ├── Styles.xaml                   # Design System (cantos retos, cantos arredondados na URL)
│           └── Themes/
│               ├── DarkTheme.xaml            # Tema escuro Fluent
│               └── LightTheme.xaml           # Tema claro Fluent
├── tests/
│   └── BaixALL.Tests/                        # Testes unitários com xUnit
├── installer/
│   └── BaixALL_Setup.iss                     # Script de instalador Windows (Inno Setup)
├── publish/win-x64/                          # Executável compilado self-contained
└── README.md
```

---

## 💻 Pré-requisitos de Desenvolvimento

- **Sistema Operacional:** Windows 10/11 (x64)
- **SDK:** .NET 10.0 SDK (ou superior)
- **Git:** Git para Windows
- *(Opcional)* **Inno Setup 6+:** Para compilação do script `.iss` do instalador

---

## 🚀 Como Executar em Desenvolvimento

Para rodar a aplicação diretamente pelo código-fonte:

```powershell
# Restaurar dependências
dotnet restore

# Executar a aplicação WPF
dotnet run --project src/BaixALL.App/BaixALL.App.csproj
```

---

## 🧪 Como Executar os Testes Unitários

O projeto conta com suíte completa de testes que não depende de conexão ativa com o YouTube (com dados mockados e testes determinísticos):

```powershell
dotnet test
```

Suítes testadas:
- Validação e extração de identificadores de URLs (`UrlValidationTests`)
- Sanitização de nomes de arquivo e proteção de nomes reservados do DOS/Windows (`FilenameSanitizerTests`)
- Análise de fluxos, detecção de 60 FPS e seleção de formatos (`FormatSelectionTests`)
- Interpretação de JSON estruturado retornado pelo yt-dlp (`YtDlpJsonParsingTests`)
- Parsing de modelos de progresso e tradução de mensagens de erro (`ProgressParsingTests`)
- Verificação de caminhos e rotinas do gerenciador de dependências (`DependencyManagerTests`)
- Persistência e integridade das configurações (`SettingsTests`)
- Fila de downloads, cálculo de taxas e histórico (`DownloadQueueTests`)
- Validações de entrada do serviço do YouTube (`YoutubeServiceTests`)

---

## 📦 Como Publicar a Versão Release (Self-Contained)

Para gerar o executável autônomo para Windows 64-bit que roda em qualquer computador sem exigir que o .NET esteja instalado:

```powershell
dotnet publish src/BaixALL.App/BaixALL.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish/win-x64
```

O arquivo final gerado será:
`publish\win-x64\BaixALL.exe`

---

## 🛠️ Onde Ficam Armazenados os Dados

Para garantir total conformidade com as diretrizes do Windows e preservar a integridade do sistema, todos os dados locais são salvos no perfil do usuário:

| Componente | Caminho no Sistema |
| :--- | :--- |
| **Ferramentas (`yt-dlp`, `FFmpeg`, `Deno`)** | `%LOCALAPPDATA%\BaixALL\tools\` |
| **Configurações (`settings.json`)** | `%LOCALAPPDATA%\BaixALL\settings.json` |
| **Histórico (`history.json`)** | `%LOCALAPPDATA%\BaixALL\history.json` |
| **Logs de Diagnóstico** | `%LOCALAPPDATA%\BaixALL\logs\baixall_YYYYMMDD.log` |
| **Pasta Padrão de Downloads** | `%USERPROFILE%\Downloads` (configurável) |

---

## 🔄 Como Atualizar yt-dlp, FFmpeg e Deno

1. Abra a aplicação BaixALL.
2. Acesse a aba **Configurações** ou **Ferramentas**.
3. Clique no botão **"Verificar Atualizações"**.
4. O BaixALL consultará as APIs oficiais do GitHub e permitirá atualizar os executáveis com um clique.
5. As atualizações utilizam substituição atômica (download em arquivo temporário -> validação com `--version` -> substituição segura).

> [!NOTE]
> O aplicativo impede atualizações de ferramentas enquanto houver downloads ativos na fila para evitar corrupção de arquivos em uso.

---

## 💿 Como Gerar o Instalador Windows

Se o compilador do **Inno Setup** (`iscc.exe`) estiver instalado:

```powershell
iscc installer/BaixALL_Setup.iss
```

O instalador `BaixALL_Setup_v1.0.0.exe` será gerado automaticamente na pasta `installer_output/`. Ele:
- Instala o executável self-contained;
- Cria atalhos opcionais na Área de Trabalho e Menu Iniciar;
- Registra o desinstalador oficial no Windows;
- **Não apaga os vídeos baixados** ao desinstalar.

---

## ⚠️ Limitações Conhecidas e Segurança

- **Vídeos Privados / Protegidos por DRM:** O aplicativo não suporta download de vídeos privados ou com proteção DRM.
- **Vídeos com Restrição de Idade:** Podem requerer autenticação ou desafio bot. O BaixALL inclui o Deno para responder a assinaturas de scripts modernos do YouTube, mas respeita as restrições impostas pela plataforma.
- **Uso Responsável:** Utilize o BaixALL exclusivamente para baixar conteúdos sob permissão do autor ou sob regras de uso legítimo e domínio público.
