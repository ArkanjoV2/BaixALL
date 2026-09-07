# BaixALL

<p align="center">
  <strong>Aplicativo desktop para Windows que permite baixar vídeos e áudios na qualidade selecionada, com suporte a yt-dlp, FFmpeg, fila com downloads simultâneos e gerenciamento automático de ferramentas.</strong>
</p>

<p align="center">
  <a href="https://github.com/ArkanjoV2/BaixALL/releases/latest"><img src="https://img.shields.io/badge/Vers%C3%A3o-1.0.0_Est%C3%A1vel-blue.svg" alt="Versão Atual"></a>
  <a href="https://github.com/ArkanjoV2/BaixALL/blob/main/LICENSE"><img src="https://img.shields.io/badge/Licen%C3%A7a-MIT-green.svg" alt="Licença"></a>
  <img src="https://img.shields.io/badge/Plataforma-Windows_10_%2F_11_(x64)-blue" alt="Plataforma">
  <img src="https://img.shields.io/badge/Tecnologia-.NET_10_%7C_WPF-purple" alt="Tecnologia">
</p>

<p align="center">
  <a href="https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/BaixALL-Setup-1.0.0.exe">
    <img src="https://img.shields.io/badge/Baixar_Instalador_Oficial_(Setup.exe)-2ea44f?style=for-the-badge&logo=windows&logoColor=white" alt="Baixar Instalador Oficial">
  </a>
  <br>
  <a href="https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/BaixALL-1.0.0-win-x64.zip">
    <em>Ou baixe a Versão Portátil (.ZIP)</em>
  </a>
  &nbsp;•&nbsp;
  <a href="https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/checksums.txt">
    <em>Verificar Hashes SHA-256</em>
  </a>
</p>

---

## 📋 Sumário

