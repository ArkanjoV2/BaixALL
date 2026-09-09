# BaixALL 1.3.0-rc.1 — Notas da Versão Candidata Multiplataforma

Apresentamos a **Release Candidate 1** da versão **1.3.0** do **BaixALL**. Esta versão transforma o aplicativo em uma central **multiplataforma**, adicionando suporte nativo para downloads de vídeos e mídias públicas do **Instagram** e do **X/Twitter**, preservando integralmente 100% da estabilidade do **YouTube** consolidada nas versões 1.0.0, 1.1.0 e 1.2.0.

> **Importante:** O motor de download único baseia-se em `yt-dlp` e `FFmpeg`. Nenhum login, cookie de sessão ou credencial de usuário é solicitado ou armazenado. Mídias privadas ou restritas (como Stories do Instagram) são rejeitadas preventivamente com mensagem informativa clara.

---

## 🚀 Principais Novidades da Versão 1.3.0-rc.1

### 1. Suporte Multiplataforma Experimental
> **Nota de Pré-lançamento:** O suporte a Instagram e X/Twitter é adicionado em caráter **experimental** e depende estritamente das regras de extração do motor `yt-dlp` e da disponibilidade de acesso público das respectivas plataformas. O suporte consolidado ao YouTube permanece o padrão primário estável.

- **Instagram (Experimental):**
  - Reels públicos (`/reel/` e `/reels/`).
  - Publicações de vídeo públicas no feed (`/p/`).
  - Carrosséis com múltiplas mídias públicas: análise agregada de itens, diferenciação clara de contadores ("3 mídias • 2 vídeos"), desativação de fotos estáticas com aviso informativo âmbar e execução via `--playlist-items` com tokens de CDN atualizados sob demanda.
- **X / Twitter (Experimental):**
  - Publicações de vídeo com links `x.com` e `twitter.com`.
  - Tratamento de GIFs animados como fluxos de vídeo (MP4).
  - Rejeição preventiva em posts de texto puro ou fotos sem vídeo.

### 2. Refinamentos Visuais e Consistência de Design
- **Badges de Plataforma:** Estilo pill discreto presente na tela inicial, carrossel, fila e histórico:
  - **YouTube:** Vermelho (`#FF4444`)
  - **Instagram:** Rosa (`#F472B6`)
  - **X / Twitter:** Ciano (`#38BDF8`)
- **Títulos e Contadores de Carrossel:** Extração da legenda original do post para exibição como título real, eliminando redundâncias de nomenclatura.
- **Fechamento Preventivo:** Diálogo de confirmação ao tentar fechar o aplicativo com downloads ativos ou pendentes, permitindo ao usuário desistir (Não) ou cancelar e encerrar graciosamente (Sim).
- **Compatibilidade Regressiva do Histórico:** Detecção por evidências reais para entradas legadas e atribuição neutra de "Desconhecido" na ausência de dados, mantendo o arquivo `history.json` íntegro.

---

## 📦 Artefatos da Versão Candidata

| Arquivo | Descrição | Tamanho | SHA-256 |
| :--- | :--- | :--- | :--- |
| `BaixALL-Setup-1.3.0-rc.1.exe` | Instalador assistido para Windows x64 (Inno Setup) | 45.887.581 bytes | `8c8532ff673aa57214075dc8195144e7958b894ab9c698e058109203a8431dbb` |
| `BaixALL-1.3.0-rc.1-win-x64.zip` | Pacote portátil autônomo (execução direta) | 65.503.534 bytes | `0f53d380f70040b4a2bbb354519062e55776d7cdbbcea49847477ffda8e11391` |
| `checksums-1.3.0-rc.1.txt` | Hashes de integridade criptográfica SHA-256 | 194 bytes | `4fe4748bd1b4da6af2f28c344f255aa1e46b1bcab2e6ed82e43143ef474ffcf5` |

---

## 🔒 Segurança e Privacidade

- **100% Local:** Sem telemetria, sem envio de dados ou metadados para servidores de terceiros.
- **Sem Coleta de Credenciais:** O BaixALL não solicita login e não acessa cookies locais de navegadores.
- **Isolamento de Processos:** Downloads executados em diretórios temporários atômicos, com limpeza rigorosa em cancelamentos ou encerramentos.

---

## ⚠️ Limitações Conhecidas e Escopo Experimental

1. **Dependência de Extratores e Disponibilidade das Plataformas:** O suporte a Instagram e X/Twitter é experimental e depende diretamente do motor upstream `yt-dlp`. Mudanças unilaterais de layout, restrições temporárias de IP ou mecanismos anti-scraping adotados por essas redes podem afetar a extração até que uma nova versão do componente seja atualizada na aba *Ferramentas*.
2. **Autenticação e Sessão:** O BaixALL não solicita, não armazena e não utiliza credenciais ou cookies locais de navegadores. Qualquer mídia que exija sessão logada (Stories, contas privadas, restrição por idade ou publicações em que a plataforma decida bloquear o acesso anônimo retornando *empty media response*) não poderá ser baixada e exibirá uma mensagem informativa ao usuário.
3. **Mídias em Carrosséis:** Em postagens com múltiplos itens, o BaixALL suporta exclusivamente as mídias em formato de vídeo. Imagens e fotos estáticas são desabilitadas com aviso visual informativo e não entram na fila de downloads.
4. **Persistência da Fila:** A fila de downloads não é persistida em disco entre reinicializações do aplicativo; caso o usuário encerre o app durante um lote, os itens pendentes deverão ser enfileirados novamente.
