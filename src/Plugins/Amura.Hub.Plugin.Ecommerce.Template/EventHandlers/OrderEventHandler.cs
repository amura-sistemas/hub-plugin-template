using Amura.Hub.Plugin.Application.Events;
using Amura.Hub.Plugin.Application.Services;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Ecommerce.Template.Models;
using Amura.Hub.Plugin.Ecommerce.Template.Services;
using Amura.Hub.Plugin.Domain.AggregatesModel.ImportAggregate;
using Amura.Hub.Plugin.Orders.Abstractions;

namespace Amura.Hub.Plugin.Ecommerce.Template.EventHandlers;

public sealed class OrderEventHandler : IOrderPullHandler
{
    private const string SystemName = "Ecommerce.Template";

    private readonly TemplateConfigurationResolver _configurationResolver;
    private readonly TemplateService _templateService;
    private readonly IPluginOrderImportService _orderImportService;
    private readonly IPluginLogger _logger;

    public OrderEventHandler(
        TemplateConfigurationResolver configurationResolver,
        TemplateService templateService,
        IPluginOrderImportService orderImportService,
        IPluginLogger logger)
    {
        _configurationResolver = configurationResolver;
        _templateService = templateService;
        _orderImportService = orderImportService;
        _logger = logger;
    }

    public async Task HandleAsync(OrderEvent notification, CancellationToken cancellationToken = default)
    {
        var customer = notification.Customer;
        if (customer is null)
        {
            return;
        }

        try
        {
            var options = await _configurationResolver.ResolveAsync(cancellationToken);
            if (!options.HasIntegrationOrdersEnabled)
            {
                return;
            }

            var updatedAtMinUtc = DateTime.UtcNow.AddDays(-Math.Max(1, options.DateFilter));
            var orders = await _templateService.GetOrdersAsync(options, updatedAtMinUtc, cancellationToken);

            var importedCount = 0;
            var skippedCount = 0;

            foreach (var order in orders)
            {
                var orderId = order.Id.Trim();
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    skippedCount++;
                    continue;
                }

                var (integration, status) = TemplateOrderMapper.Build(order, options, customer);
                var registration = await _orderImportService.RegisterAsync(
                    new OrderImportRegistration
                    {
                        ExternalOrderId = orderId,
                        StatusOrder = status,
                        Order = integration
                    },
                    cancellationToken);

                if (registration.Duplicate)
                {
                    skippedCount++;
                    continue;
                }

                importedCount++;
            }

            if (importedCount == 0 && skippedCount == 0)
            {
                return;
            }

            await _logger.WriteAsync(
                PluginLogLevel.Information,
                $"Template: {importedCount} pedidos importados com sucesso",
                $"Cliente: {customer.Company} (ID: {customer.Id}). Ignorados: {skippedCount}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            await _logger.WriteAsync(
                PluginLogLevel.Error,
                "Erro ao processar pedidos Template",
                ex.ToString(),
                cancellationToken);
        }
    }
}
