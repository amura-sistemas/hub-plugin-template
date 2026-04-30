using Amura.Hub.Plugin.Application.Events;
using Amura.Hub.Plugin.Application.Services;
using Amura.Hub.Plugin.Domain.AggregatesModel.LogAggregate;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Products.Abstractions;
using PluginLogger = Amura.Hub.Plugin.Application.Services.ILogger;

namespace Amura.Hub.Plugin.Ecommerce.Template.EventHandlers;

public sealed class ProductEventHandler : IProductPublishHandler
{
    private const string SystemName = "Ecommerce.Template";

    private readonly TemplateConfigurationResolver _configurationResolver;
    private readonly PluginLogger _logger;

    public ProductEventHandler(
        TemplateConfigurationResolver configurationResolver,
        PluginLogger logger)
    {
        _configurationResolver = configurationResolver;
        _logger = logger;
    }

    public async Task HandleAsync(ProductEvent notification, CancellationToken cancellationToken = default)
    {
        if (notification.Customer is null)
        {
            return;
        }

        var customer = notification.Customer;
        var plugin = customer.Plugins.FirstOrDefault(p =>
            p.IsEnabled &&
            p.SystemName.Equals(SystemName, StringComparison.OrdinalIgnoreCase));

        if (plugin is null)
        {
            return;
        }

        var options = await _configurationResolver.ResolveAsync(customer.Id, cancellationToken);
        if (!options.HasIntegrationProductsEnabled)
        {
            return;
        }

        await _logger.InsertLogAsync(
            LogLevel.Information,
            $"Template: {notification.Products.Count} produtos recebidos para publicacao",
            $"Cliente: {customer.Company} (ID: {customer.Id})",
            customer,
            cancellationToken);

        // Substitua este ponto por busca/criacao/atualizacao de produtos na API externa.
    }
}
