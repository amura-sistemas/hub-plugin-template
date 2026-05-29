using System.Text;
using System.Text.Json;
using Amura.Hub.Plugin.Abstractions;
using Amura.Hub.Plugin.Application.DTOs;
using Amura.Hub.Plugin.Application.Services;
using Amura.Hub.Plugin.Domain.AggregatesModel.ImportAggregate;
using Amura.Hub.Plugin.Outbound;

namespace Amura.Hub.Plugin.Ecommerce.Template.Tests;

internal sealed class TestPluginSettingsAccessor : IPluginSettingsAccessor
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(string key, string? value)
    {
        _values[key] = value;
    }

    public string? GetValue(string key)
    {
        return _values.TryGetValue(key, out var value)
            ? value
            : null;
    }

    public string GetRequiredValue(string key)
    {
        return GetValue(key) ?? throw new InvalidOperationException($"Setting '{key}' is required.");
    }
}

internal sealed class RecordingOrderImportService : IPluginOrderImportService
{
    private readonly List<(string ExternalOrderId, StatusOrderEnum StatusOrder)> _registeredKeys = [];

    public List<OrderImportRegistration> Requests { get; } = [];

    public Task<PluginOrderImportRegistrationResult> RegisterAsync(
        OrderImportRegistration request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);

        var key = (request.ExternalOrderId, request.StatusOrder);
        if (_registeredKeys.Contains(key))
        {
            return Task.FromResult(PluginOrderImportRegistrationResult.AlreadyExists());
        }

        _registeredKeys.Add(key);

        return Task.FromResult(new PluginOrderImportRegistrationResult
        {
            Registered = true,
            Duplicate = false,
            ImportId = Guid.NewGuid().ToString("N"),
            FileName = $"{request.ExternalOrderId}_{request.StatusOrder}_PEDIDO.csv"
        });
    }

    public Task<bool> ExistsAsync(
        string externalOrderId,
        StatusOrderEnum statusOrder,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_registeredKeys.Contains((externalOrderId, statusOrder)));
    }
}

internal sealed class RecordingPluginLogger : IPluginLogger
{
    public List<RecordedLog> Logs { get; } = [];

    public Task WriteAsync(
        PluginLogLevel level,
        string message,
        string details = "",
        CancellationToken cancellationToken = default)
    {
        Logs.Add(new RecordedLog(level, message, details));
        return Task.CompletedTask;
    }
}

internal sealed record RecordedLog(PluginLogLevel Level, string Message, string Details);

internal sealed class RecordingPluginOutboundClient : IPluginOutboundClient
{
    private readonly List<Func<PluginOutboundRequest, bool>> _matchers = [];
    private readonly List<PluginOutboundResponse> _responses = [];

    public List<(string TargetName, PluginOutboundRequest Request)> Requests { get; } = [];

    public void AddJsonResponse(
        Func<PluginOutboundRequest, bool> matcher,
        object body,
        int statusCode = 200,
        string contentType = "application/json")
    {
        _matchers.Add(matcher);
        _responses.Add(new PluginOutboundResponse
        {
            StatusCode = statusCode,
            ContentType = contentType,
            Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body))
        });
    }

    public Task<PluginOutboundResponse> SendAsync(
        string targetName,
        PluginOutboundRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add((targetName, request));

        for (var index = 0; index < _matchers.Count; index++)
        {
            if (_matchers[index](request))
            {
                return Task.FromResult(_responses[index]);
            }
        }

        return Task.FromResult(new PluginOutboundResponse
        {
            StatusCode = 404,
            ContentType = "application/json",
            Body = Encoding.UTF8.GetBytes("{}")
        });
    }
}
