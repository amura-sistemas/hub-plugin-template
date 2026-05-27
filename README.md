# HubPluginTemplate

Template isolado para criar plugins do Amura Hub usando somente o pacote SDK `Amura.Hub.Plugin` publicado no NuGet.org.

Este repositorio contem um plugin exemplo em `src/Plugins/Amura.Hub.Plugin.Ecommerce.Template/`, pronto para build, testes, empacotamento e upload no Hub. Ele deve servir como base para plugins headless, sem dependencias diretas dos projetos do Hub.

Plugins marketplace, como Mercado Livre, podem exigir contratos adicionais de OAuth, categorias, atributos e notificacoes. Esses contratos existem no SDK, mas este template foca no caso padrao de integracao e-commerce com pedidos, produtos, configuracao por cliente, configuracao global e webhooks.

## Principios

- O plugin deve depender apenas de `Amura.Hub.Plugin` via `PackageReference`.
- Nao referencie projetos do Hub por `ProjectReference`, `HintPath` ou caminho absoluto.
- Nao use APIs proibidas pelo modelo de plugin, como `HttpClient` direto para chamadas externas.
- Toda chamada externa deve passar por `IPluginOutboundClient` ou `PluginHttpClient`.
- `plugin.json`, `config.schema.json` e `IntegrationDefinition` devem estar alinhados.
- O ZIP final deve conter os arquivos de runtime do plugin, mas nao deve carregar assemblies compartilhadas com o host como `Amura.Hub.*`, `MediatR*` e `Microsoft.*`.

## Pre-requisitos

Use o SDK .NET definido em `global.json`:

```bash
dotnet --version
```

Versao esperada neste template:

```text
10.0.101
```

O SDK do plugin e restaurado exclusivamente do NuGet.org:

```bash
dotnet restore HubPluginTemplate.slnx --source https://api.nuget.org/v3/index.json
```

Nao e necessario ter o repositorio `HubIntegracao` ao lado deste template para compilar, testar ou empacotar.

## Politica de versao do SDK

A versao do SDK fica centralizada em `Directory.Build.props`:

```xml
<AmuraHubPluginSdkVersion>0.6.1</AmuraHubPluginSdkVersion>
```

Antes de distribuir um plugin, confirme que esta versao ainda e a mais recente no NuGet.org:

```bash
./scripts/check-sdk-version.sh
```

Se houver uma versao mais nova:

1. Atualize `AmuraHubPluginSdkVersion`.
2. Rode `dotnet restore HubPluginTemplate.slnx`.
3. Rode `dotnet test HubPluginTemplate.slnx`.
4. Rode `./scripts/package-plugins.sh`.
5. Rode `./scripts/verify-plugin-packages.sh`.
6. Teste o ZIP no Hub.

Os pacotes de teste ficam fixos para evitar ruido de migracao. A politica de "manter atualizado" se aplica ao pacote `Amura.Hub.Plugin`.

## Estrutura

```text
.
|-- Directory.Build.props
|-- Directory.Build.targets
|-- HubPluginTemplate.slnx
|-- NuGet.config
|-- global.json
|-- scripts/
|   |-- check-sdk-version.sh
|   |-- package-plugins.sh
|   `-- verify-plugin-packages.sh
`-- src/Plugins/
    |-- Amura.Hub.Plugin.Ecommerce.Template/
    `-- Amura.Hub.Plugin.Ecommerce.Template.Tests/
```

O plugin exemplo segue esta organizacao:

```text
Amura.Hub.Plugin.Ecommerce.Template/
|-- Amura.Hub.Plugin.Ecommerce.Template.csproj
|-- plugin.json
|-- config.schema.json
|-- Configuration/
|-- EventHandlers/
|-- Models/
|-- Module/
`-- Services/
```

## Fluxo local

```bash
dotnet restore HubPluginTemplate.slnx --source https://api.nuget.org/v3/index.json
dotnet build HubPluginTemplate.slnx -c Release
dotnet test HubPluginTemplate.slnx
./scripts/check-sdk-version.sh
./scripts/package-plugins.sh
./scripts/verify-plugin-packages.sh
```

