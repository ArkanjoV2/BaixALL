# Guia de Instalação e Uso do BaixALL

Este guia foi elaborado para orientar usuários no download, verificação, instalação e operação do **BaixALL** (incluindo as novidades da versão **1.4.0** em preparação e os pacotes publicados da versão estável **1.3.1**) no Windows 10 e Windows 11 com segurança, conformidade e transparência.

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
11. [Como Analisar e Baixar Vídeos do TikTok (v1.4.0)](#11-como-analisar-e-baixar-vídeos-do-tiktok-v140)
12. [Como Utilizar a Fila e Acompanhar o Progresso](#12-como-utilizar-a-fila-e-acompanhar-o-progresso)
13. [Como Verificar os Arquivos Baixados e Acessar o Histórico](#13-como-verificar-os-arquivos-baixados-e-acessar-o-histórico)
14. [Onde os Arquivos São Salvos e Como Alterar](#14-onde-os-arquivos-são-salvos-e-como-alterar)
15. [Fechamento do Aplicativo e Gerenciamento da Fila](#15-fechamento-do-aplicativo-e-gerenciamento-da-fila)
16. [Como Desinstalar com Preservação dos seus Arquivos](#16-como-desinstalar-com-preservação-dos-seus-arquivos)

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

Obtenha sempre os arquivos exclusivamente a partir da [página oficial de Releases no GitHub](https://github.com/ArkanjoV2/BaixALL/releases/latest).

> ⚠️ **Nota sobre as versões 1.4.0 e 1.3.1:**  
> A versão **1.4.0** (que adiciona suporte ao **TikTok**) está em fase final de preparação e validação. Enquanto a Release `v1.4.0` ainda não estiver publicada no GitHub Releases, os links diretos de download abaixo referem-se à última versão estável publicada (**1.3.1**, compatível com YouTube, Instagram e X/Twitter, sem suporte ao TikTok). Assim que a Release `v1.4.0` for publicada oficialmente, seus pacotes (`BaixALL-Setup-1.4.0.exe`, `BaixALL-1.4.0-win-x64.zip` e `checksums-1.4.0.txt`) ficarão disponíveis em `https://github.com/ArkanjoV2/BaixALL/releases/latest`.

### Pacotes da Última Versão Estável Publicada (v1.3.1):

- **Página Oficial da Última Release:**  
  [Acessar Releases Oficiais do BaixALL](https://github.com/ArkanjoV2/BaixALL/releases/latest)

- **Instalador Oficial Publicado (v1.3.1 — Recomendado):**  
  [Baixar BaixALL-Setup-1.3.1.exe](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.3.1/BaixALL-Setup-1.3.1.exe)  
  *Assistente de instalação completo que cria atalhos no Menu Iniciar e na Área de Trabalho e registra desinstalador seguro no painel do Windows (versão 1.3.1).*

- **Pacote Portátil Publicado (v1.3.1 — .ZIP):**  
  [Baixar BaixALL-1.3.1-win-x64.zip](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.3.1/BaixALL-1.3.1-win-x64.zip)  
  *Não requer instalação no sistema. Basta descompactar o arquivo `.zip` e executar diretamente o arquivo `BaixALL.exe` (versão 1.3.1).*

- **Manifesto de Hashes Publicado (v1.3.1):**  
  [Baixar checksums-1.3.1.txt](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.3.1/checksums-1.3.1.txt)

---

## 3. Verificação de Integridade e Hashes SHA-256

Antes de abrir qualquer executável baixado da internet, é uma boa prática de segurança verificar sua integridade criptográfica contra o manifesto oficial da respectiva versão.

1. Baixe o arquivo oficial de hashes correspondente à release publicada (atualmente: [checksums-1.3.1.txt](https://github.com/ArkanjoV2/BaixALL/releases/download/v1.3.1/checksums-1.3.1.txt)).
2. Abra o **PowerShell** na pasta onde o arquivo foi baixado e execute (substituindo pelo nome do pacote baixado, como `BaixALL-Setup-1.3.1.exe` ou, quando publicado, `BaixALL-Setup-1.4.0.exe`):
   ```powershell
   Get-FileHash BaixALL-Setup-1.3.1.exe -Algorithm SHA256
   ```
3. Para a versão estável publicada **1.3.1**, compare o valor retornado com os hashes oficiais:
   ```text
   9a6e02f1584decb3310ad6f6e9154db07562b87b3313554b277aa393b4fc7a55  BaixALL-Setup-1.3.1.exe
   501cd5f67af74005eb676a11097f4ec5eabe5837ab61b15b73b51ef61563295f  BaixALL-1.3.1-win-x64.zip
   cc25adde56857a40dd05729347a11842cbc6166c8003f1bce0305e43da7b3df8  checksums-1.3.1.txt
   ```
4. Se o hash calculado for idêntico ao publicado na Release oficial, o arquivo está íntegro e não sofreu alterações durante a transferência.

---

## 4. Avisos de Reputação do Windows Defender SmartScreen

Ao executar um executável novo no seu sistema, o **Windows Defender SmartScreen** pode apresentar um alerta preventivo:  
*"O Windows protegeu o seu computador"*.

### Como funciona o SmartScreen:
- O SmartScreen é um filtro da Microsoft baseado em reputação estatística de downloads benignos ao longo do tempo.
- Softwares de código aberto independentes recém-lançados ainda não acumularam histórico estatístico nos servidores da Microsoft, acionando o alerta preventivo.

### Recomendações de segurança:
1. **Confirme a procedência:** Certifique-se de que o arquivo foi obtido exclusivamente a partir do repositório oficial: `https://github.com/ArkanjoV2/BaixALL/releases`.
2. **Confira o hash SHA-256:** Valide o arquivo contra o manifesto `checksums-<versão>.txt` oficial anexado à Release.
3. **Não execute o arquivo se tiver dúvidas:** Caso não tenha certeza da integridade do arquivo, não confirme a execução.
4. **Nunca desabilite proteções:** Não desative o Windows Defender, o SmartScreen ou o antivírus do seu sistema operacional.
5. Se você conferiu a origem oficial e o hash SHA-256 e deseja prosseguir com a instalação, clique em **"Mais informações"** na janela do alerta e selecione **"Executar assim mesmo"**.

---

## 5. Executar o Instalador ou Usar a Versão Portátil

### Com o Instalador (`BaixALL-Setup-<versão>.exe`):
1. Dê um duplo clique no arquivo do instalador baixado (`BaixALL-Setup-1.3.1.exe` na versão publicada atual ou `BaixALL-Setup-1.4.0.exe` na versão 1.4.0).
2. O assistente de instalação abrirá em Português do Brasil.
3. Escolha se deseja criar atalhos na Área de Trabalho e no Menu Iniciar.
4. O programa é instalado no perfil do usuário em `%LOCALAPPDATA%\Programs\BaixALL`, dispensando privilégios administrativos.
5. Ao finalizar, clique em **Concluir** para abrir o aplicativo.

### Com a Versão Portátil (`BaixALL-<versão>-win-x64.zip`):
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

## 11. Como Analisar e Baixar Vídeos do TikTok (v1.4.0)

> **Nota:** Esta funcionalidade integra o **BaixALL 1.4.0**. Certifique-se de estar executando a versão `1.4.0` (visível em *Configurações > Sobre*) e com o `yt-dlp` atualizado na aba *Ferramentas*.

1. Copie a URL de um vídeo público do TikTok:
   - Links canônicos: `https://www.tiktok.com/@usuario/video/ID` ou `https://m.tiktok.com/v/ID.html`
   - Links curtos compartilhados: `https://vm.tiktok.com/...`, `https://vt.tiktok.com/...` ou `https://www.tiktok.com/t/...`
2. Cole no campo de entrada do BaixALL e clique em **"Analisar"**. O aplicativo identifica automaticamente a plataforma exibindo o badge turquesa `TikTok`, resolve links curtos para o identificador único do vídeo (`tt:ID`) e apresenta o criador (`@usuario`), duração, resolução máxima e o indicador de marca-d'água.
3. **Verifique a classificação de marca-d'água exibida pelo aplicativo:**
   - **`NoWatermark` (`✓ Sem marca-d'água`):** Fonte de reprodução identificada positivamente como limpa (`play_addr`, fluxos de playback `bytevc1_*` / `h264_*` verificados). A melhor opção sem marca-d'água vem selecionada por padrão e o download utiliza exclusivamente os `format_id` exatos classificados como limpos.
   - **`Unknown` (`• Marca-d'água não verificada`):** Os metadados do vídeo não permitem confirmar se o fluxo contém ou não marca-d'água. O BaixALL não promete vídeo limpo e não seleciona a qualidade automaticamente; selecione manualmente a opção desejada no menu caso queira baixar.
   - **`Watermarked` (`⚠ Apenas com marca-d'água`):** Nenhuma fonte limpa foi disponibilizada pela plataforma, existindo apenas fluxos explicitamente identificados com marca-d'água. Nenhuma opção é selecionada automaticamente e o download exige que você marque a caixa **"Autorizo baixar a versão com marca-d'água"**.
4. Clique em **"Baixar Agora"** (ou marque **"Baixar somente o áudio do vídeo"** para extrair MP3, M4A ou Opus).
5. **Proteção contra fallback silencioso:** Se uma fonte limpa selecionada deixar de estar disponível no momento da execução do download, o BaixALL interrompe a tarefa com aviso claro e **jamais** baixa silenciosamente um formato não verificado ou com marca-d'água no lugar.
6. *Escopos não suportados:* Publicações de fotos/slideshows (`/photo/`), transmissões ao vivo (`/live`), Stories (`/story/`) e perfis inteiros não são suportados e exibem aviso em português.

---

## 12. Como Utilizar a Fila e Acompanhar o Progresso

- Acesse a aba **Fila de Downloads** no topo da janela.
- Acompanhe a barra de progresso, a velocidade de transferência (em MB/s), o tempo restante estimado (ETA) e o tamanho total do arquivo.
- Downloads em lote (playlists e carrosséis) exibem um painel superior consolidado com percentual honesto por quantidade de itens finalizados.
- **Concorrência:** Na aba **Configurações**, configure o limite de 1, 2 ou 3 downloads simultâneos conforme a capacidade da sua conexão.
- **Cancelamento:**  
  - Clique em **"Cancelar"** no card de um item ativo para cancelar apenas aquele download e apagar seus arquivos temporários.
  - Clique em **"Cancelar Lote"** no painel de uma coleção para interromper atomicamente todos os itens daquele conjunto.

---

## 13. Como Verificar os Arquivos Baixados e Acessar o Histórico

- Na aba **Histórico**, visualize o registro unificado das mídias concluídas, identificadas com seus respectivos badges coloridos (`YouTube`, `Instagram`, `X / Twitter`, `TikTok`).
- Utilize os botões de ação:
  - **Abrir:** Reproduz a mídia no reprodutor padrão do Windows.
  - **Pasta:** Abre o Windows Explorer diretamente na pasta onde o arquivo foi gravado, com o item já em destaque.
  - **Remover (X):** Exclui o item do registro visual sem apagar o arquivo físico do computador.
- O botão **"Limpar Histórico"** limpa a listagem completa preservando integralmente os arquivos no disco.

---

## 14. Onde os Arquivos São Salvos e Como Alterar

- **Pasta Padrão:** Os arquivos são salvos na pasta de Downloads do usuário:  
  `C:\Users\<SeuUsuario>\Downloads`
- **Como Alterar:**  
  1. Acesse a aba **Configurações**.
  2. Na seção *Pasta de Destino*, clique em **"Procurar Pasta"**.
  3. Selecione o novo diretório. As alterações são gravadas em `%LOCALAPPDATA%\BaixALL\settings.json`.

---

## 15. Fechamento do Aplicativo e Gerenciamento da Fila

O BaixALL protege o usuário contra o encerramento acidental de tarefas ativas:

- **Diálogo de Confirmação Preventivo:**  
  Se você tentar fechar o aplicativo enquanto houver downloads ativos ou na fila, uma janela de confirmação alertará:
  - **Não:** Cancela o fechamento; a janela permanece aberta e os downloads continuam normalmente.
  - **Sim:** Cancela os downloads em andamento, encerra os processos auxiliares de forma limpa e fecha o programa.
- **Sem Downloads Ativos:** O aplicativo encerra imediatamente ao ser fechado.
- **Limitação de Persistência:** A fila de downloads não é persistida em disco entre reinicializações. Itens que não tenham sido concluídos deverão ser reenfileirados manualmente caso a aplicação seja fechada. O histórico de conclusões e as configurações do usuário permanecem totalmente preservados.

---

## 16. Como Desinstalar com Preservação dos seus Arquivos

1. Acesse **Configurações do Windows** > **Aplicativos** > **Aplicativos Instalados**.
2. Localize **BaixALL** e clique em **Desinstalar** (ou utilize o atalho de desinstalação no Menu Iniciar).
3. Conclua o assistente.
4. **Preservação dos Seus Arquivos:**  
   - O desinstalador remove exclusivamente os executáveis da aplicação em `%LOCALAPPDATA%\Programs\BaixALL`.
   - **Nenhum arquivo baixado na sua pasta de Downloads é excluído.**
   - O histórico e as configurações em `%LOCALAPPDATA%\BaixALL` são preservados, permitindo reinstalar o aplicativo no futuro sem perder preferências.
