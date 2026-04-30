using Amura.Hub.Plugin.Abstractions;
using Amura.Hub.Plugin.Integrations.Abstractions;
using Amura.Hub.Plugin.Integrations.DTOs;

namespace Amura.Hub.Plugin.Ecommerce.Template.Tests;

internal sealed class TestIntegrationConfigurationStore : IIntegrationConfigurationStore
{
    private readonly Dictionary<(string CustomerId, string SystemName), IntegrationConfiguration> _values = new();

    public void Seed(string customerId, string systemName, IReadOnlyDictionary<string, string?> values)
    {
        _values[(customerId, systemName)] = new IntegrationConfiguration(customerId, systemName, values);
    }

    public Task<IntegrationConfiguration?> GetAsync(
        string customerId,
        string systemName,
        CancellationToken cancellationToken)
    {
        _values.TryGetValue((customerId, systemName), out var configuration);
        return Task.FromResult(configuration);
    }

    public Task SetAsync(IntegrationConfiguration configuration, CancellationToken cancellationToken)
    {
        _values[(configuration.CustomerId, configuration.SystemName)] = configuration;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string customerId, string systemName, CancellationToken cancellationToken)
    {
        return Task.FromResult(_values.Remove((customerId, systemName)));
    }
}

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
