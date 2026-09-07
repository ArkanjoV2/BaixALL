# BaixALL 1.0.0 — Lançamento Oficial (Versão Estável)

Temos o orgulho de apresentar a **versão 1.0.0 estável oficial** do **BaixALL** para Windows 10 e Windows 11 (x64)!

O BaixALL é um aplicativo desktop nativo para Windows (desenvolvido em C# e .NET 10 LTS com WPF) para download de vídeos e extração de áudios do YouTube com máxima qualidade, preservação de metadados, controle de concorrência e privacidade total.

---

## 📦 Artefatos de Distribuição

| Arquivo | Descrição |
| :--- | :--- |
| `BaixALL-Setup-1.0.0.exe` | Instalador oficial para Windows 10/11 (x64) com assistente de instalação |
| `BaixALL-1.0.0-win-x64.zip` | Pacote portátil (extrair e executar `BaixALL.exe`) |
| `checksums.txt` | Hashes de integridade SHA-256 de todos os arquivos |

---

## 🚀 Destaques da Versão 1.0.0

- **Qualidade Máxima com FFmpeg Merge:** Suporte a vídeos em até 4K 60 FPS com mesclagem transparente de fluxos separados de áudio e vídeo sem recodificação desnecessária.
- **Modo Somente Áudio:** Extraia e converta áudios nos formatos Original, M4A, Opus e MP3 em alta qualidade.
- **Fila Inteligente de Downloads:** Gerenciamento com limite de concorrência (1, 2 ou 3 downloads ativos simultâneos) e despacho automático para itens aguardando.
- **Cancelamento Individual Seguro:** Cancele downloads ativos sem interromper os demais e com limpeza imediata de arquivos temporários.
- **DependencyManager Autônomo:** Instalação e atualização com 1 clique das ferramentas essenciais (`yt-dlp`, `FFmpeg`, `ffprobe`, `Deno`) diretamente dos canais oficiais do GitHub.
- **Histórico Persistente:** Histórico local com ações diretas para reproduzir o arquivo ou abri-lo no Explorador de Arquivos.
- **Self-Contained:** Não exige instalação do .NET Runtime, Visual Studio, Python ou Node.js.

---

## 🔒 Segurança e Privacidade

- **100% Local:** Sem telemetria, rastreamento ou chamadas a servidores de terceiros não autorizados.
- **Desinstalação Segura:** O instalador e o desinstalador nunca excluem seus vídeos baixados ou dados pessoais.
- **Código Aberto:** Licenciado sob a Licença MIT. Detalhes de componentes de terceiros em `THIRD-PARTY-NOTICES.md`.
- **Aviso sobre o Windows SmartScreen:** Por ser um binário de código aberto recém-compilado, o SmartScreen pode exibir um alerta de reputação. Para executar, clique em *"Mais informações"* e *"Executar assim mesmo"*. O projeto não desativa proteções do sistema e trata a assinatura digital como uma etapa opcional de distribuição ampla.
