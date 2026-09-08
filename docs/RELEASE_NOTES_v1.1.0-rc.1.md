# BaixALL 1.1.0-rc.1 — Notas da Versão Candidata

Apresentamos a **Release Candidate 1** da versão **1.1.0** do **BaixALL**. Esta versão é dedicada exclusivamente à consolidação da **nova identidade visual e refinamento de interface**, mantendo o motor de downloads com 100% de estabilidade e integridade funcional em relação à v1.0.0.

> **Importante:** O motor de download, fila assíncrona, conversão via FFmpeg, gerenciamento de ferramentas e histórico continuam com as mesmas capacidades comprovadas da versão 1.0.0. Suporte a playlists não está presente nesta versão (planejado para releases futuras).

---

## 🎨 O que há de novo na Identidade Visual (v1.1.0)

1. **Novo Ícone Oficial Tecnológico:**
   - Geometria moderna com símbolo de reprodução (Play) **100% reto e simétrico** e base/dock com encaixe invertido voltado para baixo.
   - **Compensação Óptica na Header:** A versão reduzida do ícone na barra de navegação superior foi calibrada especialmente com ~2.2px de espessura sólida e alinhamento a pixel (`SnapsToDevicePixels="True"`), eliminando qualquer desfoque subpixel e mantendo peso visual idêntico ao asset mestre.
   - Ícone nativo Windows multi-resolução (`icon.ico` com 7 tamanhos: 16, 24, 32, 48, 64, 128, 256 px).

2. **Tipografia Unificada de Marca (*BaixALL*):**
   - Utilização da fonte moderna `Segoe UI Variable Display` em estilo Italic SemiBold em todo o branding.
   - "Baix" em tom neutro refinado do tema; "**ALL**" destacado em azul dinâmico vibrante (`#0094F0` / `#38BDF8`).

3. **Elementos Decorativos Perimetrais na Home (Watermark):**
   - 8 grupos decorativos emoldurando o perímetro da tela, inspirados em mídia, ondas senoidais de áudio, timelines de edição, fotogramas de película, equalizadores e setas tecnológicas.
   - **Paleta Estritamente Cinza (Sem Azul):**
     - Tema Escuro: Cinza prata tecnológico nítido (`#94A3B8`).
     - Tema Claro: Cinza grafite moderno (`#475569`).

4. **Polimento Geral da Interface:**
   - Barra de rolagem personalizada moderna, fina (8px), com trilho translúcido discreto e sem setas quadradas antigas.
   - Aba "Início" mantida ativada e destacada ao iniciar o aplicativo.
   - Transição suave entre os temas Claro, Escuro e Padrão do Windows.

---

## 📦 Artefatos da Versão Candidata

| Arquivo | Descrição | Tamanho | SHA-256 |
| :--- | :--- | :--- | :--- |
| `BaixALL-Setup-1.1.0-rc.1.exe` | Instalador assistido para Windows x64 (Inno Setup) | 45.857.670 bytes | `429c6e4b7f5c6b581edb32ea826a8e6c2e304a5f679f1e553aba1fc6936307c1` |
| `BaixALL-1.1.0-rc.1-win-x64.zip` | Pacote portátil descompactável (execução direta) | 65.483.370 bytes | `ecbe7da72baa0e638c628c405fde81d76ea047351fc6a8334770610582a2a6f6` |
| `checksums-1.1.0-rc.1.txt` | Hashes de integridade criptográfica SHA-256 | 193 bytes | *(ver arquivo)* |

---

## 🔄 Compatibilidade e Atualização Segura

- **Atualização In-place:** O instalador detecta a versão 1.0.0 instalada e atualiza os binários sem duplicar entradas no Painel de Controle / Configurações do Windows.
- **Preservação de Dados:** As pastas de downloads (`%USERPROFILE%\Downloads`), histórico (`history.json`), preferências (`settings.json`) e ferramentas auxiliares são 100% preservadas.

---

## ⚠️ Avisos de Reputação do Windows SmartScreen

O Windows Defender SmartScreen pode exibir um alerta de reputação (*"O Windows protegeu o seu computador"*) para novos binários de código aberto recém-lançados.
- **Orientações:** Sempre confirme a procedência do download e valide o hash SHA-256 contra o arquivo `checksums-1.1.0-rc.1.txt`.
- O BaixALL não desabilita proteções do sistema operacional e é distribuído com código 100% auditável.
