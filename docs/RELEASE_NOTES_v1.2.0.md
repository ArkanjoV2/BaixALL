# BaixALL 1.2.0 — Suporte a Playlists do YouTube e Downloads em Lote

Temos o prazer de anunciar o lançamento do **BaixALL 1.2.0**, uma versão de grande relevância que introduz o tão aguardado suporte a **playlists do YouTube** e **downloads em lote**, mantendo a confiabilidade arquitetural, o respeito à concorrência global e a nova identidade visual introduzida na v1.1.0.

Esta versão expande as capacidades de download do BaixALL permitindo selecionar e baixar álbuns, cursos, compilações e coleções de vídeos inteiras de maneira simples, transparente e controlada.

---

## 🚀 Principais Novidades da Versão 1.2.0

### 1. Suporte Nativo a Playlists do YouTube
- **Extração Rápida e Otimizada em Dois Estágios:** Análise preliminar leve via `--flat-playlist` que carrega listas com dezenas de itens em poucos segundos, postergando a resolução detalhada de formatos para o momento da execução de cada item.
- **Detecção Inteligente de URLs Híbridas:** Ao colar uma URL de vídeo que faz parte de uma playlist (`watch?v=...&list=...`), o BaixALL identifica a ambiguidade e exibe um diálogo claro perguntando se o usuário deseja baixar apenas o vídeo avulso ou carregar a playlist inteira.
- **Tratamento Gracioso de Feeds Dinâmicos:** Playlists dinâmicas ou privadas do YouTube (como feeds de rádio automáticos `list=RD...`) exibem mensagens explicativas e amigáveis ao usuário sem travamentos.

### 2. Interface Dedicada de Seleção Granular
- **Painel de Informações da Playlist:** Exibe o título da playlist, canal criador, contagem total de itens e duração estimada calculada.
- **Controles de Seleção em Massa:** Botões operacionais para *Selecionar Todos*, *Desmarcar Todos* e *Inverter Seleção*, acompanhados de contador dinâmico de itens selecionados em tempo real (`X de Y selecionados`).
- **Lista Rolável e Virtualizada:** Exibição com numeração sequencial (`#01`, `#02`...), título, canal, duração formatada e aviso visual em caso de itens indisponíveis ou privados.

### 3. Configurações Globais do Lote
- **Qualidade e Container do Lote:** Permite definir a qualidade padrão desejada para todos os vídeos da playlist (Melhor qualidade disponível, 1080p, 720p, etc.) e o container preferencial (Automático, MP4 ou MKV).
- **Modo Somente Áudio em Lote:** Opção de extrair e converter automaticamente o áudio de todos os itens da playlist para o formato escolhido (Original, M4A, Opus ou MP3).
- **Organização em Subpastas:** Opção ativada por padrão para criar uma subpasta com o nome da playlist na pasta de destino, organizando perfeitamente seus arquivos no disco.

### 4. Integração à Fila Global e Concorrência Controlada
- **Fila Única e Universal:** Os itens do lote são enfileirados como downloads individuais vinculados a um lote na fila global compartilhada do aplicativo.
- **Respeito aos Limites de Concorrência:** O lote respeita rigorosamente o limite de downloads simultâneos configurado pelo usuário (1 a 5 slots). Não há sobrecarga de rede, travamento de threads ou esgotamento de conexões.
- **Painel Agregado de Lote:** Acompanhamento do progresso global da playlist com barra de progresso honesta ponderada por itens concluídos e percentual exato (`X de Y vídeos concluídos (Z% por itens)`).
- **Contadores Detalhados em Tempo Real:** Badges com a contagem precisa de itens `Concluídos`, `Baixando`, `Aguardando`, `Falhas` e `Cancelados`.

### 5. Cancelamento Granular e Limpeza de Temporários
- **Cancelamento Individual:** Possibilidade de cancelar um único vídeo da playlist na fila sem interferir nos demais downloads. O próximo vídeo da fila assume a vaga imediatamente.
- **Cancelamento do Lote Completo:** Botão para cancelar todos os itens restantes daquele lote de uma só vez.
- **Limpeza Garantida:** Ao cancelar qualquer download, os diretórios isolados de trabalho e os arquivos temporários parciais (`.part`, `.ytdl`, `.tmp`) são removidos imediatamente do disco no bloco de finalização.

### 6. Confirmação Preventiva de Fechamento
- Ao tentar fechar o BaixALL com downloads ativos ou itens aguardando na fila, o aplicativo exibe um diálogo de aviso claro:
  - Alerta que os downloads ativos serão interrompidos e que a fila não será retomada automaticamente.
  - Assegura que os arquivos já baixados, o histórico e as configurações permanecerão salvos.
  - Oferece as opções de continuar baixando (*Não*) ou confirmar o encerramento seguro (*Sim*).

---

## ⚠️ Limitações Conhecidas e Escopo da Versão

> [!IMPORTANT]
> **Comportamento da Fila ao Fechar o Aplicativo:**  
> Na versão 1.2.0, os lotes pendentes e downloads ativos **não são retomados automaticamente** após o encerramento do aplicativo. Caso o aplicativo seja fechado, os itens não finalizados deverão ser adicionados novamente pelo usuário caso deseje continuá-los.  
> A persistência completa de estado da fila em disco com retomada automática e recuperação resiliente de downloads interrompidos está formalmente planejada para a **versão 1.3.0**.

---

## 🛡️ Aviso do Windows SmartScreen

Ao executar o instalador ou aplicativo pela primeira vez, o Windows Defender SmartScreen pode exibir uma mensagem informando que o arquivo é desconhecido ou não reconhecido.

### Por que esse aviso aparece?
- O BaixALL é um software livre e de código aberto (*open-source*), desenvolvido de forma independente.
- Para evitar essa tela do SmartScreen, a Microsoft exige a assinatura do executável com um certificado digital corporativo pago (EV/OV), que possui custos elevados e recorrência anual, incompatíveis com projetos independentes sem fins lucrativos.
- O código-fonte é aberto, transparente e auditável publicamente neste repositório.

### Como prosseguir com segurança:
1. No aviso do Windows Defender SmartScreen, clique no link **"Mais informações"**.
2. Em seguida, clique no botão **"Executar assim mesmo"**.
3. A instalação ocorrerá normalmente.
4. Você pode conferir a integridade do instalador comparando o hash SHA-256 do arquivo baixado com os valores oficiais fornecidos abaixo.

---

## 📦 Artefatos Oficiais de Distribuição

| Arquivo | Descrição | Tamanho | SHA-256 |
| :--- | :--- | :--- | :--- |
| `BaixALL-Setup-1.2.0.exe` | Instalador assistido para Windows x64 (Inno Setup) | *(preenchido no build)* | *(preenchido no build)* |
| `BaixALL-1.2.0-win-x64.zip` | Pacote portátil descompactável (execução direta) | *(preenchido no build)* | *(preenchido no build)* |
| `checksums-1.2.0.txt` | Hashes de integridade criptográfica SHA-256 | *(preenchido no build)* | *(preenchido no build)* |
