# BaixALL

**Aplicativo desktop para Windows que permite baixar vídeos e áudios na melhor qualidade disponível, com suporte a yt-dlp, FFmpeg, fila de downloads e gerenciamento automático de ferramentas.**

O **BaixALL** é um aplicativo desktop nativo para Windows desenvolvido em **C#**, **.NET 10** e **WPF** com o padrão arquitetural **MVVM**. Ele foi projetado para oferecer uma experiência simples, moderna e confiável para download de vídeos e extração de áudios do YouTube, combinando o poder do **yt-dlp**, a precisão de mesclagem do **FFmpeg** e o suporte a scripts do **Deno**, com privacidade absoluta: 100% local, sem telemetria, sem anúncios e sem comunicação com servidores externos além dos canais oficiais de mídia e ferramentas.

<!-- 
Captura de tela real da interface:
Uma captura de tela real da interface principal poderá ser inserida aqui após a aprovação da imagem oficial.
![Interface do BaixALL](docs/screenshots/main_window.png)
-->

---

## ✨ Recursos Principais

- **Qualidade Máxima com Mesclagem Automática:** Seleciona automaticamente o melhor vídeo e o melhor áudio disponíveis (incluindo 4K a 60 FPS e 1080p a 60 FPS) e realiza a mesclagem perfeita via FFmpeg.
- **Seleção Inteligente de Resolução e FPS:** Escolha resoluções exatas (4K 2160p, 1440p, 1080p, 720p, 480p, etc.) com indicação da taxa de quadros (60 FPS ou 30 FPS).
- **Modo Somente Áudio:** Extração direta mantendo a faixa original (M4A/AAC ou Opus) ou conversão em alta qualidade para **MP3** (320 kbps) via FFmpeg.
- **Containers de Saída Flexíveis:** Opções em **MP4** e **MKV** para vídeo, e **M4A**, **Opus** e **MP3** para áudio.
- **Fila com Concorrência Configurável:** Adicione múltiplos downloads sucessivamente com controle de 1, 2 ou 3 downloads simultâneos e transição automática para o próximo item.
- **Cancelamento Individual Seguro:** Cancele downloads individualmente sem travar a aplicação, liberando a vaga na fila e limpando arquivos temporários (`.part`, `.ytdl`).
- **Histórico Persistente:** Registro completo de downloads salvos em formato JSON, com botões para abrir o arquivo ou localizá-lo diretamente no Windows Explorer.
- **Gerenciamento Automático de Ferramentas:** Instalação e atualização transparente de `yt-dlp`, `FFmpeg`, `ffprobe` e `Deno` direto dos lançamentos oficiais do GitHub, sem sujar o sistema.
- **Autônomo (Self-Contained):** Não requer instalação prévia do .NET SDK, Python, Node.js ou Visual Studio.
- **Interface Moderna em Português:** Estilo visual Fluent inspirado no Windows 11 com suporte a temas Claro e Escuro.

---

## 💻 Requisitos do Windows

- **Sistema Operacional:** Windows 10 (versão 1809 ou superior) ou Windows 11.
- **Arquitetura:** 64-bit (x64 / AMD64).
- **Runtimes:** Nenhum runtime externo é necessário (.NET 10 LTS embutido na aplicação).
- **Espaço Livre em Disco:** Mínimo de 300 MB livres para o executável e ferramentas auxiliares, além do espaço livre necessário para os seus downloads de mídia.
- **Rede:** Conexão à internet para análise de links e downloads.

---

## 📥 Download da Versão Mais Recente

Os binários oficiais e verificados da versão estável **1.0.0** estão preparados para distribuição:

| Pacote | Arquivo | Descrição |
| :--- | :--- | :--- |
| **Instalador Oficial** | `BaixALL-Setup-1.0.0.exe` | Assistente de instalação completo para Windows 10/11 com criação de atalhos e desinstalador seguro. |
| **Pacote Portátil (ZIP)** | `BaixALL-1.0.0-win-x64.zip` | Versão compactada sem necessidade de instalação. Basta descompactar e executar `BaixALL.exe`. |
| **Integridade (Hashes)** | `checksums.txt` | Arquivo contendo os hashes criptográficos SHA-256 de todos os artefatos oficiais. |

> [!NOTE]
> *(Os links diretos para download estarão disponíveis na página oficial de Releases do repositório assim que a publicação for concluída).*

---

## 🚀 Como Instalar

