using System.Text.Json;
using Amura.Hub.Plugin.Application.Events;
using Amura.Hub.Plugin.Application.Services;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Ecommerce.Template.Models;
using Amura.Hub.Plugin.Domain.AggregatesModel.ImportAggregate;
using Amura.Hub.Plugin.Webhooks;
using Amura.Hub.Plugin.Webhooks.Abstractions;

namespace Amura.Hub.Plugin.Ecommerce.Template.EventHandlers;

public sealed class TemplateWebhookHandler : IPluginWebhookHandler
{
    private const string SignatureHeader = "X-Template-Signature";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TemplateConfigurationResolver _configurationResolver;
    private readonly IPluginOrderImportService _orderImportService;
    private readonly IPluginLogger _logger;

    public TemplateWebhookHandler(
        TemplateConfigurationResolver configurationResolver,
        IPluginOrderImportService orderImportService,
        IPluginLogger logger)
    {
        _configurationResolver = configurationResolver;
        _orderImportService = orderImportService;
        _logger = logger;
    }

    public Task<PluginWebhookRegistration> GenerateRegistrationAsync(
        PluginWebhookGenerateContext context,
        CancellationToken cancellationToken = default)
    {
        _ = context;

        return PluginWebhookRegistrationFactory.GenerateAsync(
            "orders",
            SignatureHeader,
            cancellationToken);
    }

    public async Task<PluginWebhookResult> HandleAsync(
        PluginWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = request.Customer;
        if (customer is null)
        {
            return PluginWebhookResult.NotFound("Webhook não encontrado ou desabilitado.");
        }

        if (string.IsNullOrWhiteSpace(request.Payload))
        {
            return PluginWebhookResult.BadRequest("Payload do webhook vazio.");
        }

        try
        {
            var options = await _configurationResolver.ResolveAsync(cancellationToken);
            if (!options.WebhookIsEnabled)
            {
                return PluginWebhookResult.BadRequest("Webhook Template está desabilitado.");
            }

            var order = JsonSerializer.Deserialize<TemplateOrder>(request.Payload, JsonOptions);
            if (order is null || string.IsNullOrWhiteSpace(order.Id))
            {
                return PluginWebhookResult.BadRequest("Pedido do webhook inválido.");
            }

            var (integration, status) = TemplateOrderMapper.Build(order, options, customer);
            var registration = await _orderImportService.RegisterAsync(
                new OrderImportRegistration
                {
                    ExternalOrderId = order.Id.Trim(),
                    StatusOrder = status,
                    Order = integration,
                    RawPayload = request.Payload
                },
                cancellationToken);

            if (!registration.Duplicate)
            {
                await _logger.WriteAsync(
                    PluginLogLevel.Information,
                    $"Template webhook: pedido {order.Id.Trim()} importado com sucesso",
                    $"Cliente: {customer.Company} (ID: {customer.Id})",
                    cancellationToken);
            }

            return PluginWebhookResult.Ok("Webhook processado com sucesso.");
        }
        catch (JsonException)
        {
            return PluginWebhookResult.BadRequest("Payload do webhook inválido.");
        }
        catch (Exception ex)
        {
            await _logger.WriteAsync(
                PluginLogLevel.Error,
                "Erro ao processar webhook Template",
                ex.ToString(),
                cancellationToken);

            return PluginWebhookResult.BadRequest("Erro ao processar webhook Template.");
        }
    }
}
