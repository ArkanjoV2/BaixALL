# Guia de Validação Manual em Windows 11 Limpo — BaixALL 1.0.0-rc.1

Este guia fornece o procedimento passo a passo para validar a Release Candidate (**BaixALL 1.0.0-rc.1**) em um computador ou Máquina Virtual (VM) isolada com **Windows 11 x64** limpo, sem nenhuma dependência de desenvolvimento pré-instalada.

---

## 🎯 1. Pré-Requisitos do Ambiente de Teste

Para assegurar uma validação autêntica e confiável:
- **Sistema Operacional:** Windows 11 (versão 22H2 ou 23H2/24H2) de 64 bits recém-instalado ou em VM limpa.
- **O que NÃO deve estar instalado:**
  - ❌ Nenhum .NET SDK ou Runtime pré-instalado (o BaixALL é self-contained).
  - ❌ Nenhum Visual Studio ou Build Tools.
  - ❌ Nenhum Python, Git ou Node.js nas variáveis de ambiente globais (`PATH`).
  - ❌ Nenhuma cópia prévia de `yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe` ou `deno.exe`.
  - ❌ Nenhuma pasta pré-existente `%LOCALAPPDATA%\BaixALL`.
- **O que DEVE estar disponível:**
  - Conexão ativa com a internet (para que o `DependencyManager` possa baixar as ferramentas oficiais na primeira execução).
  - O arquivo instalador [`BaixALL-Setup-1.0.0-rc.1.exe`](../dist/BaixALL-Setup-1.0.0-rc.1.exe) copiado para a máquina de teste (via pendrive, pasta compartilhada ou download).

---

## 📝 2. Checklist da Etapa 1 — Primeira Instalação e Preparação

| # | Ação | Comportamento Esperado | Resultado (OK / Falha) |
|---|---|---|---|
| 1.1 | Executar `BaixALL-Setup-1.0.0-rc.1.exe` | O assistente de instalação do Inno Setup é aberto em Português do Brasil. *(Nota: Caso o SmartScreen exiba aviso de reputação para novo binário, clique em "Mais informações" > "Executar assim mesmo").* | [ ] |
| 1.2 | Seguir os passos do assistente de instalação | A instalação conclui rapidamente sem erros e sem solicitar privilégios de administrador indevidos (instalando no perfil do usuário ou em Arquivos de Programas). | [ ] |
| 1.3 | Verificar atalhos | Atalho criado no Menu Iniciar (`BaixALL`) e na Área de Trabalho com o ícone oficial da aplicação. | [ ] |
| 1.4 | Abrir o aplicativo pelo atalho | A janela principal abre rapidamente com o tema escuro moderno e estilo Fluent Windows 11. | [ ] |
| 1.5 | Conferir a versão exibida | Acessar a aba **Configurações**: deve exibir claramente **`BaixALL v1.0.0-rc.1`**. | [ ] |
| 1.6 | Fluxo de Primeira Execução (`DependencyManager`) | Como o computador está limpo, o BaixALL detecta as ferramentas ausentes e apresenta a tela/banner de configuração inicial. | [ ] |
| 1.7 | Baixar as ferramentas com 1 clique | O BaixALL baixa automaticamente: `yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe` e `deno.exe`. As barras de progresso avançam normalmente. | [ ] |
| 1.8 | Validação das ferramentas | Ao término, o status no topo muda para **`• Ferramentas Prontas`** e a tela principal de download é liberada para uso. | [ ] |

---

## 🎬 3. Checklist da Etapa 2 — Testes Reais de Download

Utilize um vídeo público de domínio público ou licença aberta (por exemplo: `https://www.youtube.com/watch?v=aqz-KE-bpKQ` - *Big Buck Bunny 4K 60fps*).

