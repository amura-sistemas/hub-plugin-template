namespace Amura.Hub.Plugin.Ecommerce.Template.Configuration;

public sealed class TemplateIntegrationOptions
{
    public string CustomerId { get; init; } = string.Empty;
    public string BaseUri { get; init; } = "https://api.example.com/";
    public string ApiToken { get; init; } = string.Empty;
    public string EndPointOrders { get; init; } = "orders";
    public string EndPointProducts { get; init; } = "products";
    public string EndPointWebhooks { get; init; } = "webhooks";
    public int DateFilter { get; init; } = 5;
    public int IdEmpresa { get; init; } = 1;
    public bool HasIntegrationOrdersEnabled { get; init; } = true;
    public bool HasIntegrationProductsEnabled { get; init; } = true;
    public bool PublishCategories { get; init; }
    public bool SimpleProduct { get; init; }
    public bool WebhookIsEnabled { get; init; }
    public string? WebhookId { get; init; }
    public string? WebhookSecretKey { get; init; }
}
