# Guia de Instalação e Uso do BaixALL

Este guia foi elaborado para orientar usuários no download, verificação, instalação e operação do **BaixALL** no Windows 10 e Windows 11 com segurança e transparência.

---

## 📋 Sumário
1. [Requisitos do Sistema Operacional](#1-requisitos-do-sistema-operacional)
2. [Baixar o Aplicativo Oficial](#2-baixar-o-aplicativo-oficial)
3. [Verificação de Integridade e Hashes SHA-256](#3-verificação-de-integridade-e-hashes-sha-256)
4. [Avisos de Reputação do Windows Defender SmartScreen](#4-avisos-de-reputação-do-windows-defender-smartscreen)
5. [Executar o Instalador ou Usar a Versão Portátil](#5-executar-o-instalador-ou-usar-a-versão-portátil)
6. [Primeira Execução e Instalação das Ferramentas](#6-primeira-execução-e-instalação-das-ferramentas)
7. [Como Realizar o Primeiro Download](#7-como-realizar-o-primeiro-download)
8. [Onde os Arquivos São Salvos e Como Alterar](#8-onde-os-arquivos-são-salvos-e-como-alterar)
9. [Como Desinstalar com Preservação dos seus Arquivos](#9-como-desinstalar-com-preservação-dos-seus-arquivos)

---

## 1. Requisitos do Sistema Operacional

- **Arquitetura:** Processador de 64 bits (x64 / AMD64).
- **Windows 11:** Totalmente suportado (sistema com ciclo de suporte oficial ativo pela Microsoft).
- **Windows 10:** Recomendado para compilações mantidas no ciclo de suporte oficial da Microsoft (como versões 22H2 dentro do período de atualização ou canais LTSC com suporte estendido).
- *Compatibilidade técnica vs. Suporte oficial:* Tecnicamente, o aplicativo foi compilado para .NET 10 Desktop (WPF) e requer uma versão mínima do Windows 10 (build 17763 / versão 1809) para execução das APIs básicas do sistema. No entanto, versões descontinuadas do Windows não recebem atualizações de segurança da Microsoft, cabendo ao usuário manter seu sistema operacional atualizado.
- **Runtimes:** Não é necessário instalar .NET SDK, Visual Studio, Python ou Node.js (a aplicação é autônoma e inclui o runtime necessário).
- **Espaço Livre em Disco:** Pelo menos 300 MB livres para a aplicação e ferramentas auxiliares, além do espaço livre necessário para as mídias baixadas.
- **Rede:** Conexão à internet ativa.

---

## 2. Baixar o Aplicativo Oficial

Obtenha sempre os arquivos a partir da página oficial de lançamentos no GitHub:

- **Instalador Oficial (Recomendado):**  
  [Baixar BaixALL-Setup-1.0.0.exe](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/BaixALL-Setup-1.0.0.exe)  
  *Assistente de instalação completo que cria atalhos no Menu Iniciar e na Área de Trabalho e registra desinstalador seguro.*

- **Pacote Portátil (.ZIP):**  
  [Baixar BaixALL-1.0.0-win-x64.zip](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/BaixALL-1.0.0-win-x64.zip)  
  *Não requer instalação no sistema. Basta descompactar o arquivo `.zip` e executar diretamente o arquivo `BaixALL.exe`.*

---

## 3. Verificação de Integridade e Hashes SHA-256

Antes de abrir qualquer executável baixado da internet, é uma boa prática de segurança verificar sua integridade.

1. Baixe o arquivo oficial de hashes: [checksums.txt](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.0.0/checksums.txt).
2. Abra o **PowerShell** na pasta onde o arquivo foi baixado e execute:
   ```powershell
   Get-FileHash BaixALL-Setup-1.0.0.exe -Algorithm SHA256
   ```
3. Compare o valor retornado com o hash oficial da versão 1.0.0:
   ```text
   aa8936531dc763cad319b29f5dcc7e317a7c97b5e7e7f72e761e0e4867179979  BaixALL-Setup-1.0.0.exe
   7e7c63be4d72b15e75361d3b6a06ddce2d88b34c55158207a9260b02888ff0dd  BaixALL-1.0.0-win-x64.zip
   ```
4. Se o hash calculado for idêntico, o arquivo está íntegro e não sofreu alterações durante a transferência.

---

## 4. Avisos de Reputação do Windows Defender SmartScreen

Ao executar um executável novo ou pouco comum no seu sistema, o **Windows Defender SmartScreen** pode apresentar um alerta com a mensagem:  
*"O Windows protegeu o seu computador"*.

### Como funciona o SmartScreen:
- O SmartScreen é um filtro de segurança da Microsoft que avalia a reputação de programas com base no volume estatístico de downloads benignos e telemetria acumulada ao longo do tempo.
- Quando um software de código aberto é lançado recentemente e disponibilizado de forma independente, os servidores da Microsoft ainda não possuem dados históricos suficientes sobre esse binário específico, o que desencadeia o aviso preventivo de reputação.
- **Importante sobre assinaturas digitais:** Mesmo aplicativos assinados por certificados comerciais passam por períodos de construção gradual de reputação no ecossistema Windows.

### Recomendações de segurança:
1. **Confirme a procedência:** Certifique-se de que o arquivo foi obtido exclusivamente a partir do repositório oficial: `https://github.com/ArkanjoV2/BaixALL/releases`.
2. **Confira o hash SHA-256:** Valide o arquivo contra o `checksums.txt` oficial.
3. **Não execute o arquivo se tiver dúvidas:** Caso não tenha certeza da origem do arquivo, não confirme a execução até verificar sua autenticidade.
4. **Nunca desabilite proteções:** Não desative o Windows Defender, o SmartScreen ou o antivírus do seu sistema operacional.
5. Se você conferiu a origem oficial e o hash SHA-256 e deseja prosseguir com a execução do instalador oficial, clique no link **"Mais informações"** na janela do alerta e avalie as informações apresentadas antes de decidir pela execução.

---

## 5. Executar o Instalador ou Usar a Versão Portátil

### Com o Instalador (`BaixALL-Setup-1.0.0.exe`):
1. Dê um duplo clique no arquivo baixado.
2. O assistente de instalação abrirá em Português do Brasil.
3. Escolha se deseja atalhos na Área de Trabalho e no Menu Iniciar.
4. O programa é instalado no perfil do usuário em `%LOCALAPPDATA%\Programs\BaixALL`, dispensando privilégios administrativos.
5. Ao finalizar, clique em Concluir para abrir o aplicativo.

### Com a Versão Portátil (`BaixALL-1.0.0-win-x64.zip`):
1. Clique com o botão direito no arquivo `.zip` e selecione **"Extrair Tudo..."**.
2. Abra a pasta resultante e dê um duplo clique no arquivo `BaixALL.exe`.

---

## 6. Primeira Execução e Instalação das Ferramentas

O BaixALL utiliza quatro ferramentas auxiliares de código aberto para análise e processamento de mídia: `yt-dlp`, `FFmpeg`, `ffprobe` e `Deno`.

- **Detecção Inicial:**  
  Na primeira inicialização, o aplicativo verifica se essas ferramentas estão instaladas. Se estiverem ausentes, uma mensagem na tela inicial exibirá o botão **"Instalar Ferramentas"**.
- **Download Seguro e Isolado:**  
  Ao clicar no botão, o BaixALL baixa automaticamente as versões oficiais mais recentes diretamente dos repositórios do GitHub para uma pasta isolada:  
  `%LOCALAPPDATA%\BaixALL\tools\`
- **Isolamento do Sistema:**  
  As ferramentas ficam restritas exclusivamente à pasta de dados do BaixALL. Elas não alteram as variáveis de ambiente (`PATH`) do Windows e não interferem em outros programas instalados no computador.

---

## 7. Como Realizar o Primeiro Download

1. Copie a URL do vídeo do YouTube no navegador.
2. Na aba **Downloader** do BaixALL, cole o link no campo de entrada.
3. Clique em **"Analisar"** (ou pressione a tecla `Enter`).
4. O BaixALL consultará os fluxos de mídia e exibirá a miniatura, o título e as opções disponíveis.
5. Selecione a opção desejada:
   - **Melhor qualidade disponível (Padrão):** O aplicativo seleciona a melhor combinação de vídeo e áudio e realiza a mesclagem automática via FFmpeg.
   - **Resolução personalizada:** Escolha opções como 1080p ou 4K com taxas de 60 FPS quando fornecidas pela plataforma.
   - **Somente Áudio:** Marque essa opção para extrair apenas a faixa sonora (em formato original M4A/Opus ou convertido para MP3 a 320 kbps).
6. Clique no botão **"Baixar Vídeo"** (ou **"Baixar Áudio"**). O item será adicionado à aba **Fila** e o progresso poderá ser acompanhado em tempo real.

---

## 8. Onde os Arquivos São Salvos e Como Alterar

- **Pasta Padrão:** Por padrão, todos os arquivos concluídos são salvos na pasta de **Downloads** do seu perfil de usuário do Windows:  
  `C:\Users\<SeuUsuario>\Downloads`
- **Como Alterar:**  
  1. No BaixALL, acesse a aba **Configurações** na barra lateral.
  2. Na seção *Pasta de Destino*, clique no botão **"Procurar Pasta"**.
  3. Escolha o novo diretório desejado e confirme. A preferência será salva para os próximos downloads.

---

## 9. Como Desinstalar com Preservação dos seus Arquivos

O BaixALL respeita integralmente os arquivos de mídia baixados pelo usuário:

1. Acesse o menu **Iniciar** > **Configurações** > **Aplicativos** > **Aplicativos Instalados**.
2. Localize **BaixALL** na lista e clique em **Desinstalar** (ou utilize o atalho de desinstalação no Menu Iniciar).
3. Conclua o assistente de desinstalação.
4. **Preservação de Dados:**  
   - O desinstalador remove exclusivamente os executáveis da aplicação em `%LOCALAPPDATA%\Programs\BaixALL`.
   - **Seus vídeos e áudios baixados na pasta Downloads NUNCA são apagados.**
   - O arquivo de histórico e configurações em `%LOCALAPPDATA%\BaixALL` é preservado, permitindo reinstalar o aplicativo futuramente sem perder suas preferências.
