# Guia de Manutenção e Ciclo de Vida do BaixALL

Este documento orienta os mantenedores e colaboradores sobre o fluxo seguro de desenvolvimento, testes, versionamento e publicação de atualizações para o BaixALL.

---

## 🔄 Fluxo de Trabalho para Correções e Novas Versões

Para manter a estabilidade do produto e a integridade da suíte de testes, todo desenvolvimento deve seguir o ciclo abaixo:

1. **Recebimento:** Analise o chamado aberto nas [Issues do GitHub](https://github.com/ArkanjoV2/BaixALL/issues) (relato de bug ou sugestão de funcionalidade).
2. **Reprodução:** Antes de alterar código, reproduza o comportamento relatado em ambiente controlado ou identifique a falha via teste unitário/integração.
3. **Criação de Branch:** Crie uma branch específica a partir da `main` atualizada (ex.: `fix/correcao-cancelamento`, `feat/novo-formato`, `chore/atualizacao-ferramentas`).
4. **Correção Pontual:** Realize alterações localizadas e estritamente necessárias. Evite refatorações cosméticas em código estável.
5. **Teste de Regressão:** Adicione pelo menos um teste automatizado no projeto `BaixALL.Tests` que comprove a resolução do problema e impeça regressões futuras.
6. **Abertura de Pull Request:** Abra um Pull Request direcionado à branch `main`, descrevendo a motivação, o impacto da mudança e os testes executados.
7. **Validação no CI:** Aguarde a execução do workflow de Integração Contínua (GitHub Actions). O PR somente deve ser aceito se todos os testes forem aprovados e o build self-contained for bem-sucedido.
8. **Revisão e Merge:** Revise as alterações e realize o merge para a branch `main`.
9. **Release de Manutenção:** Quando houver um conjunto coeso de correções ou novidades aprovadas, prepare formalmente a próxima release.

---

## 🏷️ Convenção de Versionamento Semântico

O BaixALL adota uma convenção simples de versionamento semântico (`MAJOR.MINOR.PATCH`):

- **`1.0.x` (Patch — ex.: `1.0.1`, `1.0.2`):**
  - Correções de bugs, ajustes de compatibilidade com novas versões do `yt-dlp` ou correções de interface/fila que não alterem o comportamento fundamental.
- **`1.x.0` (Minor — ex.: `1.1.0`, `1.2.0`):**
  - Novas funcionalidades retrocompatíveis (ex.: suporte a novos provedores, novas opções de conversão ou filtros adicionais).
- **`2.0.0` (Major):**
  - Mudanças arquiteturais profundas, reformulações estruturais ou alterações incompatíveis relevantes.

> ⚠️ **Importante:** Alterações exclusivas de documentação, README, imagens ou arquivos de CI **não devem** gerar novas versões do executável ou tags de release.

---

## 🛡️ Integração Contínua (CI) e Critérios de Qualidade

O repositório possui validação automática via GitHub Actions (`.github/workflows/ci.yml`):
- **Ambiente:** Windows (`windows-latest`) com SDK oficial .NET 10.
- **Etapas Obrigatórias:**
  1. Restauração limpa de pacotes NuGet (`dotnet restore`).
  2. Compilação da solução em modo Release (`dotnet build -c Release --no-restore`).
  3. Execução completa dos testes com diagnóstico de travamentos (`--blame-hang --blame-hang-timeout 3m`).
  4. Validação de empacotamento autônomo (`dotnet publish -c Release -r win-x64 --self-contained`).

### Diagnóstico de Falhas
Em caso de falha de teste ou travamento, os artefatos com relatórios `.trx` e despejos de processo são armazenados na aba *Actions* do GitHub para inspeção detalhada.
