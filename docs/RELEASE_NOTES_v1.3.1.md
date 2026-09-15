# BaixALL 1.3.1 — Versão Corretiva (Patch)

A versão **1.3.1** do **BaixALL** é uma versão corretiva (patch) focada em solucionar a interrupção de análise em publicações do tipo carrossel misto do Instagram (posts que combinam fotos e vídeos em uma mesma sequência).

Esta atualização não introduz novas funcionalidades de grande porte nem altera os fluxos existentes para YouTube e X/Twitter, concentrando-se na robustez e integridade da descoberta de mídias multiplataforma.

---

## 🛠️ Correções e Melhorias da Versão 1.3.1

### 1. Suporte a Carrosséis Mistos do Instagram (Fotos + Vídeos)
- **Problema Corrigido:** Ao analisar carrosséis do Instagram onde os primeiros itens eram fotos estáticas, o extrator encerrava com erro `[Instagram] : No video formats found!`, abortando precocemente o processo e impedindo que vídeos válidos subsequentes da mesma publicação fossem detectados.
- **Resolução:** A análise agora aplica a opção `--ignore-no-formats-error` exclusivamente na etapa de extração de metadados, permitindo que todas as mídias da publicação sejam catalogadas com sucesso. O fluxo de download final permanece estrito, garantindo a integridade dos arquivos baixados.

### 2. Preservação Rigorosa de Índices Originais (1-Based)
- Todos os itens da publicação mantêm seu índice posicional original (`PlaylistIndex = 1, 2, ...`), independentemente de itens anteriores serem fotos ou estarem desabilitados.
- Ao baixar um vídeo na segunda posição do carrossel, o BaixALL invoca `--playlist-items 2`, assegurando que o arquivo exato da publicação seja obtido sem desvios de índice.

### 3. Tratamento Visual de Fotos e Itens Indisponíveis
- **Fotos Estáticas:** Permanecem desabilitadas para download (`IsAvailable = false`, desmarcadas por padrão), exibindo aviso explicativo amigável na interface: *"Foto / imagem estática (download de imagens em carrossel planejado para versão futura)"*.
- **Mídias Indisponíveis ou Nulas:** Entradas com dados corrompidos, removidos ou restritos são exibidas como placeholders desabilitados com texto neutro (*"Mídia indisponível ou não suportada"*), sem serem classificadas indevidamente como fotos.
- **Vídeos Válidos:** Permanecem habilitados e selecionados por padrão, prontos para enfileiramento e download.

### 4. Pluralização e Contadores de Interface
- Ajustada a formatação de rótulos e badges para concordância gramatical correta:
  - Exemplos: `"2 mídias • 1 vídeo"`, `"1 de 1 vídeo selecionado"`, `"Total: 2 mídias (1 vídeo suportado, 1 foto)"`.

### 5. Estabilidade das Plataformas YouTube e X/Twitter
- O pipeline de download de vídeos, áudios e playlists do YouTube e publicações do X/Twitter não sofreu qualquer alteração, mantendo 100% de estabilidade e compatibilidade.

---

## 🧪 Qualidade e Testes Automatizados

- **Suíte de Testes:** 242 testes unitários e de integração aprovados (0 falhas, 0 pulados).
- **Homologação Real:** Validado em ambiente Windows 11 isolado com a URL real `https://www.instagram.com/p/DdUAEH9lVF8/`, confirmando a correta exibição dos itens, preservação do índice 2, download do vídeo e validação técnica via `ffprobe` (1080x1080, VP9, áudio AAC estéreo).
