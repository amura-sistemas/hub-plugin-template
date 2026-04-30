using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Ecommerce.Template.Models;
using Amura.Hub.Plugin.Outbound;
using Microsoft.Extensions.Logging;

namespace Amura.Hub.Plugin.Ecommerce.Template.Services;

public sealed class TemplateService
{
    private readonly PluginHttpClient _httpClient;
    private readonly ILogger<TemplateService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TemplateService(IPluginOutboundClient outboundClient, ILogger<TemplateService> logger)
    {
        _httpClient = new PluginHttpClient(outboundClient, "main");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IReadOnlyCollection<TemplateOrder>> GetOrdersAsync(
        TemplateIntegrationOptions options,
        DateTime updatedAtMinUtc,
        CancellationToken cancellationToken = default)
    {
        ConfigureClient(options);
        var endpoint = $"{NormalizeEndpoint(options.EndPointOrders)}?updatedAtMin={Uri.EscapeDataString(updatedAtMinUtc.ToString("O"))}";
        var envelope = await GetAsync<TemplateEnvelope<TemplateOrder>>(endpoint, cancellationToken);
        return envelope?.Items ?? [];
    }

    public async Task<TemplateProduct?> GetProductBySkuAsync(
        TemplateIntegrationOptions options,
        string sku,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        ConfigureClient(options);
        var endpoint = $"{NormalizeEndpoint(options.EndPointProducts)}/{Uri.EscapeDataString(sku.Trim())}";
        return await GetAsync<TemplateProduct>(endpoint, cancellationToken, allowNotFound: true);
    }

    public async Task RegisterWebhookAsync(
        TemplateIntegrationOptions options,
        TemplateWebhookRegistrationRequest payload,
        CancellationToken cancellationToken = default)
    {
        ConfigureClient(options);
        await SendAsync(HttpMethod.Post, NormalizeEndpoint(options.EndPointWebhooks), payload, cancellationToken);
    }

    private void ConfigureClient(TemplateIntegrationOptions options)
    {
        _httpClient.BaseAddress = new Uri(TemplateConfigurationResolver.NormalizeBaseUri(options.BaseUri), UriKind.Absolute);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(options.ApiToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ApiToken.Trim());
        }
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        return string.IsNullOrWhiteSpace(endpoint)
            ? string.Empty
            : endpoint.Trim().Trim('/');
    }

    private async Task<T?> GetAsync<T>(
        string endpoint,
        CancellationToken cancellationToken,
        bool allowNotFound = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Template GET {Endpoint} failed with {StatusCode}. Response: {Response}",
                endpoint,
                (int)response.StatusCode,
                content);
            throw new HttpRequestException(
                $"Template request failed ({(int)response.StatusCode}) for endpoint '{endpoint}'.");
        }

        return string.IsNullOrWhiteSpace(content)
            ? default
            : JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    private async Task SendAsync(
        HttpMethod method,
        string endpoint,
        object payload,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var request = new HttpRequestMessage(method, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        _logger.LogWarning(
            "Template {Method} {Endpoint} failed with {StatusCode}. Payload: {Payload}. Response: {Response}",
            method.Method,
            endpoint,
            (int)response.StatusCode,
            json,
            content);
        throw new HttpRequestException(
            $"Template request failed ({(int)response.StatusCode}) for endpoint '{endpoint}'.");
    }
}
