# BaixALL 1.4.0 — Suporte ao TikTok com Verificação de Marca-d'Água

Apresentamos a versão estável **1.4.0** do **BaixALL**, que introduz o suporte oficial a downloads de vídeos públicos do **TikTok** com classificação estrita de status de marca-d'água (`WatermarkStatus`) e seletores por IDs exatos verificados, mantendo 100% de estabilidade nos fluxos de **YouTube**, **Instagram** e **X/Twitter**.

> **Importante:** O motor de extração baseia-se no `yt-dlp`. O BaixALL não remove, borra, corta ou oculta marca-d'água por pós-processamento de imagem — quando disponível, seleciona diretamente a fonte de reprodução limpa fornecida pela plataforma, sem prometer ausência de marca-d'água quando os metadados não permitirem confirmação positiva.

---

## 🚀 Novidades da Versão 1.4.0

### 1. Suporte ao TikTok com Verificação Estrita de Marca-d'Água (`WatermarkStatus`)
- **Suporte a URLs Públicas do TikTok:** Análise e download de links padrão (`tiktok.com/@usuario/video/ID`) e links curtos redirecionados (`vm.tiktok.com`, `vt.tiktok.com` e `tiktok.com/t/...`).
- **Classificação Honesta em 3 Estados (`WatermarkStatus`):**
  - `NoWatermark` (**Sem marca-d'água**): somente quando há evidência positiva nos metadados do formato (`format_id` de reprodução limpa como `bytevc1_*`, `h264_*` ou `play_addr*`, ou `format_note` indicando explicitamente `no watermark` / `without watermark` / `unwatermarked` / `direct video`) combinada com URL sem indicadores de marca-d'água (`watermark=1`, `wm=1`, `download_addr`, `logo_name`).
  - `Watermarked` (**Com marca-d'água**): quando `format_id` (`download_addr*`, `watermark`), `format_note` ou URL indicam explicitamente a presença de marca-d'água.
  - `Unknown` (**Marca-d'água não verificada**): quando os metadados não trazem evidência conclusiva de ausência nem de presença de marca-d'água (ex.: `format_id` genérico `0`, `mp4` ou `play`), sem jamais promover ausência de informação ou o campo `preference` isoladamente a confirmação positiva.

### 2. Seletores por IDs Exatos e Proteção contra Fallback Não Verificado
- **Opções `Sem marca-d'água` (`NoWatermark`):** Utilizam exclusivamente a lista de `format_id` exatos aprovados na classificação individual (ex.: `bytevc1_1080p_545661-0/bytevc1_1080p_545661-1/h264_540p_365592-0/...`), sem acrescentar seletores genéricos por prefixo ou `best[...]` que pudessem capturar formatos não verificados.
- **Opções `Marca-d'água não verificada` (`Unknown`):** Utilizam exclusivamente os `format_id` exatos classificados como `Unknown`, impedindo que seletores genéricos baixem silenciosamente um stream `download_addr` com marca-d'água.
- **Mapeamento Contextual de Erros:** Caso a fonte limpa expire entre a análise e o download, o BaixALL aborta a tarefa com segurança e exibe mensagem específica para pedidos `NoWatermark` (sem baixar versão com marca-d'água por engano), mantendo mensagem genérica apropriada para solicitações `Unknown` ou `Watermarked`.

### 3. Transparência Visual na Interface, Fila e Histórico
- **Badge Oficial `TikTok`:** Identificação visual na tela de análise, na fila de downloads e no histórico persistente, com chave canônica `tt:ID` para deduplicação independente.
- **Rótulos Claros de Qualidade:** Exibição explícita de `[✓ Sem marca-d'água]`, `[• Marca-d'água não verificada]` e `[⚠ Apenas com marca-d'água]` no seletor de formatos, além de aviso amarelo informativo sempre que uma opção não limpa ou não verificada estiver selecionada.
- **Rejeição Preventiva de Slideshows e Links Não Suportados:** Publicações de fotos (`/photo/`), perfis, hashtags, lives e sons retornam aviso claro em português antes de enfileirar.

---

## 🧪 Qualidade e Testes Automatizados

- **Suíte de Testes:** **307 testes automatizados aprovados** (0 falhas, 0 pulados), incluindo testes determinísticos de contrato com o binário real `yt-dlp --load-info-json --simulate` executados no CI.
- **Homologação Real:** Validado em máquina virtual Windows 11 isolada com links reais do TikTok (padrão e curto), YouTube, Instagram e X/Twitter, além de inspeção técnica de vídeo e áudio via `ffprobe` (`1080x1920`, HEVC + AAC estéreo).

---

## 📦 Arquivos Oficiais da Versão e Hashes SHA-256

| Arquivo | Descrição | Tamanho Exato | Hash SHA-256 |
| :--- | :--- | :--- | :--- |
| `BaixALL-Setup-1.4.0.exe` | Instalador oficial assistido para Windows x64 (Inno Setup 6) | 45.904.840 bytes | `3ee7be401ba46d0a47793cf738b99c2fd1dc864658a48ebf9478c55d1752ac36` |
| `BaixALL-1.4.0-win-x64.zip` | Pacote portátil autônomo (execução direta, win-x64) | 63.459.198 bytes | `6be0502566550efb45948699ba76a44d59dc9e18471df3980128d8102072169a` |
| `checksums-1.4.0.txt` | Manifesto de verificação criptográfica SHA-256 | 182 bytes | `0f12d288b53778972b9c0ee41f5215cebb36a1349e0075c7f216238fbb3cdeb3` |