O build copia a saida do plugin para:

```text
artifacts/plugins/<systemName>/stage/
```

O empacotamento gera:

```text
artifacts/plugins/<systemName>/<systemName>-<version>.zip
```

O ZIP e o artefato que deve ser enviado no painel admin do Hub.

## Como criar um plugin real

1. Copie ou renomeie `src/Plugins/Amura.Hub.Plugin.Ecommerce.Template/`.
2. Copie ou renomeie o projeto de testes correspondente.
3. Troque `Template` pelo nome do sistema em namespaces, classes e arquivos.
4. Atualize o `.csproj` para refletir o novo nome da assembly.
5. Atualize `plugin.json`.
6. Atualize `Module/<Sistema>IntegrationModule.cs`.
7. Ajuste `config.schema.json` para expor somente configuracoes por cliente.
8. Ajuste `Configuration/<Sistema>ConfigurationResolver.cs`.
9. Implemente cliente HTTP, modelos e handlers.
10. Adicione ou ajuste testes.
11. Rode build, test, package e verify.

Exemplo de identidade minima:

```json
{
  "systemName": "Ecommerce.MinhaLoja",
  "friendlyName": "Minha Loja",
  "group": "Ecommerce",
  "version": "0.1.0",
  "assembly": "Amura.Hub.Plugin.Ecommerce.MinhaLoja.dll",
  "entrypoint": "Amura.Hub.Plugin.Ecommerce.MinhaLoja.Module.MinhaLojaIntegrationModule",
  "configSchema": "config.schema.json"
}
```

## Arquivos que precisam ficar alinhados

`plugin.json` e `IntegrationDefinition` devem declarar a mesma identidade operacional:

- `systemName`
- `friendlyName`
- `version`
- `group`
- `author`
- `description`
- `settingsSectionName`
- `globalConfigurationDefaults`
- `outboundTargets`

O `.csproj` do plugin deve manter:

- `TargetFramework` em `net8.0`
- `Nullable` habilitado
- `ImplicitUsings` habilitado
- `EnableDynamicLoading` habilitado
- `PackageReference` para `Amura.Hub.Plugin` usando `$(AmuraHubPluginSdkVersion)`
- copia de `plugin.json` e `config.schema.json` para a saida

Nao declare `<Version>` no `.csproj` do plugin. A versao de assembly e pacote vem de `plugin.json`, lida por `Directory.Build.targets`.

## `plugin.json`

`plugin.json` e o manifesto que o Hub usa para descobrir e carregar o plugin.

Campos principais:

| Campo | Obrigatorio | Descricao |
| --- | --- | --- |
| `entrypoint` | Sim | Nome completo da classe que implementa `IIntegrationPlugin`. Nao use `entry`. |
| `assembly` | Sim | Nome da DLL principal do plugin. Deve existir no ZIP. |
| `systemName` | Sim | Identificador estavel do plugin. Ex: `Ecommerce.Template`. |
| `friendlyName` | Sim | Nome exibido no painel. |
| `version` | Sim | Versao do artefato. Tambem define a versao de assembly. |
| `group` | Sim | Agrupamento no catalogo. Ex: `Ecommerce`. |
| `author` | Recomendado | Autor ou mantenedor. |
| `description` | Recomendado | Descricao exibida no painel. |
| `configSchema` | Opcional | Caminho do schema de configuracao por cliente. Normalmente `config.schema.json`. |
| `settingsSectionName` | Recomendado | Nome logico da secao de settings globais do plugin. |
| `globalConfigurationDefaults` | Recomendado | Defaults globais usados pelo Hub para `BaseUri` e variaveis. |
| `outboundTargets` | Sim para HTTP externo | Allowlist de destinos externos usados por `IPluginOutboundClient`. |

Exemplo completo:

