<h1 align="center">
  LumiFinder
</h1>

<p align="center">
  <strong>As Miller Columns do Finder do macOS, reimaginadas para Windows.</strong><br>
  Para usuários avançados que migraram para Windows mas nunca pararam de sentir falta da visualização em colunas.
</p>

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/releases/latest"><img src="https://img.shields.io/github/v/release/LumiBearStudio/LumiFinder?style=for-the-badge&label=Latest" alt="Última versão"></a>
  <a href="../LICENSE"><img src="https://img.shields.io/github/license/LumiBearStudio/LumiFinder?style=for-the-badge" alt="Licença"></a>
  <a href="https://github.com/sponsors/LumiBearStudio"><img src="https://img.shields.io/badge/Sponsor-%E2%9D%A4-ff69b4?style=for-the-badge&logo=github-sponsors" alt="Patrocinar"></a>
</p>

<p align="center">
  <a href="../README.md">English</a> |
  <a href="README.ko.md">한국어</a> |
  <a href="README.ja.md">日本語</a> |
  <a href="README.zh-CN.md">简体中文</a> |
  <a href="README.zh-TW.md">繁體中文</a> |
  <a href="README.de.md">Deutsch</a> |
  <a href="README.es.md">Español</a> |
  <a href="README.fr.md">Français</a> |
  <strong>Português</strong>
</p>

---

![Navegação com Miller Columns](miller-columns.gif)

> **Navegue hierarquias de pastas como elas deveriam ser navegadas.**
> Clique numa pasta, e o seu conteúdo aparece na próxima coluna. Você sempre vê onde está, de onde veio e para onde está indo — tudo de uma vez. Sem mais cliques de avançar e voltar.

![LumiFinder — Miller Columns em ação](hero.png)

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/stargazers"><img src="https://img.shields.io/github/stars/LumiBearStudio/LumiFinder?style=social" alt="GitHub Stars"></a>
</p>
<p align="center">
  Se o LumiFinder lhe for útil, considere dar uma ⭐ — ajuda outras pessoas a descobrirem este projeto!
</p>

---

## Por que LumiFinder?

| | Explorador do Windows | LumiFinder |
|---|---|---|
| **Miller Columns** | Não | Sim — navegação hierárquica multi-coluna |
| **Multi-aba** | Apenas Windows 11 (básico) | Abas completas com destacamento, redocking, duplicação, restauração de sessão |
| **Visualização dividida** | Não | Painel duplo com modos de visualização independentes |
| **Painel de prévia** | Básico | Mais de 10 tipos de arquivo — imagens, vídeo, áudio, código, hex, fontes, PDF |
| **Navegação por teclado** | Limitada | Mais de 30 atalhos, busca type-ahead, design teclado-primeiro |
| **Renomear em lote** | Não | Regex, prefixo/sufixo, numeração sequencial |
| **Desfazer/Refazer** | Limitado | Histórico completo de operações (profundidade configurável) |
| **Cor de destaque personalizada** | Não | 10 cores predefinidas + tema claro/escuro/sistema |
| **Densidade de layout** | Não | 6 níveis de altura de linha + escala de fonte/ícone independente |
| **Conexões remotas** | Não | FTP, FTPS, SFTP com credenciais salvas |
| **Espaços de trabalho** | Não | Salve e restaure layouts de abas nomeados instantaneamente |
| **Prateleira de arquivos** | Não | Área de trânsito drag & drop estilo Yoink |
| **Status da nuvem** | Sobreposição básica | Selos de sincronização em tempo real (OneDrive, iCloud, Dropbox) |
| **Velocidade de inicialização** | Lento em diretórios grandes | Carregamento assíncrono com cancelamento — sem atrasos |

---

## Recursos

### Miller Columns — Veja tudo de uma vez

Navegue hierarquias profundas de pastas sem perder o contexto. Cada coluna representa um nível — clique numa pasta e seu conteúdo aparece na próxima coluna. Você sempre vê onde está e de onde veio.

- Separadores de coluna arrastáveis para larguras personalizadas
- Auto-equalização de colunas (Ctrl+Shift+=) ou auto-ajuste ao conteúdo (Ctrl+Shift+-)
- Rolagem horizontal suave que mantém a coluna ativa visível
- Layout estável — sem tremor de rolagem na navegação por teclado ↑/↓

### Quatro modos de visualização

- **Miller Columns** (Ctrl+1) — Navegação hierárquica, a marca do LumiFinder
- **Detalhes** (Ctrl+2) — Tabela ordenável com nome, data, tipo, tamanho
- **Lista** (Ctrl+3) — Layout multi-coluna denso para escanear diretórios grandes
- **Ícones** (Ctrl+4) — Visualização em grade com 4 tamanhos até 256×256

![Modo Detalhes com aba Lixeira](details.png)

### Multi-aba com restauração completa de sessão