### Opção 1: Instalador Oficial (`BaixALL-Setup-1.0.0.exe`)
1. Baixe o instalador `BaixALL-Setup-1.0.0.exe`.
2. Dê um duplo clique para iniciar o assistente em Português do Brasil.
3. Escolha se deseja criar atalhos na Área de Trabalho e no Menu Iniciar.
4. Conclua a instalação. O instalador instala os arquivos na pasta do usuário (`%LOCALAPPDATA%\Programs\BaixALL`) sem exigir privilégios de administrador.

### Opção 2: Pacote Portátil (`BaixALL-1.0.0-win-x64.zip`)
1. Baixe o arquivo `BaixALL-1.0.0-win-x64.zip`.
2. Clique com o botão direito e selecione **"Extrair Tudo..."** para a pasta de sua preferência.
3. Abra a pasta extraída e execute `BaixALL.exe`.

---

## 📖 Como Utilizar o Aplicativo

### 1. Primeiro Uso e Instalação Automática das Ferramentas
Ao abrir o BaixALL pela primeira vez, o aplicativo verifica a presença das ferramentas de apoio (`yt-dlp`, `FFmpeg`, `ffprobe` e `Deno`):
- Se alguma ferramenta estiver ausente, o BaixALL exibirá a aba **Ferramentas** com um resumo claro e o botão **"Instalar Ferramentas"**.
- Com um único clique, o BaixALL baixa automaticamente as versões oficiais mais recentes diretamente dos repositórios do GitHub para a pasta isolada `%LOCALAPPDATA%\BaixALL\tools`.
- **Como funciona o isolamento:** As ferramentas são mantidas exclusivamente na pasta de dados do BaixALL. Elas não são adicionadas ao `PATH` global do Windows, não interferem em outras ferramentas instaladas no seu computador e não exigem privilégios elevados.
- O BaixALL valida os binários antes de utilizá-los e impede atualizações enquanto houver downloads em andamento para evitar arquivos corrompidos.

### 2. Analisar um Vídeo
1. Copie o link (URL) do vídeo do YouTube no seu navegador.
2. No BaixALL, clique no campo de texto da tela inicial e cole o link (o aplicativo suporta colagem automática ao focar).
3. Clique em **"Analisar"** (ou pressione `Enter`).
4. O BaixALL consulta a estrutura de streams do YouTube e exibe título, autor/canal, duração e a miniatura em alta resolução.

### 3. Escolher Qualidade e Formato
- **"Melhor qualidade disponível" (Padrão Recomendado):** O BaixALL identifica a melhor faixa de vídeo e a melhor faixa de áudio e as mescla sem perda de fidelidade.
- **Resolução específica:** Escolha entre 2160p (4K), 1440p (2K), 1080p (Full HD), 720p (HD), 480p, etc., selecionando também se deseja a taxa em 60 FPS quando disponível.
- **Formato do vídeo:** Escolha entre **MP4** (máxima compatibilidade com TVs, celulares e editores) ou **MKV** (suporte avançado a múltiplos codecs).

### 4. Modo Somente Áudio
Se você precisa apenas da música ou áudio:
1. Marque a opção **"Somente Áudio"** na tela inicial.
2. Escolha o formato desejado:
   - **Melhor áudio disponível (Original):** Mantém a faixa nativa (M4A ou Opus) sem nenhuma recodificação.
   - **M4A:** Codec AAC em container MP4, ideal para iPhone, iPad e aparelhos tradicionais.
   - **Opus:** Alta fidelidade sonora com arquivo compacto.
   - **MP3:** Conversão em alta fidelidade a 320 kbps via FFmpeg, compatível com reprodutores automotivos e caixas de som antigas.

### 5. Pasta de Destino
- Por padrão, os downloads são salvos em `%USERPROFILE%\Downloads`.
- Você pode alterar a pasta padrão na aba **Configurações** a qualquer momento. O nome dos arquivos é sanitizado automaticamente para evitar caracteres inválidos do Windows.

### 6. Gerenciar Fila e Downloads Simultâneos
- Ao clicar em **"Baixar Agora"**, o item é enviado para a aba **Fila**.
- Você pode adicionar vários links em sequência sem esperar os anteriores terminarem.
- Na aba **Configurações**, escolha a concorrência desejada (1, 2 ou 3 downloads simultâneos). Quando um download termina, o próximo da fila inicia automaticamente.
- Cada download possui um botão **"Cancelar"** individual: ele encerra o processo de forma limpa, remove arquivos temporários parciais (`.part`, `.ytdl`) e libera a vaga na fila para o próximo item.