```json
{
  "entrypoint": "Amura.Hub.Plugin.Ecommerce.Template.Module.TemplateIntegrationModule",
  "assembly": "Amura.Hub.Plugin.Ecommerce.Template.dll",
  "systemName": "Ecommerce.Template",
  "friendlyName": "Plugin Template",
  "version": "0.1.0",
  "group": "Ecommerce",
  "author": "Amura",
  "description": "Template de plugin de integracao padrao para o Amura Hub",
  "configSchema": "config.schema.json",
  "settingsSectionName": "Template",
  "globalConfigurationDefaults": {
    "baseUri": "https://api.example.com",
    "endpoints": {
      "orders": "orders",
      "products": "products",
      "webhooks": "webhooks"
    }
  },
  "outboundTargets": [
    {
      "name": "main",
      "baseUrl": "https://api.example.com",
      "allowedHosts": ["api.example.com"]
    }
  ]
}
```

### `globalConfigurationDefaults`

Use para valores globais administrados pelo Hub, nao por cliente:

- `baseUri`: URL base padrao da API externa.
- `variables`: mapa livre de variaveis globais.
- `userAgent`: alias legado para `variables.userAgent`.
- `endpoints`: alias legado para endpoints tecnicos; o Hub normaliza para variaveis globais.

Exemplo:

```json
{
  "globalConfigurationDefaults": {
    "baseUri": "https://api.example.com",
    "variables": {
      "userAgent": "MinhaLojaHub/1.0",
      "orders": "orders",
      "products": "products"
    }
  }
}
```

### `outboundTargets`

Cada destino externo deve ser declarado:

| Campo | Obrigatorio | Descricao |
| --- | --- | --- |
| `name` | Sim | Nome usado pelo `PluginHttpClient`. Ex: `main`. |
| `baseUrl` | Sim | URL base permitida. |
| `baseUrlSettingKey` | Opcional | Chave global que pode substituir `baseUrl`. Normalmente `BaseUri`. |
| `allowedHosts` | Recomendado | Hosts permitidos. Use nomes exatos quando possivel. |
| `timeoutSeconds` | Opcional | Timeout por requisicao. Default do SDK: 30. |
| `retryCount` | Opcional | Tentativas de retry. Default do SDK: 2. |
| `authenticationKind` | Opcional | Tipo de autenticacao injetada pelo host quando aplicavel. |
| `sensitiveHeaders` | Opcional | Headers sensiveis que nao devem vazar em logs. |

Prefira `allowedHosts` exatos. Use wildcard apenas quando a plataforma realmente usa subdominios por cliente.

## `config.schema.json`

`config.schema.json` descreve somente configuracao por cliente. O Hub usa esse arquivo para renderizar o formulario de instalacao/edicao do plugin.

Formato aceito pelo template:

```json
{
  "version": 1,
  "groups": [
    { "name": "auth", "label": "Autenticacao" }
  ],
  "fields": [
    {
      "key": "apiToken",
      "label": "Token da API",
      "type": "secret",
      "required": true,
      "group": "auth"
    }
  ]
}
```

Regras importantes:

- `fields` deve ficar na raiz.
- `groups` deve conter apenas metadados de grupos, como `name` e `label`.
- Nao use `groups[].fields`; esse formato nao e renderizado pelo Hub.
- Use `default`, nao `defaultValue`.
- Use `hint`, nao `description`, no formato SDK v1.
- Use `type: "secret"` para senhas, tokens e chaves.
- Nao declare `baseUri`, `apiUrl`, `userAgent`, `Variables:*` ou `endPoint*` no schema por cliente; esses valores pertencem ao escopo global/admin.
- Nao declare os campos comuns do Hub: `integrateOrders`, `publicationType`, `publishCategories`.

### Campos permitidos

