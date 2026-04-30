using System.Text.Json.Serialization;

namespace Amura.Hub.Plugin.Ecommerce.Template.Models;

public sealed class TemplateOrder
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}

public sealed class TemplateProduct
{
    [JsonPropertyName("sku")]
    public string Sku { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public sealed class TemplateWebhookRegistrationRequest
{
    [JsonPropertyName("resource")]
    public string Resource { get; init; } = string.Empty;

    [JsonPropertyName("targetUrl")]
    public string TargetUrl { get; init; } = string.Empty;
}

public sealed class TemplateEnvelope<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; init; } = [];
}
