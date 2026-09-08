# BaixALL 1.2.0 — Suporte a Playlists do YouTube e Downloads em Lote

Temos o prazer de apresentar o **BaixALL 1.2.0**, versão que introduz suporte a **playlists do YouTube** e **downloads em lote**, mantendo a confiabilidade arquitetural do motor de download, o controle de concorrência global e a identidade visual introduzida na versão 1.1.0.

Esta atualização expande a usabilidade do BaixALL, permitindo selecionar e baixar múltiplos vídeos de coleções públicas de maneira prática, transparente e com acompanhamento em tempo real.

---

## 🚀 Novidades e Melhorias da Versão 1.2.0

### 1. Suporte a Playlists do YouTube
- **Extração Rápida em Dois Estágios:** Análise preliminar leve via `--flat-playlist` que carrega a listagem de vídeos em poucos segundos, postergando a resolução aprofundada de formatos para o momento do download de cada item individual.
- **Detecção Inteligente de URLs Híbridas:** Ao colar um link de vídeo associado a uma playlist (`watch?v=...&list=...`), o aplicativo identifica a ambiguidade e pergunta se o usuário deseja baixar apenas o vídeo avulso ou carregar a playlist completa.
- **Tratamento de Feeds Especiais:** URLs com parâmetros dinâmicos ou não suportados (como rádios algorítmicas `list=RD...`) exibem notificações claras ao usuário sobre a indisponibilidade, sem travamento da aplicação.

### 2. Interface de Seleção Granular
- **Painel de Informações da Playlist:** Exibição do título da playlist, canal, contagem total de itens e estimativa de duração somada.
- **Controles de Seleção em Massa:** Botões para *Selecionar Todos*, *Desmarcar Todos* e *Inverter Seleção*, com indicador dinâmico do total selecionado (`X de Y selecionados`).
- **Lista Rolável e Otimizada:** Exibição sequencial (`#01`, `#02`...) com título, canal, duração e sinalização visual para vídeos indisponíveis ou privados.

### 3. Configurações de Lote
- **Qualidade e Container Padrão:** Definição da qualidade padrão (Melhor disponível, 1080p, 720p, etc.) e container preferencial (Automático, MP4 ou MKV) para os itens do lote.
- **Modo Somente Áudio em Lote:** Opção para extrair e converter o áudio de todos os itens selecionados (Original, M4A, Opus ou MP3).
- **Organização em Subpastas:** Criação opcional de subpasta com o título da playlist dentro da pasta de destino do usuário.

### 4. Integração à Fila Global e Concorrência Efetiva
- **Fila Universal:** Os itens do lote são integrados como downloads vinculados na fila global compartilhada do aplicativo.
- **Concorrência Controlada:** O lote respeita rigorosamente o limite de downloads simultâneos configurado nas preferências do usuário (suporte testado e validado para **1, 2 ou 3 downloads simultâneos**).
- **Progresso Agregado por Itens:** O painel de lote exibe uma barra de progresso consolidada calculada a partir do avanço individual de cada item (`X de Y vídeos concluídos (Z% por itens)`). Ressalta-se que este indicador reflete o progresso de conclusão dos itens e **não representa necessariamente a porcentagem exata de bytes transferidos do lote**, uma vez que os tamanhos totais em bytes de todos os vídeos de uma playlist nem sempre estão disponíveis previamente de forma confiável.
- **Contadores em Tempo Real:** Badges com a contagem exata de itens `Concluídos`, `Baixando`, `Aguardando`, `Falhas` e `Cancelados`.

### 5. Cancelamento e Rotinas de Limpeza
- **Cancelamento Granular:** O usuário pode cancelar um download individual na fila ou cancelar o lote completo através do botão *Cancelar Lote*.
- **Tratamento de Arquivos Transitórios:** Ao cancelar um download ou em situações de interrupção, o aplicativo encerra os processos filhos associados e executa rotinas para remover as pastas temporárias de trabalho isoladas (`jobTempDir`) e os arquivos parciais gerados (`.part`, `.ytdl`, `.tmp`), visando evitar acúmulo de arquivos residuais no disco.

### 6. Confirmação Preventiva ao Fechar
- Ao tentar fechar a janela enquanto houver downloads ativos ou itens aguardando, o BaixALL exibe um diálogo de confirmação informando que os downloads em andamento serão cancelados e que itens pendentes não serão retomados automaticamente, preservando os arquivos já concluídos, o histórico e as configurações.

---

## ⚠️ Limitações Conhecidas e Escopo

- **Disponibilidade no YouTube:** O BaixALL depende da disponibilidade técnica dos vídeos na plataforma e dos extratores do `yt-dlp`. Vídeos privados, excluídos, restritos por região, protegidos por DRM comercial ou com desafios estritos de verificação humana não são suportados. Não há garantia de compatibilidade universal com toda e qualquer URL ou playlist.
- **Comportamento da Fila ao Fechar o Aplicativo:** Na versão 1.2.0, os lotes pendentes e downloads em andamento **não são retomados automaticamente** após o encerramento da aplicação. Os itens pendentes cancelados no fechamento deverão ser reenfileirados manualmente pelo usuário. A persistência completa do estado da fila com retomada resiliente pós-reinicialização é uma possibilidade que poderá ser estudada para a **versão 1.3.0**, não constituindo funcionalidade implementada nem garantia de cronograma.
- **Conexão e Desempenho:** A velocidade de download e estabilidade dependem dos servidores do YouTube e da conectividade local do usuário.
- **Qualidade Técnica:** O projeto conta com **170 testes automatizados aprovados** cobrindo modelos, concorrência, cancelamento, parsing de playlists e regressões funcionais.

---

## 🛡️ Orientações sobre o Windows SmartScreen

Ao executar o instalador ou aplicativo pela primeira vez, o Windows Defender SmartScreen pode exibir um alerta de reputação (*"O Windows protegeu o seu computador"*). Isso ocorre porque softwares livres e independentes recém-compilados ainda não possuem reputação estatística consolidada junto à Microsoft.

### Orientações de Segurança:
1. Confirme que você baixou o arquivo exclusivamente a partir da página oficial de [Releases do BaixALL](https://github.com/ArkanjoV2/BaixALL/releases).
2. Verifique a autenticidade comparando o hash SHA-256 do arquivo baixado com os valores publicados na tabela oficial abaixo e no arquivo `checksums-1.2.0.txt`.
3. **Caso tenha qualquer dúvida sobre a integridade ou origem do arquivo, não execute o executável.**
4. O BaixALL não recomenda e não solicita a desativação de nenhum recurso de segurança ou proteção do Windows Defender.

---

## 📦 Artefatos Oficiais de Distribuição

| Arquivo | Descrição | Tamanho | SHA-256 |
| :--- | :--- | :--- | :--- |
| `BaixALL-Setup-1.2.0.exe` | Instalador assistido para Windows x64 (Inno Setup) | 45.884.362 bytes (~43,8 MB) | `f3eb80a7cefa97cb02a683a1784453749b5ff050701f0047fb9e46e6bb7749c1` |
| `BaixALL-1.2.0-win-x64.zip` | Pacote portátil descompactável (execução direta) | 65.490.739 bytes (~62,5 MB) | `cc99f827815b2b7d45af85cef324cee2e1f15a0c9abb38134beb9469a6bebb3d` |
| `checksums-1.2.0.txt` | Hashes de integridade criptográfica SHA-256 | 181 bytes | `fcbe8ccc39c49a417cf0362116b373a33d562f88ebac3f5045e9bb818fc38e93` |