- [Visão Geral](#-visão-geral)
- [Capturas de Tela](#-capturas-de-tela)
- [Recursos Principais](#-recursos-principais)
- [Requisitos do Sistema](#-requisitos-do-sistema)
- [Instalação](#-instalação)
- [Como Utilizar](#-como-utilizar)
  - [1. Analisar e Baixar um Vídeo](#1-analisar-e-baixar-um-vídeo)
  - [2. Selecionar Qualidade e Formato](#2-selecionar-qualidade-e-formato)
  - [3. Baixar Somente Áudio](#3-baixar-somente-áudio)
  - [4. Fila com Downloads Simultâneos](#4-fila-com-downloads-simultâneos)
  - [5. Acessar o Histórico](#5-acessar-o-histórico)
  - [6. Atualizar as Ferramentas](#6-atualizar-as-ferramentas)
- [Limitações Conhecidas](#-limitações-conhecidas)
- [Como Relatar Problemas e Obter Suporte](#-como-relatar-problemas-e-obter-suporte)
- [Licença e Créditos](#-licença-e-créditos)

---

## 💡 Visão Geral

O **BaixALL** é um aplicativo desktop nativo desenvolvido em **C#** e **.NET 10** com interface gráfica moderna em **WPF** (padrão MVVM). Criado para proporcionar simplicidade e confiabilidade, ele integra ferramentas consagradas de código aberto (**yt-dlp**, **FFmpeg** e **Deno**) em um único fluxo acessível, sem anúncios, sem telemetria e com execução 100% local no seu computador.

---

## 📸 Capturas de Tela

Abaixo estão os pontos de destaque da interface do BaixALL:

| Aba Principal (Downloader) | Seleção de Resoluções e FPS |
| :---: | :---: |
| ![Tela Principal](docs/screenshots/01-home-analise.png) <br><sub>*Análise de link com título, miniatura e metadados*</sub> | ![Seleção de Qualidade](docs/screenshots/02-selecao-qualidade.png) <br><sub>*Menu inteligente com suporte a 4K e 60 FPS*</sub> |

| Fila de Downloads | Histórico Persistente |
| :---: | :---: |
| ![Fila de Downloads](docs/screenshots/03-fila-download.png) <br><sub>*Acompanhamento de progresso e cancelamento individual*</sub> | ![Histórico de Mídias](docs/screenshots/04-historico.png) <br><sub>*Histórico de conclusões com botões de abrir arquivo e pasta*</sub> |

> ℹ️ *Caso deseje consultar detalhes ou adicionar novas capturas à documentação, consulte o [diretório de screenshots](docs/screenshots/README.md).*

---

## ✨ Recursos Principais

- **Download com Mesclagem Automática:** Detecta as faixas separadas de vídeo e áudio fornecidas pelos servidores e realiza a união em alta definição utilizando o FFmpeg nos containers **MP4** ou **MKV**.
- **Seleção de Resolução e FPS:** Escolha resoluções de 360p até 4K (2160p), com identificação clara de taxas de quadros elevadas (60 FPS ou 30 FPS).
- **Modo Somente Áudio:** Extraia o áudio preservando o fluxo original (M4A/AAC ou Opus) ou converta para **MP3** (320 kbps estéreo) para máxima compatibilidade.
- **Fila com Concorrência Configurável:** Adicione múltiplos downloads sucessivamente e controle o limite de concorrência (1, 2 ou 3 downloads simultâneos) na aba Configurações.
- **Cancelamento Individual Seguro:** Interrompa um download específico em andamento sem interferir nos outros downloads ativos, limpando arquivos temporários parciais (`.part`, `.ytdl`).
- **Histórico Persistente:** Registra downloads concluídos em arquivo local `%LOCALAPPDATA%\BaixALL\history.json`, com atalhos para abrir o arquivo no player ou destacá-lo no Windows Explorer.
- **Gerenciamento Automático de Ferramentas:** Detecta, baixa e atualiza `yt-dlp`, `FFmpeg`, `ffprobe` e `Deno` diretamente dos lançamentos oficiais do GitHub, mantidos em pasta isolada (`%LOCALAPPDATA%\BaixALL\tools`) sem alterar o `PATH` do sistema.
- **Autônomo (Self-Contained):** O runtime .NET 10 LTS já está embutido no aplicativo. Não é necessário instalar .NET SDK, Python, Node.js ou Visual Studio.
- **Interface em Português:** Estilo visual Fluent inspirado no Windows 11, com suporte completo aos temas Claro e Escuro.

---

## 💻 Requisitos do Sistema

- **Sistema Operacional:** Windows 10 (versão 1809 ou superior) ou Windows 11.
- **Arquitetura:** 64-bit (x64 / AMD64).
- **Runtimes Externos:** Nenhum (arquivos autônomos embutidos).
- **Espaço Livre em Disco:** Mínimo de 300 MB para o programa e ferramentas auxiliares, além do espaço livre necessário para as mídias baixadas.
- **Conexão:** Acesso à internet para análise de links e downloads.

---

## 🚀 Instalação

Para instruções detalhadas, consulte o [Guia Completo de Instalação](docs/GUIA_INSTALACAO.md).

### Opção 1: Instalador Oficial (Recomendado)
1. Baixe o instalador oficial: [BaixALL-Setup-1.0.0.exe](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/BaixALL-Setup-1.0.0.exe).
2. Execute o instalador e escolha se deseja criar atalhos na Área de Trabalho e no Menu Iniciar.
3. A instalação ocorre no perfil do usuário (`%LOCALAPPDATA%\Programs\BaixALL`) sem exigir permissões de administrador.

### Opção 2: Pacote Portátil
1. Baixe o arquivo: [BaixALL-1.0.0-win-x64.zip](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/BaixALL-1.0.0-win-x64.zip).
2. Extraia o conteúdo para a pasta de sua preferência.
3. Dê um duplo clique no arquivo `BaixALL.exe` para iniciar imediatamente.

> 🛡️ **Sobre o aviso do Windows SmartScreen:**  
> Por ser um projeto independente e recém-publicado, o Windows Defender SmartScreen pode exibir o alerta *"O Windows protegeu o seu computador"*. Trata-se de um aviso padrão de reputação para novos executáveis na nuvem da Microsoft. Para prosseguir: clique em **"Mais informações"** e depois no botão **"Executar assim mesmo"**. Consulte nosso [Guia de Instalação](docs/GUIA_INSTALACAO.md#3-avisos-de-reputação-do-windows-smartscreen) para mais detalhes.

---

## 📖 Como Utilizar

### 1. Analisar e Baixar um Vídeo
1. Copie o endereço (URL) do vídeo do YouTube no navegador.
2. Na aba **Downloader** do BaixALL, cole o link no campo de entrada e clique em **"Analisar"** (ou pressione `Enter`).
3. O aplicativo exibirá título, miniatura em alta resolução, canal e duração.
4. Clique em **"Baixar Vídeo"** (ou **"Baixar Áudio"**) para enviar à fila.

### 2. Selecionar Qualidade e Formato
- **"Melhor qualidade disponível" (Padrão):** O BaixALL seleciona o fluxo de vídeo de resolução mais alta e a melhor faixa de áudio, mesclando-os automaticamente com o FFmpeg.
- **Resoluções personalizadas:** Selecione opções como 2160p (4K), 1440p (2K), 1080p, 720p, etc., com suporte a taxas de 60 FPS quando fornecidas pela plataforma.
- **Container:** Escolha entre **MP4** (máxima compatibilidade) ou **MKV**.

### 3. Baixar Somente Áudio
1. Marque a caixa **"Somente Áudio"** na tela inicial.
2. Selecione o formato desejado:
   - **Melhor áudio disponível (Original):** Mantém a faixa original sem recodificação (geralmente M4A/AAC ou Opus em WebM).
   - **M4A:** Codec AAC em container MP4, ideal para dispositivos Apple e reprodutores comuns.
   - **Opus:** Codec de alta compressão e excelente fidelidade.
   - **MP3:** Conversão em 320 kbps via FFmpeg para compatibilidade universal (aparelhos de som, rádios automotivos).

### 4. Fila com Downloads Simultâneos
- Acesse a aba **Fila** para acompanhar os itens ativos com barra de progresso, percentual, velocidade e tamanho transferido.
- Na aba **Configurações**, configure o limite de downloads simultâneos (1, 2 ou 3). Ao concluir um download, o próximo item com status *Aguardando* inicia de forma automática.
- Clicar no botão **"Cancelar"** encerra imediatamente o processo correspondente e limpa os arquivos parciais, sem interromper os demais downloads ativos.

### 5. Acessar o Histórico
- Na aba **Histórico**, consulte o registro de mídias concluídas.
- Use o botão **"Abrir arquivo"** para reproduzir a mídia no aplicativo padrão do Windows ou **"Abrir pasta"** para localizar o arquivo no Windows Explorer.
- O botão **"Limpar Histórico"** limpa o registro visual sem apagar os arquivos físicos do disco.

### 6. Atualizar as Ferramentas
O YouTube atualiza periodicamente seus formatos e desafios de assinatura.
- Acesse a aba **Ferramentas** (ou **Configurações**) e clique em **"Verificar Atualizações"**.
- O aplicativo consultará os repositórios oficiais e atualizará `yt-dlp`, `FFmpeg` ou `Deno` de forma atômica e segura.

---

## ⚠️ Limitações Conhecidas

- **Disponibilidade no YouTube:** O BaixALL depende da disponibilidade dos vídeos e da capacidade do motor `yt-dlp`. Vídeos privados, vídeos restritos por região geográfica, conteúdos com proteção comercial por DRM ou com exigência de login com verificação humana não são suportados. **Não há promessa de compatibilidade universal com todo e qualquer vídeo.**
- **Atualização das Ferramentas:** Caso uma URL apresente falha na análise, utilize a aba **Ferramentas** para atualizar o `yt-dlp` antes de relatar um problema.
- **Instabilidade de Conexão:** Quedas prolongadas de rede durante downloads de arquivos pesados podem exigir o reinício do download correspondente.

---

## 💬 Como Relatar Problemas e Obter Suporte

Agradecemos o seu feedback para manter o BaixALL confiável:

- **Encontrou um erro ou falha?** Abra um [Relato de Bug](https://github.com/ArkanjoV2/BaixALL/issues/new?template=bug_report.yml).
- **Tem uma ideia de melhoria?** Abra uma [Sugestão de Funcionalidade](https://github.com/ArkanjoV2/BaixALL/issues/new?template=feature_request.yml).
- **Dúvidas gerais de uso:** Consulte o nosso [Guia de Suporte](SUPPORT.md).
- **Relatório Privado de Vulnerabilidades:** Consulte a nossa política em [SECURITY.md](SECURITY.md).

---

## 📜 Licença e Créditos

- **BaixALL:** Distribuído sob a licença **MIT** (consulte o arquivo [LICENSE](LICENSE)).
- **Componentes de Terceiros:** Utiliza `yt-dlp` (Unlicense), `FFmpeg` / `ffprobe` (GPLv3), `Deno` (MIT) e bibliotecas .NET (MIT/Apache 2.0). Detalhes sobre limites de processo, compilação e conformidade legal com a GPLv3 estão formalmente documentados em [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

---

<p align="center">
  <sub>BaixALL — Desenvolvido de forma independente para arquivamento pessoal e interoperabilidade técnica.</sub>
</p>
