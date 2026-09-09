# BaixALL 1.3.0 — Versão Estável Multiplataforma

Apresentamos a versão estável **1.3.0** do **BaixALL** — a maior atualização desde o lançamento inicial. Esta versão consolida o suporte nativo experimental a downloads de vídeos públicos do **Instagram** e do **X/Twitter**, preservando integralmente a estabilidade já comprovada do **YouTube** nas versões anteriores.

> **Importante:** O motor de extração baseia-se no `yt-dlp`. O suporte ao Instagram e ao X/Twitter depende dos extratores upstream e da disponibilidade de acesso público por parte das plataformas. Conteúdos que exigem autenticação não são suportados.

---

## 🚀 Novidades da Versão 1.3.0

### 1. Downloads do Instagram (Experimental)

- **Reels públicos** via URLs `/reel/` e `/reels/`.
- **Publicações com vídeo** no feed (`/p/`) sem autenticação.
- **Carrosséis com múltiplos vídeos:** o BaixALL analisa a coleção completa, exibe a contagem de mídias, desabilita fotos estáticas com aviso informativo e enfileira cada vídeo individualmente com o índice original preservado.
- Identificação automática da plataforma com badge rosa `Instagram` na fila e no histórico.
- Subpasta criada automaticamente com o título/legenda da publicação ao baixar em lote.

### 2. Downloads do X/Twitter (Experimental)

- **Publicações de vídeo** via links `x.com` e `twitter.com`.
- **GIFs animados** tratados como fluxos de vídeo (MP4).
- Rejeição preventiva de posts sem mídia de vídeo (textos puros, imagens estáticas) com mensagem informativa em português.
- Identificação automática com badge ciano `X / Twitter` na fila e no histórico.

### 3. Identificação Automática de Plataforma

- Detecção de plataforma por evidências na URL, no extrator ou nos metadados de cada item.
- Chaves canônicas únicas por mídia (`yt:ID`, `ig:ID`, `x:ID`) garantindo que cada arquivo seja rastreado e deduplicado de forma independente no histórico.

### 4. Seleção de Mídias em Coleções

- Coleções (playlists do YouTube, carrosséis do Instagram) são analisadas e listadas com checkbox individual.
- Botões de ação em lote: **Selecionar Todos**, **Desmarcar Todos**, **Inverter Seleção** e contador de seleção em tempo real.
- Download seletivo: somente os itens marcados entram na fila de downloads.
- O índice original da coleção é preservado na execução (`--playlist-items N`) para evitar tokens de CDN expirados.

### 5. Fila Global Multiplataforma

- Downloads do YouTube, Instagram e X/Twitter convivem na mesma fila sem conflito.
- Concorrência configurável: 1, 2 ou 3 downloads simultâneos (padrão: 2).
- Botão **Cancelar** por item individual e **Cancelar Lote** por coleção, com feedback visual imediato.
- Barra de progresso por item e progresso agregado honesto por quantidade de itens concluídos para lotes.

### 6. Histórico Multiplataforma

- Entradas do histórico incluem o badge da plataforma (YouTube, Instagram, X/Twitter ou Desconhecido).
- Compatibilidade regressiva: entradas anteriores (v1.0.0–v1.2.0) são inferidas por evidências e exibidas sem perda de dados.
- Botões de ação por item: **Abrir Arquivo**, **Abrir Pasta** e **Remover do Histórico**.

### 7. Diálogo de Fechamento Preventivo

- Ao tentar fechar o aplicativo com downloads ativos ou pendentes, um diálogo de confirmação informa o impacto antes de cancelar:
  - **Não:** cancela o fechamento, downloads continuam em execução.
  - **Sim:** cancela todos os downloads ativos e encerra a aplicação de forma limpa, sem processos órfãos.
- Sem downloads ativos, o aplicativo fecha imediatamente sem exibir o diálogo.

### 8. Correções de Formatos e Codecs

- Resolução correta de formatos com codec de vídeo desconhecido ou `none` (ex: streams de vídeo apenas com áudio separado no YouTube).
- Tratamento de vídeos verticais de alta resolução do X/Twitter sem quebra de pipeline de mesclagem.
- Seleção de container automático (`Auto`) que preserva a qualidade sem transcodificação desnecessária.

---

## 📦 Artefatos da Versão

| Arquivo | Descrição | Tamanho | SHA-256 |
| :--- | :--- | :--- | :--- |
| `BaixALL-Setup-1.3.0.exe` | Instalador assistido para Windows x64 (Inno Setup) | 45.890.001 bytes | `b434214ae7b121347f034d0e0bbc74d708688b48d2bcce5e6eeb879b999985c8` |
| `BaixALL-1.3.0-win-x64.zip` | Pacote portátil autônomo (execução direta) | 65.504.526 bytes | `2310927586a61f8155944f5a2636399fdba7cf24c0f52e5fbd534c02699d6b01` |
| `checksums-1.3.0.txt` | Hashes de integridade criptográfica SHA-256 | 185 bytes | `1823e861154b9197c5529d41b3cca61619c3d57c3e3041ab6e4a745cdb6eb0d7` |


---

## 🔒 Segurança e Privacidade

- **100% Local:** sem telemetria, sem envio de dados ou metadados para servidores de terceiros.
- **Sem Coleta de Credenciais:** o BaixALL não solicita login e não acessa cookies locais de navegadores.
- **Isolamento de Processos:** downloads executados em diretórios temporários atômicos com limpeza rigorosa em cancelamentos e encerramentos.

---

## ⚠️ Limitações Conhecidas

1. **Dependência de Extratores e Disponibilidade das Plataformas:** O suporte ao Instagram e ao X/Twitter depende diretamente do motor upstream `yt-dlp`. Mudanças de layout, restrições de IP ou mecanismos anti-scraping adotados pelas plataformas podem afetar a extração até que uma nova versão dos componentes seja atualizada na aba *Ferramentas*.
2. **Autenticação e Conteúdo Privado:** Stories, contas privadas, conteúdos restritos por idade, Spaces e perfis inteiros estão fora do escopo. Qualquer mídia que exija sessão logada retorna uma mensagem informativa ao usuário; nenhuma credencial é solicitada ou armazenada.
3. **Fotos em Carrosséis:** somente mídias em formato de vídeo são baixadas. Imagens e fotos estáticas são desabilitadas com aviso visual.
4. **Persistência da Fila:** a fila de downloads não é persistida entre reinicializações do aplicativo; itens pendentes precisam ser enfileirados novamente após o fechamento.
5. **Compatibilidade Universal:** o BaixALL não garante funcionamento para 100% das publicações de Instagram e X/Twitter. A taxa de sucesso depende do tipo de mídia, da conta, da região e da versão do extrator.

---

## 🔄 Compatibilidade

- **Sistema Operacional:** Windows 10/11 x64 (sem necessidade de instalar o .NET separadamente — runtime incluído).
- **Atualização da v1.0.0, v1.1.0 ou v1.2.0:** o instalador preserva histórico, configurações e ferramentas automaticamente.
- **Histórico Legado:** entradas anteriores são detectadas e exibidas com badge de plataforma inferida, sem perda de dados.
