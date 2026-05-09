# LumiFinder — Política de privacidade

**Última atualização: 9 de maio de 2026**

<p align="center">
  <a href="../PRIVACY.md">English</a> |
  <a href="PRIVACY.ko.md">한국어</a> |
  <a href="PRIVACY.ja.md">日本語</a> |
  <a href="PRIVACY.zh-CN.md">简体中文</a> |
  <a href="PRIVACY.zh-TW.md">繁體中文</a> |
  <a href="PRIVACY.de.md">Deutsch</a> |
  <a href="PRIVACY.es.md">Español</a> |
  <a href="PRIVACY.fr.md">Français</a> |
  <strong>Português</strong>
</p>

---

## Visão geral

LumiFinder ("o App") é uma aplicação de explorador de arquivos para Windows desenvolvida pela LumiBear Studio. Estamos comprometidos em proteger sua privacidade. Esta política explica quais dados coletamos, como os protegemos e como você pode controlá-los.

## Dados que coletamos

### Relatórios de falha (Sentry)

O App usa o [Sentry](https://sentry.io) para relatórios automáticos de falha. Quando o App falha ou encontra um erro não tratado, os seguintes dados **podem** ser enviados:

- **Detalhes do erro**: Tipo de exceção, mensagem e stack trace
- **Informações do dispositivo**: Versão do SO, arquitetura da CPU, uso de memória no momento da falha
- **Informações do app**: Versão do app, versão do runtime, configuração de build

Os relatórios de falha são usados **apenas** para identificar e corrigir bugs. Eles **não** incluem:

- Nomes de arquivos, nomes de pastas ou conteúdo de arquivos
- Informações de conta de usuário
- Histórico de navegação ou caminhos de navegação
- Qualquer informação pessoalmente identificável (PII)

### Proteções de privacidade nos relatórios de falha

Antes de qualquer relatório de falha ser enviado, várias camadas de limpeza de PII são aplicadas automaticamente:

- **Mascaramento de nome de usuário** — Caminhos de pasta de usuário do Windows (`C:\Users\<seu-nome-de-usuário>\...`) são detectados e a parte do nome de usuário é substituída antes da transmissão. O mesmo se aplica a caminhos UNC (`\\servidor\compartilhado\Users\<nome-de-usuário>\...`).
- **`SendDefaultPii = false`** — A coleta automática do SDK do Sentry de endereços IP, nomes de servidores e identificadores de usuário está totalmente desativada.
- **Sem conteúdo de arquivos** — Stack traces nunca contêm conteúdo de arquivos ou pastas; apenas números de linha e nomes de métodos.

Você pode verificar a implementação por si mesmo no código aberto:
[`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

### Configurações locais

O App armazena preferências do usuário (tema, idioma, pastas recentes, favoritos, cor de destaque personalizada, etc.) localmente em seu dispositivo usando `ApplicationData.LocalSettings` do Windows. Esses dados **nunca** são transmitidos para qualquer servidor.

## Dados que NÃO coletamos

- Sem informações pessoais (nome, e-mail, endereço)
- Sem conteúdo do sistema de arquivos ou metadados de arquivos
- Sem analítica de uso ou telemetria
- Sem dados de localização
- Sem identificadores de publicidade
- Sem dados compartilhados com terceiros para marketing

## Acesso à rede

O App requer acesso à internet apenas para:

- **Relatórios de falha** (Sentry) — relatórios de erro automáticos, pode ser desativado (veja "Como optar por não participar" abaixo)
- **Conexões FTP / FTPS / SFTP** — apenas quando explicitamente configuradas pelo usuário
- **Restauração de pacotes NuGet** — apenas durante builds de desenvolvimento (não executa para usuários finais)

## Como optar por não receber relatórios de falha

Os relatórios de falha podem ser desativados diretamente do App sem desconectar da internet:

1. Abra **Configurações** (canto inferior esquerdo da barra lateral)
2. Navegue até **Avançado**
3. Desative **Relatórios de falha**

A alteração entra em vigor imediatamente. Após optar por não participar, nenhum relatório de falha será enviado sob qualquer circunstância. Relatórios passados já nos servidores do Sentry ainda expirarão de acordo com o cronograma padrão de retenção de 90 dias.

## Armazenamento e retenção de dados

- **Servidores Sentry**: Relatórios de falha são armazenados no datacenter de **Frankfurt, Alemanha (UE)** da Sentry — escolhido para conformidade com o GDPR. Relatórios são automaticamente excluídos após **90 dias**.
- **Configurações locais**: Armazenadas apenas em seu dispositivo. Removidas ao desinstalar o App.

## Sentry como Operador de Dados (GDPR)

A Sentry atua como Operador de Dados (Data Processor) para relatórios de falha sob o GDPR. Para detalhes sobre as práticas de privacidade e medidas de segurança da Sentry:

- **Política de privacidade da Sentry**: [https://sentry.io/privacy/](https://sentry.io/privacy/)
- **Segurança da Sentry**: [https://sentry.io/security/](https://sentry.io/security/)
- **GDPR da Sentry**: [https://sentry.io/legal/dpa/](https://sentry.io/legal/dpa/)

A LumiBear Studio revisou os termos de processamento de dados da Sentry e selecionou a região da UE (`o4510949994266624.ingest.de.sentry.io`) para garantir que os dados de falha não saiam do Espaço Econômico Europeu sem salvaguardas apropriadas.

## Privacidade de crianças

O App não coleta deliberadamente dados de crianças menores de 13 anos. O App não é direcionado a crianças e não coleta nenhuma informação pessoal que possa identificar uma criança.

## Seus direitos

Como não coletamos dados pessoais, não há dados pessoais para acessar, modificar ou excluir. Especificamente:

- **Direito de acesso / portabilidade**: Não aplicável — nenhum dado pessoal é mantido por nós.
- **Direito de exclusão**: Não aplicável — nenhum dado pessoal é mantido por nós. Configurações locais podem ser removidas desinstalando o App.
- **Direito de optar por não participar**: Disponível em Configurações > Avançado > Relatórios de falha (veja "Como optar por não participar" acima).

## Código aberto

LumiFinder é código aberto sob a licença GPL v3.0. Você é bem-vindo a inspecionar, auditar ou modificar o código por si mesmo:

- **Código-fonte**: [https://github.com/LumiBearStudio/LumiFinder](https://github.com/LumiBearStudio/LumiFinder)
- **Bibliotecas open-source utilizadas**: Veja [OpenSourceLicenses.md](../OpenSourceLicenses.md)

## Contato

Se você tiver perguntas sobre esta política de privacidade, encontrou uma violação ou deseja exercer os direitos descritos acima:

- **Issues do GitHub**: [https://github.com/LumiBearStudio/LumiFinder/issues](https://github.com/LumiBearStudio/LumiFinder/issues)
- **Divulgação de segurança**: Veja [SECURITY.md](../SECURITY.md)

## Mudanças nesta política

Podemos atualizar esta política de tempos em tempos à medida que o App evolui ou os requisitos legais mudam. Mudanças materiais serão anunciadas via GitHub Releases. Cada atualização incrementa a data de "Última atualização" no topo deste documento. O histórico de versões está permanentemente disponível no [histórico do Git](https://github.com/LumiBearStudio/LumiFinder/commits/main/PRIVACY.md).