- Abas ilimitadas, cada uma com seu próprio caminho, modo de visualização e histórico
- **Destacar e redocar abas**: Arraste uma aba para fora para criar uma nova janela, arraste-a de volta para redocar — indicador fantasma estilo Chrome e feedback de janela semi-transparente
- **Duplicação de aba**: Clone uma aba com seu caminho e configurações exatos
- Salvamento automático de sessão: Feche o app, reabra — cada aba exatamente onde você deixou

### Visualização dividida — Painel duplo verdadeiro

![Visualização dividida com Miller Columns + Prévia de código](2.png)

- Navegação lado a lado com navegação independente por painel
- Cada painel pode usar um modo de visualização diferente (Miller esquerda, Detalhes direita)
- Painéis de prévia separados para cada painel
- Arraste arquivos entre painéis para copiar/mover

### Painel de prévia — Saiba antes de abrir

Pressione **Espaço** para Quick Look (estilo Finder do macOS):

- **Navegação com setas e Espaço**: Navegue arquivos sem fechar o Quick Look
- **Persistência do tamanho da janela**: Quick Look lembra o último tamanho
- **Imagens**: JPEG, PNG, GIF, BMP, WebP, TIFF com resolução e metadados
- **Vídeo**: MP4, MKV, AVI, MOV, WEBM com controles de reprodução
- **Áudio**: MP3, AAC, M4A com artista, álbum, duração
- **Texto e código**: Mais de 30 extensões com exibição de sintaxe
- **PDF**: Prévia da primeira página
- **Fontes**: Amostras de glifos com metadados
- **Binário hex**: Visualização de bytes brutos para desenvolvedores
- **Pastas**: Tamanho, contagem de itens, data de criação
- **Hash de arquivo**: Exibição de soma SHA256 com cópia em um clique (ativável em Configurações)

### Design teclado-primeiro

Mais de 30 atalhos de teclado para usuários que mantêm as mãos no teclado:

| Atalho | Ação |
|----------|--------|
| Setas | Navegar colunas e itens |
| Enter | Abrir pasta ou executar arquivo |
| Espaço | Alternar painel de prévia |
| Ctrl+L / Alt+D | Editar barra de endereço |
| Ctrl+F | Buscar |
| Ctrl+C / X / V | Copiar / Recortar / Colar |
| Ctrl+Z / Y | Desfazer / Refazer |
| Ctrl+Shift+N | Nova pasta |
| F2 | Renomear (renomeação em lote com multi-seleção) |
| Ctrl+T / W | Nova aba / Fechar aba |
| Ctrl+Tab / Ctrl+Shift+Tab | Ciclar abas |
| Ctrl+1-4 | Trocar modo de visualização |
| Ctrl+Shift+E | Alternar visualização dividida |
| F6 | Trocar painel de visualização dividida |
| Ctrl+Shift+S | Salvar espaço de trabalho |
| Ctrl+Shift+W | Abrir paleta de espaços de trabalho |
| Ctrl+Shift+H | Alternar extensões de arquivo |
| Shift+F10 | Menu de contexto shell nativo completo |
| Delete | Mover para a Lixeira |

### Temas e personalização

![Configurações — Aparência com destaque personalizado + densidade de layout](4.png)

- Acompanhamento de tema **Claro / Escuro / Sistema**
- **10 cores de destaque predefinidas** — substitua o destaque de qualquer tema com um clique (Lumi Gold padrão)
- **6 níveis de densidade de layout** — XS / S / M / L / XL / XXL alturas de linha
- **Escala de fonte/ícone independente** — separada da densidade de linha
- **9 idiomas**: Inglês, coreano, japonês, chinês (simplificado/tradicional), alemão, espanhol, francês, português (BR)

### Configurações gerais

![Configurações — Geral com visualização dividida + opções de prévia](3.png)

- **Comportamento de inicialização por painel** — Abrir unidade do sistema / Restaurar última sessão / Caminho personalizado, esquerda e direita independentemente
- **Modo de visualização de inicialização** — escolha Miller Columns / Detalhes / Lista / Ícones por painel
- **Painel de prévia** — ativar na inicialização ou alternar sob demanda com Espaço
- **Prateleira de arquivos** — prateleira de trânsito estilo Yoink opcional, com persistência opcional entre sessões
- **Bandeja do sistema** — minimizar para a bandeja em vez de fechar

### Ferramentas de desenvolvedor