### 7. Histórico de Downloads
- Na aba **Histórico**, visualize todos os itens já concluídos com informações de data, tamanho e duração.
- Use o botão **"Abrir arquivo"** para tocar a mídia no player padrão do Windows ou **"Abrir pasta"** para destacá-lo no Windows Explorer.
- O histórico é salvo em `%LOCALAPPDATA%\BaixALL\history.json`. O botão **"Limpar Histórico"** limpa a lista visual sem apagar os vídeos baixados no disco.

---

## 🗑️ Desinstalação Segura

O BaixALL foi projetado para respeitar rigorosamente os arquivos do usuário:
- **Seus vídeos e músicas baixados NUNCA são apagados.** A desinstalação remove apenas os arquivos do programa.
- Para desinstalar, use **Configurações do Windows > Aplicativos > Aplicativos Instalados > BaixALL > Desinstalar** (ou o atalho no Menu Iniciar).
- As configurações e histórico em `%LOCALAPPDATA%\BaixALL` são preservados caso deseje reinstalar futuramente.

---

## 🛡️ Segurança, SmartScreen e Assinatura Digital

- **Aviso do Windows Defender SmartScreen na Primeira Execução:**
  Ao executar um instalador ou executável novo de código aberto, o Windows Defender SmartScreen pode exibir o diálogo *"O Windows protegeu o seu computador"*, informando que se trata de um aplicativo não comumente baixado.
  - Para prosseguir normalmente: clique no link **"Mais informações"** e em seguida no botão **"Executar assim mesmo"**.
- **Esclarecimento Técnico sobre Assinatura e Reputação:**
  - **Reputação do SmartScreen:** O SmartScreen é um serviço baseado em telemetria e volume estatístico de downloads benignos ao longo do tempo.
  - **Certificados Digitais (Authenticode):** Aplicativos novos **podem apresentar avisos de reputação mesmo quando assinados digitalmente** (inclusive com certificados comerciais OV ou EV), uma vez que a reputação precisa ser construída gradualmente conforme os usuários utilizam o software.
  - **Política do BaixALL:** O BaixALL **nunca desabilita** proteções nativas do Windows e **não utiliza certificados autoassinados fictícios**, pois certificados falsos não possuem cadeia de confiança pública e não resolvem avisos do sistema.
  - A contratação de um certificado comercial Authenticode poderá ser avaliada futuramente caso o mantenedor decida adquirir o serviço para distribuição de larga escala.

---

## ⚠️ Limitações Conhecidas

- **Disponibilidade no YouTube:** O BaixALL depende da disponibilidade dos vídeos e da capacidade do motor `yt-dlp`. Vídeos privados, vídeos com restrição geográfica, conteúdos com proteção comercial por DRM ou que exijam login com verificação de idade rigorosa podem não ser passíveis de download. O projeto **não promete suporte universal a todo e qualquer vídeo**.
- **Mudanças na Plataforma:** O YouTube atualiza seus protocolos periodicamente. Caso algum vídeo apresente falha na análise, verifique se há atualizações do `yt-dlp` na aba **Ferramentas**.
- **Conexão de Rede:** Falhas severas de conexão de rede durante o download de arquivos pesados podem exigir o reinício do download correspondente.

---

## ⚖️ Aviso Legal e Direitos Autorais

O **BaixALL** é uma ferramenta tecnológica criada para interoperabilidade, arquivamento pessoal e uso em conformidade com as leis aplicáveis.

> [!IMPORTANT]
> O usuário é o único responsável pelo uso que faz deste software. Certifique-se de baixar apenas vídeos e áudios sobre os quais você possua os devidos direitos autorais, que estejam em **Domínio Público**, licenciados sob termos abertos (como **Creative Commons**) ou para os quais você tenha autorização expressa do detentor dos direitos. Não utilize o aplicativo para violar os Termos de Serviço da plataforma ou leis de propriedade intelectual vigentes.

---

## 📜 Licença e Componentes de Terceiros

- O código-fonte do **BaixALL** é distribuído sob a licença **MIT** (consulte o arquivo [LICENSE](LICENSE)).
- O BaixALL utiliza e interage com componentes de terceiros (`yt-dlp`, `FFmpeg`, `ffprobe`, `Deno` e bibliotecas NuGet). As respectivas licenças, configurações de compilação, notices de direitos autorais e compromissos de conformidade com a GPLv3 estão detalhadamente documentados em [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
