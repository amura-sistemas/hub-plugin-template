using Amura.Hub.Plugin.Application.Events;
using Amura.Hub.Plugin.Application.Services;
using Amura.Hub.Plugin.Domain.AggregatesModel.LogAggregate;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Ecommerce.Template.Services;
using Amura.Hub.Plugin.Orders.Abstractions;
using PluginLogger = Amura.Hub.Plugin.Application.Services.ILogger;

namespace Amura.Hub.Plugin.Ecommerce.Template.EventHandlers;

public sealed class OrderEventHandler : IOrderPullHandler
{
    private const string SystemName = "Ecommerce.Template";

    private readonly TemplateConfigurationResolver _configurationResolver;
    private readonly TemplateService _templateService;
    private readonly PluginLogger _logger;

    public OrderEventHandler(
        TemplateConfigurationResolver configurationResolver,
        TemplateService templateService,
        PluginLogger logger)
    {
        _configurationResolver = configurationResolver;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task HandleAsync(OrderEvent notification, CancellationToken cancellationToken = default)
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
        if (!options.HasIntegrationOrdersEnabled)
        {
            return;
        }

        var updatedAtMinUtc = DateTime.UtcNow.AddDays(-Math.Max(1, options.DateFilter));
        var orders = await _templateService.GetOrdersAsync(options, updatedAtMinUtc, cancellationToken);

        await _logger.InsertLogAsync(
            LogLevel.Information,
            $"Template: {orders.Count} pedidos consultados",
            $"Cliente: {customer.Company} (ID: {customer.Id})",
            customer,
            cancellationToken);

        // Substitua este ponto pelo mapeamento para OrderIntegrationDto e gravacao de Import/HistoryOrder.
    }
}