- **Selos de status Git**: Modified, Added, Deleted, Untracked por arquivo
- **Visualizador hex dump**: Primeiros 512 bytes em hex + ASCII
- **Integração de terminal**: Ctrl+` abre terminal no caminho atual
- **Conexões remotas**: FTP/FTPS/SFTP com armazenamento criptografado de credenciais

### Integração com armazenamento em nuvem

- **Selos de status de sincronização**: Apenas nuvem, Sincronizado, Upload pendente, Sincronizando
- **OneDrive, iCloud, Dropbox** detectados automaticamente
- **Miniaturas inteligentes**: Usa prévias em cache — nunca aciona downloads indesejados

### Busca inteligente

- **Consultas estruturadas**: `type:image`, `size:>100MB`, `date:today`, `ext:.pdf`
- **Type-ahead**: Comece a digitar em qualquer coluna para filtrar instantaneamente
- **Processamento em segundo plano**: A busca nunca trava a UI

### Espaço de trabalho — Salvar e restaurar layouts de abas

- **Salvar abas atuais**: Clique direito em qualquer aba → "Salvar layout de abas..." ou pressione Ctrl+Shift+S
- **Restaurar instantaneamente**: Clique no botão de espaço de trabalho na barra lateral ou Ctrl+Shift+W
- **Gerenciar espaços de trabalho**: Restaurar, renomear ou excluir layouts salvos do menu de espaços de trabalho
- Perfeito para alternar entre contextos de trabalho — "Desenvolvimento", "Edição de fotos", "Documentos"

### Prateleira de arquivos

- Área de trânsito drag & drop estilo Yoink do macOS
- Arraste arquivos para a Prateleira enquanto navega, solte-os onde precisar
- Excluir um item da Prateleira só remove a referência — seu arquivo original nunca é tocado
- Desativada por padrão — ative em **Configurações > Geral > Lembrar itens da Prateleira**

---

## Desempenho

Projetado para velocidade. Testado com mais de 10.000 itens por pasta.

- E/S assíncrona em todos os lugares — nada bloqueia a thread da UI
- Atualizações de propriedades em lote com sobrecarga mínima
- Seleção com debounce evita trabalho redundante durante navegação rápida
- Cache por aba — troca instantânea de aba, sem re-renderização
- Carregamento concorrente de miniaturas com throttling SemaphoreSlim

---

## Requisitos do sistema

| | |
|---|---|
| **OS** | Windows 10 versão 1903+ / Windows 11 |
| **Arquitetura** | x64, ARM64 |
| **Runtime** | Windows App SDK 1.8 (.NET 8) |
| **Recomendado** | Windows 11 para fundo Mica |

---

## Compilar do código-fonte

```bash
# Pré-requisitos: Visual Studio 2022 com cargas de trabalho .NET Desktop + WinUI 3

# Clonar
git clone https://github.com/LumiBearStudio/LumiFinder.git
cd LumiFinder

# Compilar
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -p:Platform=x64

# Executar testes unitários
dotnet test src/LumiFiles/LumiFiles.Tests/LumiFiles.Tests.csproj -p:Platform=x64
```

> **Nota**: Apps WinUI 3 não podem ser iniciados via `dotnet run`. Use **Visual Studio F5** (empacotamento MSIX necessário).

---

## Contribuir

Encontrou um bug? Tem uma solicitação de recurso? [Abra uma issue](https://github.com/LumiBearStudio/LumiFinder/issues) — todo feedback é bem-vindo.

Veja [CONTRIBUTING.md](../CONTRIBUTING.md) para configuração de build, convenções de código e diretrizes de PR.

---

## Apoie o projeto

Se o LumiFinder melhora seu gerenciamento de arquivos, considere:

- **[Patrocinar no GitHub](https://github.com/sponsors/LumiBearStudio)** — me pague um café, um hambúrguer ou um bife
- **Dê uma ⭐ a este repositório** para ajudar outros a descobri-lo
- **Compartilhe** com colegas que sentem falta do Finder do macOS no Windows
- **Reporte bugs** — cada issue torna o LumiFinder mais estável

---

## Privacidade e telemetria

LumiFinder usa [Sentry](https://sentry.io) **apenas para relatórios de falha** — e você pode desativá-lo.

- **O que coletamos**: Tipo de exceção, stack trace, versão do SO, versão do app
- **O que NÃO coletamos**: Nomes de arquivos, caminhos de pastas, histórico de navegação, informações pessoais
- **Sem analítica de uso, sem rastreamento, sem anúncios**
- Todos os caminhos de arquivos em relatórios de falha são limpos automaticamente antes do envio
- `SendDefaultPii = false` — sem endereços IP ou identificadores de usuário
- **Opt-out**: Configurações > Avançado > Botão "Relatórios de falha" para desativar completamente
- Código aberto — verifique você mesmo em [`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

Veja a [Política de privacidade](../PRIVACY.md) para detalhes completos.

---

## Licença

Este projeto está licenciado sob a [GNU General Public License v3.0](../LICENSE).

**Marca**: O nome "LumiFinder" e o logotipo oficial são marcas comerciais da LumiBear Studio. Forks devem usar um nome e logotipo diferentes. Veja [LICENSE.md](../LICENSE.md) para a política completa de marcas.

---

<p align="center">
  <a href="https://github.com/sponsors/LumiBearStudio">Patrocinar</a> ·
  <a href="../PRIVACY.md">Política de privacidade</a> ·
  <a href="../OpenSourceLicenses.md">Licenças open source</a> ·
  <a href="https://github.com/LumiBearStudio/LumiFinder/issues">Relatórios de bugs e solicitações de recursos</a>
</p>
