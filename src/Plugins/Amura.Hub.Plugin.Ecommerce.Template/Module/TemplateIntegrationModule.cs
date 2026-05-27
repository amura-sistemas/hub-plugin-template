using Amura.Hub.Plugin.Abstractions;
using Amura.Hub.Plugin.Configuration;
using Amura.Hub.Plugin.Definitions;

namespace Amura.Hub.Plugin.Ecommerce.Template.Module;

public sealed class TemplateIntegrationModule : IIntegrationPlugin
{
    public IntegrationDefinition Definition { get; } = new()
    {
        SystemName = "Ecommerce.Template",
        FriendlyName = "Plugin Template",
        Group = "Ecommerce",
        Version = "0.1.0",
        Author = "Amura",
        Description = "Template de plugin de integracao padrao para o Amura Hub",
        SettingsSectionName = "Template",
        GlobalConfigurationDefaults = new PluginGlobalConfigurationDefaults
        {
            BaseUri = "https://api.example.com",
            Endpoints = new Dictionary<string, string?>
            {
                ["orders"] = "orders",
                ["products"] = "products",
                ["webhooks"] = "webhooks"
            }
        },
        OutboundTargets =
        [
            new PluginOutboundTargetDefinition
            {
                Name = "main",
                BaseUrl = "https://api.example.com",
                AllowedHosts = ["api.example.com"]
            }
        ]
    };
}