| Propriedade | Obrigatoria | Descricao |
| --- | --- | --- |
| `key` | Sim | Chave persistida. Deve ser estavel entre versoes. |
| `label` | Sim | Rotulo exibido no painel. |
| `type` | Sim | Tipo visual e de validacao. |
| `required` | Sim | Define obrigatoriedade para campos nao booleanos. |
| `default` | Opcional | Valor inicial em string. Para bool use `"true"` ou `"false"`. |
| `group` | Opcional | Nome de um grupo declarado em `groups`. |
| `hint` | Opcional | Ajuda curta exibida no formulario. |
| `validation` | Opcional | Regras de validacao. |
| `options` | Opcional | Opcoes para `enum`. |

Tipos aceitos:

- `string`
- `int`
- `bool`
- `decimal`
- `date`
- `enum`
- `secret`

Validacoes aceitas:

```json
{
  "validation": {
    "regex": "^[A-Z0-9]+$",
    "min": 1,
    "max": 500,
    "minLength": 3,
    "maxLength": 100
  }
}
```

Campo enum:

```json
{
  "key": "environment",
  "label": "Ambiente",
  "type": "enum",
  "required": true,
  "default": "production",
  "options": [
    { "value": "sandbox", "label": "Sandbox" },
    { "value": "production", "label": "Producao" }
  ]
}
```

## Escopos de configuracao

O Hub separa configuracao global e configuracao por cliente.

Configuracao global:

- Disponivel por `systemName`.
- Administrada em area admin.
- Usada para `BaseUri` e variaveis tecnicas.
- Pode ser criada automaticamente a partir de `globalConfigurationDefaults`.

Configuracao por cliente:

- Persistida por `customerId + systemName`.
- Renderizada a partir de `config.schema.json`.
- Deve conter credenciais e preferencias especificas daquele cliente.

O resolver do template aceita aliases legados, como `baseUri`, `apiUrl`, `hasIntegrationOrdersEnabled` e `simpleProduct`, para manter compatibilidade com configuracoes antigas. Novos plugins devem seguir o contrato atual.

## Resolver de configuracao

`TemplateConfigurationResolver` combina:

1. Configuracao por cliente vinda de `IIntegrationConfigurationStore`.
2. Settings globais vindos de `IPluginSettingsAccessor`.
3. Defaults seguros do plugin.

Prioridade recomendada:

1. Valor por cliente, quando for realmente uma configuracao por cliente.
2. Valor global/admin.
3. Default do plugin.

`baseUri` permanece aceito por cliente apenas por compatibilidade legada. Para novos plugins, declare `baseUri` em `globalConfigurationDefaults` e leia via `PluginGlobalSettings.GetBaseUri` ou `IPluginSettingsAccessor`.

## Outbound HTTP

Nao use `HttpClient` direto. Use `PluginHttpClient`:

```csharp
public TemplateService(IPluginOutboundClient outboundClient, ILogger<TemplateService> logger)
{
    _httpClient = new PluginHttpClient(outboundClient, "main");
    _logger = logger;
}
```

Configure o cliente com a URL normalizada do resolver:

```csharp
_httpClient.BaseAddress = new Uri(options.BaseUri, UriKind.Absolute);
```

Regras:

- O target usado no codigo precisa existir em `outboundTargets`.
- A URL final precisa bater com `allowedHosts`.
- Headers sensiveis devem ser declarados em `sensitiveHeaders` quando necessario.
- Credenciais sensiveis nao devem ser logadas.

## Webhooks

O plugin nao registra controllers, Minimal APIs ou workers no host. O Hub expoe rotas runtime:

```text
GET  /api/integrations/{systemName}/runtime/webhook/generate
POST /api/webhook/{systemName}/runtime/{webhookId}/{resource}
```

Implemente `IPluginWebhookHandler` para:

- gerar registro com `PluginWebhookRegistrationFactory`;
- resolver cliente pelo `webhookId`;
- validar assinatura quando a plataforma envia HMAC;
- processar payload ou delegar para um handler existente;
- retornar `PluginWebhookResult` adequado.

## Empacotamento

`Directory.Build.targets` monta o stage automaticamente apos o build:

```text
artifacts/plugins/<systemName>/stage/
```

`scripts/package-plugins.sh` copia somente os arquivos necessarios para:

