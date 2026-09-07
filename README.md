# BaixALL — Aplicativo Desktop para Download de Vídeos

**BaixALL** é um aplicativo desktop nativo para Windows desenvolvido em **C#**, **.NET 10** e **WPF** com o padrão arquitetural **MVVM**. Ele foi projetado para permitir o download simples, rápido e seguro de vídeos e áudios do YouTube, utilizando **yt-dlp**, **FFmpeg** e **Deno** nos bastidores, com interface moderna estilo Fluent Design (Windows 11) e privacidade absoluta (100% local, sem telemetria, sem anúncios e sem comunicação com servidores externos além dos canais oficiais de mídia e ferramentas).

---

## 📋 Índice

1. [Requisitos do Sistema](#-requisitos-do-sistema)
2. [Instalação](#-instalação)
3. [Primeiros Passos e Primeiro Download](#-primeiros-passos-e-primeiro-download)
4. [Seleção de Qualidade e Formatos](#-seleção-de-qualidade-e-formatos)
5. [Modo Somente Áudio](#-modo-somente-áudio)
6. [Pasta de Destino](#-pasta-de-destino)
7. [Fila de Downloads e Concorrência](#-fila-de-downloads-e-concorrência)
8. [Histórico de Downloads](#-histórico-de-downloads)
9. [Atualização das Ferramentas](#-atualização-das-ferramentas)
10. [Desinstalação e Preservação de Dados](#-desinstalação-e-preservação-de-dados)
11. [Segurança, SmartScreen e Assinatura Digital](#-segurança-smartscreen-e-assinatura-digital)
12. [Limitações Conhecidas](#-limitações-conhecidas)
13. [Aviso Legal e Direitos Autorais](#-aviso-legal-e-direitos-autorais)
14. [Licenças de Terceiros](#-licenças-de-terceiros)

---

## 💻 Requisitos do Sistema

- **Sistema Operacional:** Windows 10 (versão 1809 ou superior) ou Windows 11.
- **Arquitetura:** 64-bit (x64 / AMD64).
- **Runtimes:** Nenhum runtime adicional é necessário. O BaixALL é distribuído em modo *self-contained* (autônomo), não exigindo instalação de .NET SDK, Visual Studio, Python ou Node.js.
- **Conexão:** Acesso à internet para análise de links e downloads.

---

## 🚀 Instalação

O BaixALL está disponível em dois formatos de distribuição:

### 1. Instalador Oficial (`BaixALL-Setup-1.0.0-rc.2.exe`)
1. Baixe o executável de instalação.
2. Execute o assistente de instalação. Você pode optar por criar um atalho na Área de Trabalho e no Menu Iniciar.
3. Não são necessários privilégios de administrador obrigatórios para a instalação padrão no perfil do usuário (`%LOCALAPPDATA%\Programs\BaixALL` ou `Program Files`).

### 2. Pacote Portátil (`BaixALL-1.0.0-rc.2-win-x64.zip`)
1. Baixe o arquivo compactado `.zip`.
2. Extraia o conteúdo para a pasta de sua preferência.
3. Execute diretamente o arquivo `BaixALL.exe`.

---

## 🎬 Primeiros Passos e Primeiro Download

1. **Primeira Execução:** Na primeira abertura, o BaixALL verifica a presença das ferramentas de apoio (`yt-dlp`, `FFmpeg`, `ffprobe` e `Deno`). Se alguma estiver ausente, o gerenciador exibirá uma interface clara para baixá-las automaticamente com apenas um clique.
2. **Análise do Link:**
   - Copie a URL do vídeo do YouTube no navegador.
   - Cole o link no campo de entrada no BaixALL (o aplicativo suporta colagem automática ao focar o campo).
   - Clique em **"Analisar"** (ou pressione Enter).
   - O aplicativo carregará o título, canal, duração e a miniatura em alta resolução do vídeo.
3. **Iniciar Download:**
   - Escolha a qualidade desejada ou marque "Somente Áudio".
   - Clique no botão principal **"Baixar Agora"**.
   - O vídeo será enviado para a aba **Fila** e o download iniciará imediatamente.

---

## 🎥 Seleção de Qualidade e Formatos

O BaixALL analisa todas as faixas de vídeo e áudio disponibilizadas pelo YouTube e oferece opções inteligentes:

- **"Melhor qualidade disponível" (Recomendado):** Seleciona automaticamente o fluxo de vídeo de maior resolução e taxa de quadros (como 4K a 60 FPS ou 1080p a 60 FPS) e a melhor faixa de áudio, mesclando-os com o FFmpeg sem perda de qualidade.
- **Resoluções Específicas:** Você pode selecionar resoluções exatas (ex: 2160p 4K, 1440p 2K, 1080p Full HD, 720p HD, 480p, etc.) com indicação precisa da taxa de quadros (60 FPS ou 30 FPS).
- **Formato/Container:** Opções de saída em **MP4** ou **MKV**.

---

## 🎵 Modo Somente Áudio

Se você deseja apenas a faixa sonora do vídeo (músicas, podcasts, aulas):

1. Marque a caixa de seleção **"Somente Áudio"** na tela inicial.
2. Selecione o formato desejado no menu de containers:
   - **Melhor áudio disponível (Original):** Mantém a faixa nativa sem nenhuma recodificação (geralmente M4A/AAC ou Opus em WebM).
   - **M4A:** Container com codec AAC de alta compatibilidade para reprodutores portáteis e dispositivos Apple.
   - **Opus:** Codec aberto de última geração com alta fidelidade e compressão eficiente.
   - **MP3:** Converte o áudio via FFmpeg em alta qualidade (bitrate variável ou 320 kbps), compatível com qualquer dispositivo.

---

## 📂 Pasta de Destino

- Por padrão, os arquivos concluídos são salvos na sua pasta padrão de **Downloads** do Windows (`%USERPROFILE%\Downloads`).
- Você pode alterar a pasta de destino padrão na aba **Configurações** clicando em **"Procurar Pasta"**.
- O BaixALL sanitiza automaticamente o nome dos arquivos para evitar caracteres proibidos no Windows (`:`, `*`, `?`, `"`, `<`, `>`, `|`, etc.).

---

## ⚡ Fila de Downloads e Concorrência

O BaixALL conta com um motor de fila assíncrono projetado para máxima eficiência:

- **Adição Contínua:** Você pode analisar e enfileirar múltiplos vídeos sucessivamente sem esperar os anteriores terminarem.
- **Limite de Downloads Simultâneos:** Na aba **Configurações**, configure a concorrência desejada:
  - **1 download simultâneo (Padrão):** Ideal para conexões limitadas. Executa um item por vez; os demais aguardam.
  - **2 downloads simultâneos:** Equilíbrio ideal entre velocidade e uso de banda.
  - **3 downloads simultâneos:** Para conexões de alta velocidade.
- **Transição Automática:** Quando um download termina, o próximo item com status *Aguardando* inicia imediatamente.
- **Cancelamento Individual:** Ao cancelar um download em andamento, apenas aquele processo é encerrado de forma limpa; os demais continuam normalmente e a vaga liberada é assumida pelo próximo item da fila.

---

## 📖 Histórico de Downloads

- Acesse a aba **Histórico** na barra lateral.
- Todos os downloads concluídos ficam registrados de forma persistente com título, duração, formato, tamanho final e data.
- **Ações Rápidas:**
  - **Abrir arquivo:** Reproduz o vídeo/áudio no player padrão do Windows.
  - **Abrir pasta:** Abre o Windows Explorer com o arquivo selecionado em destaque.
  - **Limpar Histórico:** Remove o registro visual do histórico sem apagar os arquivos físicos do disco.

---

## 🔧 Atualização das Ferramentas

O YouTube atualiza frequentemente seus protocolos e assinaturas de streaming. Para garantir funcionamento contínuo:

1. Acesse a aba **Ferramentas** ou **Configurações**.
2. Clique no botão **"Verificar Atualizações"**.
3. Se houver uma versão mais recente do `yt-dlp`, `FFmpeg` ou `Deno`, o BaixALL realizará a atualização com substituição atômica e segura.

> [!NOTE]
> Por segurança e para evitar corrupção de arquivos em uso, o BaixALL impede a atualização das ferramentas enquanto houver downloads ativos em execução.

---

## 🗑️ Desinstalação e Preservação de Dados

Você pode desinstalar o BaixALL a qualquer momento com total segurança:

1. Abra o menu **Iniciar** > **Configurações** > **Aplicativos** > **Aplicativos Instalados**.
2. Localize **BaixALL** e clique em **Desinstalar** (ou utilize o atalho de desinstalação na pasta do Menu Iniciar).
3. **Preservação de Dados:**
   - **Seus vídeos baixados NUNCA são excluídos.** A desinstalação remove apenas os arquivos do programa.
   - Suas preferências e histórico em `%LOCALAPPDATA%\BaixALL` são mantidos intactos, permitindo que você reinstale o aplicativo futuramente sem perder suas configurações.

---

## 🛡️ Segurança, SmartScreen e Assinatura Digital

- **Aviso do Windows SmartScreen na Primeira Execução:**
  Ao executar um instalador de código aberto recém-compilado, o Windows Defender SmartScreen pode exibir o diálogo *"O Windows protegeu o seu computador"*, informando que se trata de um aplicativo não comumente baixado.
  - Para prosseguir, clique em **"Mais informações"** e em seguida no botão **"Executar assim mesmo"**.
- **Distinção entre Assinatura Digital e Reputação do SmartScreen:**
  - A **Assinatura Digital (Authenticode)** atesta a identidade do desenvolvedor e garante que o binário não sofreu alterações maliciosas após a compilação.
  - A **Reputação do SmartScreen**, por sua vez, é um sistema autônomo baseado no volume e histórico de downloads benignos ao longo do tempo no ecossistema Windows. Mesmo softwares assinados digitalmente passam por um período inicial de acúmulo de reputação.
- **Compromisso de Segurança:**
  - O BaixALL **nunca desabilita** o Windows Defender, o SmartScreen ou quaisquer proteções nativas do sistema operacional.
  - Não são utilizados certificados autoassinados fictícios que simulem falsamente uma autoridade certificadora pública.
  - A integração de um certificado comercial Authenticode formal é considerada uma pendência opcional de distribuição para futuras versões de ampla escala pública.

---

## ⚠️ Limitações Conhecidas

- **Vídeos Privados ou Restritos:** O BaixALL opera sem login do usuário por razões de privacidade. Portanto, vídeos marcados como privados ou que exijam login com verificação de idade rigorosa do YouTube podem não ser passíveis de download.
- **Vídeos com DRM Comercial:** Vídeos protegidos por sistemas de gerenciamento de direitos digitais (DRM) não são suportados.
- **Instabilidade na Conexão:** Downloads interrompidos por perda prolongada de sinal de rede podem necessitar de reinício.
- **Não promessa de suporte universal:** O YouTube pode alterar seus algoritmos sem aviso prévio. Caso um vídeo falhe na análise, atualize o `yt-dlp` na aba Ferramentas.

---

## ⚖️ Aviso Legal e Direitos Autorais

O **BaixALL** é uma ferramenta de software desenvolvida para fins educacionais, arquivamento pessoal e interoperabilidade técnica.

> [!IMPORTANT]
> O usuário é o único responsável pelo uso que faz do aplicativo. Certifique-se de baixar apenas vídeos e conteúdos sobre os quais você detenha os direitos autorais, que estejam em **Domínio Público**, disponibilizados sob licenças abertas (como **Creative Commons**) ou para os quais você possua autorização expressa do titular dos direitos. Não utilize o aplicativo para violar termos de serviço ou leis de propriedade intelectual vigentes.

---

## 📜 Licenças de Terceiros

- O código-fonte do **BaixALL** está sob licença **MIT** (consulte o arquivo [LICENSE](LICENSE)).
- Componentes de terceiros (`yt-dlp`, `FFmpeg`, `ffprobe`, `Deno` e bibliotecas .NET) possuem suas respectivas licenças, configurações de build e ofertas de código-fonte documentadas detalhadamente em [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
