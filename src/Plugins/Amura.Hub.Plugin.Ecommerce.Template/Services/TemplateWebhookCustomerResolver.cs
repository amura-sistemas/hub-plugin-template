using Amura.Hub.Plugin.Application.Services;
using Amura.Hub.Plugin.Integrations.Abstractions;
using Amura.Hub.Plugin.Webhooks;
using Microsoft.Extensions.Logging;

namespace Amura.Hub.Plugin.Ecommerce.Template.Services;

public sealed class TemplateWebhookCustomerResolver : PluginWebhookCustomerResolverBase
{
    public TemplateWebhookCustomerResolver(
        ICustomerService customerService,
        IIntegrationConfigurationStore configurationStore,
        ILogger<TemplateWebhookCustomerResolver> logger)
        : base("Ecommerce.Template", customerService, configurationStore, logger)
    {
    }
}
