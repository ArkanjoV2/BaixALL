# Guia de Instalação e Uso do BaixALL

Este guia foi elaborado para orientar usuários no download, verificação, instalação e operação do **BaixALL 1.3.0** no Windows 10 e Windows 11 com segurança, conformidade e transparência.

---

## 📋 Sumário
1. [Requisitos do Sistema Operacional](#1-requisitos-do-sistema-operacional)
2. [Baixar o Aplicativo Oficial](#2-baixar-o-aplicativo-oficial)
3. [Verificação de Integridade e Hashes SHA-256](#3-verificação-de-integridade-e-hashes-sha-256)
4. [Avisos de Reputação do Windows Defender SmartScreen](#4-avisos-de-reputação-do-windows-defender-smartscreen)
5. [Executar o Instalador ou Usar a Versão Portátil](#5-executar-o-instalador-ou-usar-a-versão-portátil)
6. [Primeira Execução e Instalação das Ferramentas](#6-primeira-execução-e-instalação-das-ferramentas)
7. [Como Analisar e Baixar Vídeos do YouTube](#7-como-analisar-e-baixar-vídeos-do-youtube)
8. [Como Baixar Reels e Vídeos do Instagram](#8-como-baixar-reels-e-vídeos-do-instagram)
9. [Como Selecionar Vídeos em Carrosséis do Instagram](#9-como-selecionar-vídeos-em-carrosséis-do-instagram)
10. [Como Baixar Vídeos e GIFs do X/Twitter](#10-como-baixar-vídeos-e-gifs-do-xtwitter)
11. [Como Utilizar a Fila e Acompanhar o Progresso](#11-como-utilizar-a-fila-e-acompanhar-o-progresso)
12. [Como Verificar os Arquivos Baixados e Acessar o Histórico](#12-como-verificar-os-arquivos-baixados-e-acessar-o-histórico)
13. [Onde os Arquivos São Salvos e Como Alterar](#13-onde-os-arquivos-são-salvos-e-como-alterar)
14. [Fechamento do Aplicativo e Gerenciamento da Fila](#14-fechamento-do-aplicativo-e-gerenciamento-da-fila)
15. [Como Desinstalar com Preservação dos seus Arquivos](#15-como-desinstalar-com-preservação-dos-seus-arquivos)

---

## 1. Requisitos do Sistema Operacional

- **Arquitetura:** Processador de 64 bits (x64 / AMD64).
- **Windows 11 (64-bit):** Totalmente suportado (sistema com ciclo de suporte oficial ativo pela Microsoft).
- **Windows 10 (64-bit):** Recomendado para compilações mantidas no ciclo de suporte oficial da Microsoft (como versões 22H2 dentro do período de atualização ou canais LTSC com suporte estendido).
- *Compatibilidade técnica:* O aplicativo foi compilado para .NET 10 Desktop (WPF) e requer uma versão mínima do Windows 10 (build 17763 / versão 1809) para execução das APIs básicas do sistema operacional.
- **Runtimes:** Não é necessário instalar .NET SDK, Visual Studio, Python ou Node.js (a aplicação é autônoma e inclui o runtime necessário).
- **Espaço Livre em Disco:** Mínimo de 300 MB livres para a aplicação e ferramentas auxiliares, além do espaço livre necessário para as mídias baixadas.
- **Rede:** Conexão à internet ativa.

---

## 2. Baixar o Aplicativo Oficial

Obtenha sempre os arquivos a partir da página oficial de lançamentos no GitHub:

- **Instalador Oficial (Recomendado):**  
  [Baixar BaixALL-Setup-1.3.0.exe](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.3.0/BaixALL-Setup-1.3.0.exe)  
  *Assistente de instalação completo que cria atalhos no Menu Iniciar e na Área de Trabalho e registra desinstalador seguro no painel do Windows.*

- **Pacote Portátil (.ZIP):**  
  [Baixar BaixALL-1.3.0-win-x64.zip](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.3.0/BaixALL-1.3.0-win-x64.zip)  
  *Não requer instalação no sistema. Basta descompactar o arquivo `.zip` e executar diretamente o arquivo `BaixALL.exe`.*

- **Manifesto de Hashes:**  
  [Baixar checksums-1.3.0.txt](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.3.0/checksums-1.3.0.txt)

---

## 3. Verificação de Integridade e Hashes SHA-256

Antes de abrir qualquer executável baixado da internet, é uma boa prática de segurança verificar sua integridade criptográfica.

1. Baixe o arquivo oficial de hashes: [checksums-1.3.0.txt](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.3.0/checksums-1.3.0.txt).
2. Abra o **PowerShell** na pasta onde o arquivo foi baixado e execute:
   ```powershell
   Get-FileHash BaixALL-Setup-1.3.0.exe -Algorithm SHA256
   ```
3. Compare o valor retornado com os hashes oficiais da versão 1.3.0:
   ```text
   b434214ae7b121347f034d0e0bbc74d708688b48d2bcce5e6eeb879b999985c8  BaixALL-Setup-1.3.0.exe
   2310927586a61f8155944f5a2636399fdba7cf24c0f52e5fbd534c02699d6b01  BaixALL-1.3.0-win-x64.zip
   1823e861154b9197c5529d41b3cca61619c3d57c3e3041ab6e4a745cdb6eb0d7  checksums-1.3.0.txt
   ```
4. Se o hash calculado for idêntico, o arquivo está íntegro e não sofreu alterações durante a transferência.

---

## 4. Avisos de Reputação do Windows Defender SmartScreen

Ao executar um executável novo no seu sistema, o **Windows Defender SmartScreen** pode apresentar um alerta preventivo:  
*"O Windows protegeu o seu computador"*.

### Como funciona o SmartScreen:
- O SmartScreen é um filtro da Microsoft baseado em reputação estatística de downloads benignos ao longo do tempo.
- Softwares de código aberto independentes recém-lançados ainda não acumularam histórico estatístico nos servidores da Microsoft, acionando o alerta preventivo.

### Recomendações de segurança:
1. **Confirme a procedência:** Certifique-se de que o arquivo foi obtido exclusivamente a partir do repositório oficial: `https://github.com/ArkanjoV2/BaixALL/releases/tag/v1.3.0`.
2. **Confira o hash SHA-256:** Valide o arquivo contra o `checksums-1.3.0.txt` oficial.
3. **Não execute o arquivo se tiver dúvidas:** Caso não tenha certeza da integridade do arquivo, não confirme a execução.
4. **Nunca desabilite proteções:** Não desative o Windows Defender, o SmartScreen ou o antivírus do seu sistema operacional.
5. Se você conferiu a origem oficial e o hash SHA-256 e deseja prosseguir com a instalação, clique em **"Mais informações"** na janela do alerta e selecione **"Executar assim mesmo"**.

---

## 5. Executar o Instalador ou Usar a Versão Portátil

### Com o Instalador (`BaixALL-Setup-1.3.0.exe`):
1. Dê um duplo clique no arquivo baixado.
2. O assistente de instalação abrirá em Português do Brasil.
3. Escolha se deseja criar atalhos na Área de Trabalho e no Menu Iniciar.
4. O programa é instalado no perfil do usuário em `%LOCALAPPDATA%\Programs\BaixALL`, dispensando privilégios administrativos.
5. Ao finalizar, clique em **Concluir** para abrir o aplicativo.

### Com a Versão Portátil (`BaixALL-1.3.0-win-x64.zip`):
1. Clique com o botão direito no arquivo `.zip` e selecione **"Extrair Tudo..."**.
2. Abra a pasta resultante e dê um duplo clique no arquivo `BaixALL.exe`.

---

## 6. Primeira Execução e Instalação das Ferramentas

O BaixALL utiliza componentes auxiliares de código aberto para análise e processamento de mídia: `yt-dlp`, `FFmpeg`, `ffprobe` e `Deno`.

- **Detecção Inicial:**  
  Na primeira inicialização, o aplicativo verifica a presença dessas ferramentas. Se estiverem ausentes, o badge superior alertará e o botão **"Instalar Ferramentas"** estará disponível.
- **Download Seguro e Isolado:**  
  Ao clicar no botão, o BaixALL baixa automaticamente as versões oficiais mais recentes diretamente dos repositórios do GitHub para uma pasta isolada:  
  `%LOCALAPPDATA%\BaixALL\tools\`
- **Isolamento do Sistema:**  
  As ferramentas ficam restritas exclusivamente à pasta de dados do BaixALL. Elas não alteram as variáveis de ambiente (`PATH`) do Windows e não interferem em outros softwares instalados.

---

## 7. Como Analisar e Baixar Vídeos do YouTube

1. Copie a URL do vídeo do YouTube no navegador.
2. Na tela inicial do BaixALL, cole o link no campo de entrada e clique em **"Analisar"** (ou pressione `Enter`).
3. O aplicativo exibirá título, canal, miniatura e a lista de resoluções disponíveis.
4. Selecione a resolução desejada ou utilize o padrão **"Melhor qualidade disponível"**.
5. Se desejar apenas a faixa sonora, marque a opção **"Baixar somente o áudio do vídeo"** e selecione o formato (MP3, M4A ou Opus).
6. Clique em **"Baixar Agora"**.

---

## 8. Como Baixar Reels e Vídeos do Instagram

> **Importante:** O usuário deve possuir autorização para acessar e baixar o conteúdo. Conteúdos públicos sem exigência de autenticação são suportados. Stories, contas privadas e conteúdos com restrição de idade estão fora do escopo.

1. Copie a URL do Reel (`https://www.instagram.com/reel/...`) ou da publicação de vídeo (`https://www.instagram.com/p/...`).
2. Cole no campo de entrada do BaixALL e clique em **"Analisar"**.
3. O aplicativo identifica automaticamente a plataforma exibindo o badge rosa `Instagram` e a miniatura correspondente.
4. Escolha a qualidade e o container desejados (o container *Automático* preserva o formato nativo da plataforma sem recodificação).
5. Clique em **"Baixar Agora"**. O item será adicionado à fila geral de downloads.

---

## 9. Como Selecionar Vídeos em Carrosséis do Instagram

Publicações do Instagram que combinam múltiplos vídeos e fotos em uma única postagem são tratadas de forma granular:

1. Cole o endereço do carrossel no BaixALL e clique em **"Analisar"**.
2. O aplicativo identifica a coleção com o badge `CARROSSEL DO INSTAGRAM`, calcula a contagem total de mídias e lista os vídeos encontrados.
3. Utilize as caixas de seleção ao lado de cada item para definir quais vídeos baixar, ou utilize os botões em lote:
   - **Selecionar Todos**
   - **Desmarcar Todos**
   - **Inverter Seleção**
4. *Fotos estáticas:* Imagens são automaticamente desabilitadas com aviso visual, pois o motor destina-se ao processamento de mídias em vídeo.
5. Selecione se deseja criar uma subpasta automática com a legenda da postagem e clique em **"Adicionar Vídeos Selecionados à Fila"**. O índice original de cada item é rigorosamente preservado na execução.

---

## 10. Como Baixar Vídeos e GIFs do X/Twitter

1. Copie a URL do post do X/Twitter (`https://x.com/.../status/...` ou `https://twitter.com/.../status/...`).
2. Cole no BaixALL e clique em **"Analisar"**. O aplicativo aplica o badge ciano `X / Twitter`.
3. Para publicações contendo vídeo ou animações GIF (que são entregues pela plataforma como fluxos de vídeo em MP4), o aplicativo detecta a maior resolução disponível.
4. Clique em **"Baixar Agora"**.
5. *Posts sem vídeo:* Publicações que contenham apenas texto puro ou fotos estáticas exibem uma mensagem explicativa informando que não foram detectadas mídias audiovisuais compatíveis.

---

## 11. Como Utilizar a Fila e Acompanhar o Progresso

- Acesse a aba **Fila de Downloads** no topo da janela.
- Acompanhe a barra de progresso, a velocidade de transferência (em MB/s), o tempo restante estimado (ETA) e o tamanho total do arquivo.
- Downloads em lote (playlists e carrosséis) exibem um painel superior consolidado com percentual honesto por quantidade de itens finalizados.
- **Concorrência:** Na aba **Configurações**, configure o limite de 1, 2 ou 3 downloads simultâneos conforme a capacidade da sua conexão.
- **Cancelamento:**  
  - Clique em **"Cancelar"** no card de um item ativo para cancelar apenas aquele download e apagar seus arquivos temporários.
  - Clique em **"Cancelar Lote"** no painel de uma coleção para interromper atomicamente todos os itens daquele conjunto.

---

## 12. Como Verificar os Arquivos Baixados e Acessar o Histórico

- Na aba **Histórico**, visualize o registro unificado das mídias concluídas, identificadas com seus respectivos badges coloridos (`YouTube`, `Instagram`, `X / Twitter`).
- Utilize os botões de ação:
  - **Abrir:** Reproduz a mídia no reprodutor padrão do Windows.
  - **Pasta:** Abre o Windows Explorer diretamente na pasta onde o arquivo foi gravado, com o item já em destaque.
  - **Remover (X):** Exclui o item do registro visual sem apagar o arquivo físico do computador.
- O botão **"Limpar Histórico"** limpa a listagem completa preservando integralmente os arquivos no disco.

---

## 13. Onde os Arquivos São Salvos e Como Alterar

- **Pasta Padrão:** Os arquivos são salvos na pasta de Downloads do usuário:  
  `C:\Users\<SeuUsuario>\Downloads`
- **Como Alterar:**  
  1. Acesse a aba **Configurações**.
  2. Na seção *Pasta de Destino*, clique em **"Procurar Pasta"**.
  3. Selecione o novo diretório. As alterações são gravadas em `%LOCALAPPDATA%\BaixALL\settings.json`.

---

## 14. Fechamento do Aplicativo e Gerenciamento da Fila

O BaixALL protege o usuário contra o encerramento acidental de tarefas ativas:

- **Diálogo de Confirmação Preventivo:**  
  Se você tentar fechar o aplicativo enquanto houver downloads ativos ou na fila, uma janela de confirmação alertará:
  - **Não:** Cancela o fechamento; a janela permanece aberta e os downloads continuam normalmente.
  - **Sim:** Cancela os downloads em andamento, encerra os processos auxiliares de forma limpa e fecha o programa.
- **Sem Downloads Ativos:** O aplicativo encerra imediatamente ao ser fechado.
- **Limitação de Persistência:** A fila de downloads não é persistida em disco entre reinicializações. Itens que não tenham sido concluídos deverão ser reenfileirados manualmente caso a aplicação seja fechada. O histórico de conclusões e as configurações do usuário permanecem totalmente preservados.

---

## 15. Como Desinstalar com Preservação dos seus Arquivos

1. Acesse **Configurações do Windows** > **Aplicativos** > **Aplicativos Instalados**.
2. Localize **BaixALL** e clique em **Desinstalar** (ou utilize o atalho de desinstalação no Menu Iniciar).
3. Conclua o assistente.
4. **Preservação dos Seus Arquivos:**  
   - O desinstalador remove exclusivamente os executáveis da aplicação em `%LOCALAPPDATA%\Programs\BaixALL`.
   - **Nenhum arquivo baixado na sua pasta de Downloads é excluído.**
   - O histórico e as configurações em `%LOCALAPPDATA%\BaixALL` são preservados, permitindo reinstalar o aplicativo no futuro sem perder preferências.
