# Guia de Instalação e Uso do BaixALL

Este guia foi elaborado para ajudar usuários de todos os níveis a baixar, instalar e utilizar o **BaixALL** no Windows 10 e Windows 11 com facilidade e segurança.

---

## 📋 Sumário
1. [Baixar o Aplicativo](#1-baixar-o-aplicativo)
2. [Executar o Instalador](#2-executar-o-instalador)
3. [Avisos de Reputação do Windows SmartScreen](#3-avisos-de-reputação-do-windows-smartscreen)
4. [Primeira Execução e Instalação das Ferramentas](#4-primeira-execução-e-instalação-das-ferramentas)
5. [Como Realizar o Primeiro Download](#5-como-realizar-o-primeiro-download)
6. [Onde os Arquivos São Salvos](#6-onde-os-arquivos-são-salvos)
7. [Como Desinstalar sem Perder seus Vídeos](#7-como-desinstalar-sem-perder-seus-vídeos)

---

## 1. Baixar o Aplicativo

O BaixALL é distribuído oficialmente através do GitHub:

- **Instalador Recomendado (Setup):**  
  [Baixar BaixALL-Setup-1.0.0.exe](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/BaixALL-Setup-1.0.0.exe)  
  *Cria atalhos na Área de Trabalho e no Menu Iniciar e inclui assistente de desinstalação seguro.*

- **Versão Portátil (ZIP):**  
  [Baixar BaixALL-1.0.0-win-x64.zip](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/BaixALL-1.0.0-win-x64.zip)  
  *Não requer instalação. Basta descompactar o arquivo `.zip` e abrir o executável `BaixALL.exe` diretamente.*

- **Verificação de Integridade (Hashes):**  
  Você pode conferir a integridade dos arquivos baixados comparando o hash SHA-256 com os valores publicados em [checksums.txt](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/checksums.txt).

---

## 2. Executar o Instalador

1. Localize o arquivo `BaixALL-Setup-1.0.0.exe` na sua pasta de downloads e dê um duplo clique para abrir.
2. O assistente de instalação abrirá em Português do Brasil.
3. Escolha se deseja criar um atalho na **Área de Trabalho** e no **Menu Iniciar**.
4. A instalação é realizada na pasta do usuário (`%LOCALAPPDATA%\Programs\BaixALL`) e não exige privilégios de administrador.
5. Ao concluir, marque a opção para iniciar o aplicativo ou clique no atalho criado.

---

## 3. Avisos de Reputação do Windows SmartScreen

Ao abrir o instalador pela primeira vez, o Windows Defender SmartScreen poderá exibir uma janela com o título:  
*"O Windows protegeu o seu computador"*.

### Por que esse aviso aparece?
- O SmartScreen da Microsoft avalia a reputação de programas com base no volume e histórico estatístico de downloads benignos ao longo do tempo.
- Por ser um projeto novo de código aberto recém-lançado, o aplicativo ainda está no período inicial de construção de reputação nos servidores da Microsoft.
- O aviso de reputação **não significa que o arquivo seja malicioso**, mas sim que o sistema ainda não possui telemetria suficiente acumulada sobre este binário específico.
- Mesmo programas assinados digitalmente por certificados comerciais podem exibir alertas durante as primeiras semanas de lançamento até acumularem histórico.

### O que fazer:
1. Verifique sempre se você baixou o instalador diretamente da página oficial do projeto: `https://github.com/ArkanjoV2/BaixALL/releases`.
2. Compare o hash SHA-256 se desejar certificar-se de que o arquivo não foi alterado.
3. Na janela do SmartScreen, clique no link **"Mais informações"**.
4. Em seguida, clique no botão **"Executar assim mesmo"**.

> ⚠️ **Importante:**  
> **Nunca desabilite o Windows Defender ou o SmartScreen.** Esses recursos de segurança são fundamentais para a proteção do seu sistema. Caso tenha dúvidas sobre a origem de qualquer arquivo, não o execute até confirmar sua procedência oficial.

---

## 4. Primeira Execução e Instalação das Ferramentas

Para analisar mídias e mesclar fluxos de alta fidelidade, o BaixALL utiliza quatro ferramentas de apoio: `yt-dlp`, `FFmpeg`, `ffprobe` e `Deno`.

- **Instalação com 1 Clique:**  
  Na primeira vez que você abrir o BaixALL, se as ferramentas ainda não estiverem presentes, uma mensagem na tela inicial solicitará a instalação.  
  Basta clicar no botão **"Instalar Ferramentas"**.
- **Onde ficam guardadas:**  
  O BaixALL baixa as versões oficiais diretamente do GitHub e as armazena em uma pasta isolada:  
  `%LOCALAPPDATA%\BaixALL\tools\`
- **Sem bagunçar seu computador:**  
  As ferramentas ficam restritas ao BaixALL. Elas não são adicionadas ao `PATH` do sistema e não interferem em nenhum outro programa instalado no seu Windows.

---

## 5. Como Realizar o Primeiro Download

1. Abra o YouTube no seu navegador e copie a URL (link) do vídeo desejado.
2. No BaixALL, na aba inicial **Downloader**, cole o link no campo de entrada.
3. Clique em **"Analisar"** (ou pressione a tecla `Enter`).
4. O aplicativo buscará os dados do vídeo e mostrará o título, miniatura, duração e as opções disponíveis.
5. Escolha a configuração desejada:
   - **Melhor qualidade disponível (Recomendado):** Seleciona o melhor vídeo e áudio e realiza a mesclagem automática.
   - **Resolução específica:** Escolha resoluções como 1080p, 1440p ou 4K, verificando a taxa de 60 FPS quando disponível.
   - **Somente Áudio:** Marque essa opção se desejar apenas o áudio (em formato original M4A/Opus ou convertido para MP3 a 320 kbps).
6. Clique no botão **"Baixar Vídeo"** (ou **"Baixar Áudio"**).
7. O item será adicionado à aba **Fila** e o download iniciará imediatamente.

---

## 6. Onde os Arquivos São Salvos

- Por padrão, todos os vídeos e áudios concluídos são gravados na pasta padrão de **Downloads** do seu perfil do Windows:  
  `C:\Users\<SeuUsuario>\Downloads`
- **Como alterar a pasta de destino:**  
  1. No BaixALL, clique na aba **Configurações** na barra lateral.
  2. Na seção *Pasta de Destino*, clique no botão **"Procurar Pasta"**.
  3. Selecione a pasta de sua preferência e confirme. A nova pasta será lembrada para os próximos downloads.

---

## 7. Como Desinstalar sem Perder seus Vídeos

O BaixALL foi projetado para respeitar rigorosamente os seus arquivos pessoais:

1. Abra o menu **Iniciar** > **Configurações** > **Aplicativos** > **Aplicativos Instalados**.
2. Localize o **BaixALL** na lista e clique nos três pontos `...` > **Desinstalar** (ou utilize o atalho de desinstalação criado no Menu Iniciar).
3. Siga as instruções do assistente de desinstalação.
4. **Garantia de Preservação:**  
   - O desinstalador remove exclusivamente os executáveis do programa em `%LOCALAPPDATA%\Programs\BaixALL`.
   - **Seus vídeos e músicas baixados na pasta Downloads NUNCA são apagados.**
   - O seu histórico e preferências em `%LOCALAPPDATA%\BaixALL` permanecem preservados para que você não perca suas configurações caso decida reinstalar o programa futuramente.
