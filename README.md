# HubPluginTemplate

Template compilavel para criar plugins do Amura Hub usando o SDK `Amura.Hub.Plugin`.

Este repositorio traz um plugin exemplo em `src/Plugins/Amura.Hub.Plugin.Ecommerce.Template/`, pronto para build, testes e empacotamento. Use-o como ponto de partida para uma integracao padrao de Ecommerce ou Hub.

Plugins marketplace, como Mercado Livre, exigem contratos adicionais de OAuth, categorias e notificacoes. Esse padrao nao faz parte desta primeira versao do template.

## Pre-requisitos

Use o SDK .NET definido em `global.json`:

```bash
dotnet --version
```

Versao esperada: `10.0.101`.

O pacote `Amura.Hub.Plugin` e resolvido pelo feed local do repositorio irmao `../HubIntegracao` e pelo `nuget.org`, conforme `NuGet.config`.

Antes de restaurar pacotes, publique o SDK local:

```bash
../HubIntegracao/scripts/publish-local-plugin-feed.sh
```

## Estrutura

```text
.
├── Directory.Build.props
├── Directory.Build.targets
├── HubPluginTemplate.slnx
├── NuGet.config
├── global.json
├── scripts/
│   ├── package-plugins.sh
│   └── verify-plugin-packages.sh
└── src/Plugins/
    ├── Amura.Hub.Plugin.Ecommerce.Template/
    └── Amura.Hub.Plugin.Ecommerce.Template.Tests/
```

O plugin exemplo segue a organizacao padrao:

```text
Amura.Hub.Plugin.Ecommerce.Template/
├── Amura.Hub.Plugin.Ecommerce.Template.csproj
├── plugin.json
├── config.schema.json
├── Configuration/
├── EventHandlers/
├── Models/
├── Module/
└── Services/
```

## Fluxo local

```bash
dotnet restore HubPluginTemplate.slnx
dotnet build HubPluginTemplate.slnx -c Release
dotnet test HubPluginTemplate.slnx
./scripts/package-plugins.sh
./scripts/verify-plugin-packages.sh
```

O build copia a saida do plugin para `artifacts/plugins/<systemName>/stage/`.
O empacotamento gera `artifacts/plugins/<systemName>/<systemName>-<version>.zip`.

## Como criar um plugin real

1. Copie ou renomeie `src/Plugins/Amura.Hub.Plugin.Ecommerce.Template/`.
2. Troque `Template` pelo nome do sistema em namespaces, classes e arquivos.
3. Atualize o `.csproj` para refletir o novo nome da assembly.
4. Atualize `plugin.json`.
5. Atualize `Module/<Sistema>IntegrationModule.cs`.
6. Ajuste `config.schema.json` para expor somente configuracoes administraveis por cliente.
7. Implemente cliente HTTP, modelos e handlers.
8. Adicione ou ajuste testes.
9. Rode build, test, package e verify.

Exemplo de identidade para um novo plugin:

```json
{
  "systemName": "Ecommerce.MinhaLoja",
  "friendlyName": "Minha Loja",
  "group": "Ecommerce",
  "assembly": "Amura.Hub.Plugin.Ecommerce.MinhaLoja.dll",
  "entrypoint": "Amura.Hub.Plugin.Ecommerce.MinhaLoja.Module.MinhaLojaIntegrationModule"
}
```

## Arquivos que precisam ficar alinhados

`plugin.json` e `IntegrationDefinition` devem declarar a mesma identidade operacional:

- `systemName`
- `friendlyName`
- `group`
- `version`
- `description`
- `settingsSectionName`
- `globalConfigurationDefaults`
- `outboundTargets`

O `.csproj` deve manter:

- `TargetFramework` em `net8.0`
- `Nullable` habilitado
- `ImplicitUsings` habilitado
- `EnableDynamicLoading` habilitado
- `PackageReference` para `Amura.Hub.Plugin` usando `$(AmuraHubPluginSdkVersion)`
- copia de `plugin.json` e `config.schema.json` para a saida

Nao declare versao no `.csproj`. A versao de assembly e pacote vem de `plugin.json`, lida por `Directory.Build.targets`.

## Configuracao

Use `config.schema.json` para configuracoes por cliente:

- credenciais do cliente
- URL por cliente quando aplicavel
- flags especificas da integracao
- dados de webhook por cliente

Use `plugin.json` e `IntegrationDefinition.GlobalConfigurationDefaults` para defaults globais:

- endpoints tecnicos
- variaveis globais
- valores padrao administrados fora do cliente

O schema usa o formato v1 do SDK:

- raiz com `version`, `groups` e `fields`
- campos com `key`, `label`, `type`, `required`, `default`, `group`, `hint`, `validation` e `options`
- `default`, nao `defaultValue`

Nao exponha no schema campos comuns do host, como `integrateOrders`, `publicationType` e `publishCategories`, salvo se o contrato do host mudar.

## Outbound HTTP

Plugins nao devem usar `HttpClient` direto. Use `IPluginOutboundClient` com `PluginHttpClient`, como no `TemplateService`.

Declare cada destino em `outboundTargets`:

```json
{
  "name": "main",
  "baseUrl": "https://api.example.com",
  "allowedHosts": ["api.example.com"]
}
```

Se a API usar subdominios por cliente, ajuste `allowedHosts` com o wildcard apropriado e normalize a URL no resolver de configuracao.

## Webhooks

O plugin nao cria rotas HTTP proprias. O host expoe as rotas runtime:

```text
GET  /api/integrations/{systemName}/runtime/webhook/generate
POST /api/webhook/{systemName}/runtime/{webhookId}/{resource}
```

No plugin, implemente `IPluginWebhookHandler` para:

- gerar registro com `PluginWebhookRegistrationFactory`
- resolver cliente pelo `webhookId`
- validar assinatura quando a plataforma envia HMAC
- processar payload ou delegar para um handler existente

## Testes

Para mudancas comportamentais, comece por testes:

- resolucao e normalizacao de configuracao
- autenticacao e headers HTTP
- serializacao e desserializacao
- handlers de pedidos, produtos e webhooks
- regressos de bugs

Convencao de nomes:

```text
Metodo_Cenario_Resultado
```

Exemplo:

```csharp
ResolveAsync_ShouldFallbackToGlobalSettings_WhenCustomerValuesAreMissing
```

## Checklist antes de distribuir

```bash
dotnet test HubPluginTemplate.slnx
./scripts/package-plugins.sh
./scripts/verify-plugin-packages.sh
```

O pacote final precisa conter:

- exatamente um `plugin.json`
- a assembly declarada no manifest
- um arquivo `.deps.json`
- `config.schema.json`, quando o plugin expuser configuracao

Assemblies compartilhadas com o host, como `Amura.Hub.*`, `MediatR*` e `Microsoft.*`, sao excluidas do pacote pelos scripts.