| # | Ação | Comportamento Esperado | Resultado (OK / Falha) |
|---|---|---|---|
| 2.1 | Colar a URL e clicar em **"Analisar"** | O aplicativo analisa o vídeo via yt-dlp em segundo plano. Miniatura em alta definição, título, duração e canal são renderizados na tela. | [ ] |
| 2.2 | Selecionar "Melhor qualidade disponível" e clicar em **"Baixar Agora"** | O item é adicionado à aba **Fila**. O status muda de *Aguardando* para *Baixando*, exibindo porcentagem, velocidade (MB/s) e tempo restante (ETA). | [ ] |
| 2.3 | Mesclagem (Merge) pelo FFmpeg | Ao término do download das faixas de áudio e vídeo, o status passa por *Processando... (mesclando)* e finaliza com **`Concluído`**. | [ ] |
| 2.4 | Acessar a pasta de download | Clicar em **"Abrir pasta"**: o Windows Explorer abre na pasta `%USERPROFILE%\Downloads` com o vídeo `.mp4` selecionado. | [ ] |
| 2.5 | Reprodução do vídeo | O arquivo reproduz perfeitamente no reprodutor padrão do Windows (Filmes e TV / Windows Media Player) com vídeo e áudio sincronizados em alta resolução. | [ ] |
| 2.6 | Download Somente Áudio | Marcar a opção **"Somente Áudio"**, escolher o container **MP3** e baixar. O arquivo `.mp3` é gerado, tem áudio limpo e tags corretas. | [ ] |
| 2.7 | Teste de Concorrência (Fila com limite 2) | Em **Configurações**, definir concorrência como `2`. Enfileirar 3 vídeos. Confirmar que 2 vídeos baixam simultaneamente enquanto o 3º aguarda na fila, iniciando automaticamente após um dos downloads terminar. | [ ] |

---

## 🔄 4. Checklist da Etapa 3 — Persistência, Reinicialização e Atualizações

| # | Ação | Comportamento Esperado | Resultado (OK / Falha) |
|---|---|---|---|
| 3.1 | Acessar a aba **Histórico** | Os vídeos baixados aparecem listados com data, duração e botões de ação ("Abrir arquivo" e "Abrir pasta"). | [ ] |
| 3.2 | Fechar e reabrir o BaixALL | Ao abrir novamente, o aplicativo restaura imediatamente: histórico intacto, ferramentas detectadas prontas (sem novo download) e configurações salvas mantidas. | [ ] |
| 3.3 | Verificação de Atualizações | Na aba **Ferramentas** / **Configurações**, clicar em "Verificar Atualizações". O sistema consulta os repositórios oficiais sem travar a interface e informa se há novas versões disponíveis. | [ ] |
| 3.4 | Bloqueio de atualização durante download | Iniciar um download longo e tentar clicar em atualizar ferramentas: o aplicativo deve avisar que a atualização está bloqueada enquanto houver downloads em andamento. | [ ] |

---

## 🗑️ 5. Checklist da Etapa 4 — Desinstalação Segura

| # | Ação | Comportamento Esperado | Resultado (OK / Falha) |
|---|---|---|---|
| 4.1 | Desinstalar pelo Windows | Abrir **Configurações do Windows** > **Aplicativos** > **Aplicativos Instalados** > Localizar **BaixALL** > Clicar em **Desinstalar**. | [ ] |
| 4.2 | Execução do desinstalador | O assistente remove os arquivos executáveis da aplicação e os atalhos do Menu Iniciar e da Área de Trabalho. | [ ] |
| 4.3 | **Segurança de Vídeos (Crítico)** | Abrir a pasta `%USERPROFILE%\Downloads`: **TODOS os vídeos e áudios baixados continuam intactos e preservados.** | [ ] |
| 4.4 | **Preservação de Dados do Usuário** | As pastas de perfil `%LOCALAPPDATA%\BaixALL` (histórico e configurações) não são excluídas silenciosamente. | [ ] |

---

## 📋 Conclusão da Validação

Se todos os itens forem marcados como **OK**:
- A Release Candidate **1.0.0-rc.1** é considerada formalmente **APROVADA**.
- O projeto pode ser promovido para a versão final definitiva **BaixALL 1.0.0**.