```text
artifacts/plugins/<systemName>/package/
```

e gera:

```text
artifacts/plugins/<systemName>/<systemName>-<version>.zip
```

Comandos:

```bash
./scripts/package-plugins.sh
./scripts/package-plugins.sh --no-build
CONFIGURATION=Debug ./scripts/package-plugins.sh
SOLUTION_FILE=/caminho/para/Outra.slnx ./scripts/package-plugins.sh
```

`scripts/verify-plugin-packages.sh` valida:

- exatamente um `plugin.json`;
- nome do ZIP como `<systemName>-<version>.zip`;
- diretorio esperado em `artifacts/plugins/<systemName>`;
- assembly declarada no manifesto;
- arquivo `.deps.json`;
- arquivo `.runtimeconfig.json`;
- `configSchema` declarado, quando existir.

## Checklist antes de distribuir

```bash
dotnet restore HubPluginTemplate.slnx --source https://api.nuget.org/v3/index.json
dotnet test HubPluginTemplate.slnx
./scripts/check-sdk-version.sh
./scripts/package-plugins.sh
./scripts/verify-plugin-packages.sh
unzip -Z1 artifacts/plugins/Ecommerce.Template/Ecommerce.Template-0.1.0.zip
```

O ZIP final deve conter:

- `plugin.json`
- `config.schema.json`, quando `configSchema` for declarado
- assembly principal declarada em `plugin.json`
- `.deps.json`
- `.runtimeconfig.json`
- dependencias privadas do plugin

## Erros comuns

### Configuracao nao aparece no Hub

Verifique:

- `plugin.json` contem `configSchema`.
- O arquivo declarado em `configSchema` existe no ZIP.
- `config.schema.json` tem `fields` na raiz.
- Os campos nao sao admin-only, como `baseUri`, `apiUrl`, `userAgent`, `Variables:*` ou `endPoint*`.
- Os campos nao duplicam os comuns do host: `integrateOrders`, `publicationType`, `publishCategories`.

### Plugin aparece, mas runtime nao carrega

Verifique:

- Use `entrypoint`, nao `entry`.
- A classe do `entrypoint` implementa `IIntegrationPlugin`.
- A DLL declarada em `assembly` existe no ZIP.
- `assembly` e namespace foram atualizados apos renomear o template.
- O ZIP contem `.deps.json` e `.runtimeconfig.json`.

### Chamada HTTP externa falha

Verifique:

- O target usado no `PluginHttpClient` existe em `outboundTargets`.
- `allowedHosts` contem o host real da URL final.
- `baseUrl` ou `BaseUri` global estao corretos.
- A URL relativa enviada ao client nao tenta escapar para outro host.

### Build usa pacote antigo do SDK

Rode:

```bash
./scripts/check-sdk-version.sh
dotnet list HubPluginTemplate.slnx package --outdated --source https://api.nuget.org/v3/index.json
```

Atualize `AmuraHubPluginSdkVersion` em `Directory.Build.props` quando houver nova versao do SDK.

## Testes

Para mudancas comportamentais, comece por testes:

- resolucao e normalizacao de configuracao;
- fallback de settings globais;
- aliases legados;
- autenticacao e headers HTTP;
- serializacao e desserializacao;
- handlers de pedidos, produtos e webhooks;
- regressos de bugs.

Convencao de nomes:

```text
Metodo_Cenario_Resultado
```

Exemplo:

```csharp
ResolveAsync_ShouldFallbackToGlobalSettings_WhenCustomerValuesAreMissing
```

## Publicacao no Hub

1. Gere o ZIP com `./scripts/package-plugins.sh`.
2. Valide com `./scripts/verify-plugin-packages.sh`.
3. Acesse o painel admin do Hub.
4. Envie o ZIP na tela de plugins.
5. Configure a configuracao global/admin se necessario.
6. Abra um cliente e instale o plugin.
7. Confirme que os campos de `config.schema.json` aparecem no formulario.
8. Execute um fluxo real ou smoke test do plugin.
