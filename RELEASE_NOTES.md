# BaixALL 1.0.0-rc.2 — Release Candidate 2

A **Release Candidate 2** do **BaixALL** traz uma correção pontual e crítica para a inicialização da aplicação em instalações limpas do Windows, além de blindagem completa no sistema de logs.

---

## 📦 Artefatos de Distribuição (RC2)

| Arquivo | Descrição |
| :--- | :--- |
| `BaixALL-Setup-1.0.0-rc.2.exe` | Instalador oficial para Windows 10/11 (x64) com assistente de instalação |
| `BaixALL-1.0.0-rc.2-win-x64.zip` | Pacote portátil (extrair e executar `BaixALL.exe`) |
| `checksums.txt` | Hashes de integridade SHA-256 de todos os arquivos |

---

## 🛠️ Correções Realizadas na RC2

- **Carregamento Seguro de Recursos BAML (`MainWindow`):** Corrigida a declaração do ícone da janela para Pack URI canônico (`pack://application:,,,/Resources/icon.ico`) e embutido o arquivo `Resources/icon.ico` como recurso compilado (`<Resource>`) da aplicação. Isso resolve a exceção `XamlParseException` / `IOException` (`Não é possível localizar o recurso 'resources/icon.ico'`) que impedia a abertura da janela em máquinas sem o ambiente de desenvolvimento.
- **Rastreamento Abrangente de Exceções (`LoggerService`):** O formatador de erros agora percorre recursivamente toda a cadeia de exceções internas (`InnerException`), registrando tipo, mensagem, HResult, origem e stack trace de cada nível, além do `ToString()` integral.
- **Mensagens Amigáveis ao Usuário:** Mensagens de erro fatal agora informam o caminho amigável da pasta de logs (`%LOCALAPPDATA%\BaixALL\logs`) sem poluir a interface visual com stack traces.
- **Teste de Regressão Específico:** Adicionada validação automatizada para garantir que `MainWindow` pode ser instanciada sem dependência de caminhos em disco.

---

## 🔒 Segurança e Privacidade

- **100% Local:** Sem telemetria, rastreamento ou chamadas a servidores de terceiros não autorizados.
- **Desinstalação Segura:** O instalador e o desinstalador nunca excluem seus vídeos baixados ou dados pessoais.
- **Código Aberto:** Licenciado sob a Licença MIT. Detalhes de componentes de terceiros em `THIRD-PARTY-NOTICES.md`.
- **Aviso sobre o Windows SmartScreen:** Por ser um binário de código aberto recém-compilado, o SmartScreen pode exibir um alerta de reputação. Para executar, clique em *"Mais informações"* e *"Executar assim mesmo"*. O projeto não desativa proteções do sistema e trata a assinatura digital como uma etapa opcional de distribuição ampla.
