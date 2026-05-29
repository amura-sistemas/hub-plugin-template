using Amura.Hub.Plugin.Abstractions;
using Amura.Hub.Plugin.Configuration;

namespace Amura.Hub.Plugin.Ecommerce.Template.Configuration;

public sealed class TemplateConfigurationResolver
{
    private const string DefaultBaseUri = "https://api.example.com";

    private readonly PluginExecutionContext _executionContext;
    private readonly IPluginSettingsAccessor _settingsAccessor;

    public TemplateConfigurationResolver(
        PluginExecutionContext executionContext,
        IPluginSettingsAccessor settingsAccessor)
    {
        _executionContext = executionContext;
        _settingsAccessor = settingsAccessor;
    }

    public Task<TemplateIntegrationOptions> ResolveAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in _executionContext.Configuration)
        {
            var normalizedKey = NormalizeKey(key);
            if (!string.IsNullOrWhiteSpace(normalizedKey))
            {
                values[normalizedKey] = value;
            }
        }

        return Task.FromResult(BuildOptions(_executionContext.CustomerId, values, _settingsAccessor));
    }

    private static TemplateIntegrationOptions BuildOptions(
        string customerId,
        IReadOnlyDictionary<string, string?> values,
        IPluginSettingsAccessor settingsAccessor)
    {
        return new TemplateIntegrationOptions
        {
            CustomerId = customerId,
            BaseUri = NormalizeBaseUri(
                GetString(values, "baseUri")
                ?? GetGlobalValue(settingsAccessor, "BaseUri", "Variables:baseUri")
                ?? DefaultBaseUri),
            ApiToken = GetString(values, "apiToken")
                ?? GetGlobalValue(settingsAccessor, "Variables:apiToken", "ApiToken")
                ?? string.Empty,
            EndPointOrders = PluginGlobalSettings.GetEndpoint(settingsAccessor, "orders")
                ?? GetString(values, "endPointOrders")
                ?? "orders",
            EndPointProducts = PluginGlobalSettings.GetEndpoint(settingsAccessor, "products")
                ?? GetString(values, "endPointProducts")
                ?? "products",
            EndPointWebhooks = PluginGlobalSettings.GetEndpoint(settingsAccessor, "webhooks")
                ?? GetString(values, "endPointWebhooks")
                ?? "webhooks",
            DateFilter = Math.Max(1, GetInt(values, "dateFilter", 5)),
            IdEmpresa = Math.Max(1, GetInt(values, "idEmpresa", 1)),
            HasIntegrationOrdersEnabled = GetBool(values, "integrateOrders", true),
            HasIntegrationProductsEnabled = GetBool(values, "integrateProducts", true),
            PublishCategories = GetBool(values, "publishCategories"),
            SimpleProduct = GetBool(values, "simpleProduct"),
            HasCreateProductCodRefMaisCodCor = GetBool(values, "hasCreateProductCodRefMaisCodCor"),
            WebhookIsEnabled = GetBool(values, "webhookIsEnabled")
        };
    }

    public static string NormalizeBaseUri(string baseUri)
    {
        var value = string.IsNullOrWhiteSpace(baseUri)
            ? DefaultBaseUri
            : baseUri.Trim();

        return value.EndsWith('/') ? value : $"{value}/";
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var trimmed = key.Trim();
        var normalized = trimmed
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalized switch
        {
            "apiurl" => "baseUri",
            "baseurl" => "baseUri",
            "baseuri" => "baseUri",
            "token" => "apiToken",
            "apitoken" => "apiToken",
            "bearertoken" => "apiToken",
            "endpointorders" => "endPointOrders",
            "endpointproducts" => "endPointProducts",
            "endpointwebhooks" => "endPointWebhooks",
            "datefilter" => "dateFilter",
            "idempresa" => "idEmpresa",
            "integrateorders" => "integrateOrders",
            "hasintegrationordersenabled" => "integrateOrders",
            "integrateproducts" => "integrateProducts",
            "hasintegrationproductsenabled" => "integrateProducts",
            "publishcategories" => "publishCategories",
            "hascreatecategoriesenabled" => "publishCategories",
            "simpleproduct" => "simpleProduct",
            "hascreateproductcodrefmaiscodcor" => "hasCreateProductCodRefMaisCodCor",
            "webhookisenabled" => "webhookIsEnabled",
            _ => trimmed
        };
    }

    private static string? GetString(IReadOnlyDictionary<string, string?> values, string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static int GetInt(
        IReadOnlyDictionary<string, string?> values,
        string key,
        int defaultValue)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return int.TryParse(value.Trim(), out var parsed) ? parsed : defaultValue;
    }

    private static bool GetBool(
        IReadOnlyDictionary<string, string?> values,
        string key,
        bool defaultValue = false)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value.Trim(), out var parsed) ? parsed : defaultValue;
    }

    private static string? GetGlobalValue(
        IPluginSettingsAccessor settingsAccessor,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = settingsAccessor.GetValue(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
