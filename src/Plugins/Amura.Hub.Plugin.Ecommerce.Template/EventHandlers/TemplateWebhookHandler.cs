using Amura.Hub.Plugin.Application.Events;
using Amura.Hub.Plugin.Application.Services;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Ecommerce.Template.Services;
using Amura.Hub.Plugin.Webhooks;
using Amura.Hub.Plugin.Webhooks.Abstractions;

namespace Amura.Hub.Plugin.Ecommerce.Template.EventHandlers;

public sealed class TemplateWebhookHandler : IPluginWebhookHandler
{
    private const string SystemName = "Ecommerce.Template";
    private const string SignatureHeader = "X-Template-Signature";

    private readonly TemplateWebhookCustomerResolver _webhookCustomerResolver;
    private readonly TemplateConfigurationResolver _configurationResolver;
    private readonly ICustomerService _customerService;
    private readonly OrderEventHandler _orderEventHandler;

    public TemplateWebhookHandler(
        TemplateWebhookCustomerResolver webhookCustomerResolver,
        TemplateConfigurationResolver configurationResolver,
        ICustomerService customerService,
        OrderEventHandler orderEventHandler)
    {
        _webhookCustomerResolver = webhookCustomerResolver;
        _configurationResolver = configurationResolver;
        _customerService = customerService;
        _orderEventHandler = orderEventHandler;
    }

    public Task<PluginWebhookRegistration> GenerateRegistrationAsync(
        PluginWebhookGenerateContext context,
        CancellationToken cancellationToken = default)
    {
        return PluginWebhookRegistrationFactory.GenerateAsync(
            _webhookCustomerResolver,
            "orders",
            SignatureHeader,
            cancellationToken);
    }

    public async Task<PluginWebhookResult> HandleAsync(
        PluginWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerId = await _webhookCustomerResolver.ResolveCustomerIdAsync(request.WebhookId, cancellationToken);
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return PluginWebhookResult.NotFound("Webhook Template nao encontrado ou desabilitado.");
        }

        if (string.IsNullOrWhiteSpace(request.Payload))
        {
            return PluginWebhookResult.BadRequest("Payload do webhook vazio.");
        }

        var options = await _configurationResolver.ResolveAsync(customerId, cancellationToken);
        if (!options.WebhookIsEnabled)
        {
            return PluginWebhookResult.BadRequest("Webhook Template esta desabilitado.");
        }

        var signature = PluginWebhookSecurity.GetFirstNotEmptyHeader(request.Headers, SignatureHeader);
        if (!PluginWebhookSecurity.ValidateHmacSha256(signature, request.Payload, options.WebhookSecretKey))
        {
            return PluginWebhookResult.Unauthorized();
        }

        var customer = await _customerService.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return PluginWebhookResult.NotFound("Cliente nao encontrado para o webhook informado.");
        }

        var plugin = customer.Plugins.FirstOrDefault(p =>
            p.IsEnabled &&
            p.SystemName.Equals(SystemName, StringComparison.OrdinalIgnoreCase));

        if (plugin is null)
        {
            return PluginWebhookResult.NotFound("Plugin Template nao esta habilitado para o cliente informado.");
        }

        await _orderEventHandler.HandleAsync(new OrderEvent
        {
            Customer = customer,
            IsEventManual = true
        }, cancellationToken);

        return PluginWebhookResult.Ok("Webhook processado com sucesso.");
    }
}
