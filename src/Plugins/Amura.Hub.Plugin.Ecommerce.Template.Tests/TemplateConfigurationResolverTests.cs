using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using FluentAssertions;
using Xunit;

namespace Amura.Hub.Plugin.Ecommerce.Template.Tests;

public sealed class TemplateConfigurationResolverTests
{
    private const string CustomerId = "customer-template";
    private const string SystemName = "Ecommerce.Template";

    [Fact]
    public async Task ResolveAsync_ShouldBuildOptionsFromCustomerConfiguration()
    {
        var configurationStore = new TestIntegrationConfigurationStore();
        configurationStore.Seed(CustomerId, SystemName, new Dictionary<string, string?>
        {
            ["baseUri"] = "https://tenant.example.com/api",
            ["apiToken"] = "customer-token",
            ["idEmpresa"] = "77",
            ["dateFilter"] = "9",
            ["webhookIsEnabled"] = "true",
            ["webhookId"] = "2345",
            ["webhookSecretKey"] = "webhook-secret"
        });

        var settingsAccessor = new TestPluginSettingsAccessor();
        settingsAccessor.Seed("Endpoints:orders", "custom-orders");
        settingsAccessor.Seed("Endpoints:products", "custom-products");
        settingsAccessor.Seed("Endpoints:webhooks", "custom-webhooks");

        var sut = new TemplateConfigurationResolver(configurationStore, settingsAccessor);

        var options = await sut.ResolveAsync(CustomerId, CancellationToken.None);

        options.CustomerId.Should().Be(CustomerId);
        options.BaseUri.Should().Be("https://tenant.example.com/api/");
        options.ApiToken.Should().Be("customer-token");
        options.IdEmpresa.Should().Be(77);
        options.DateFilter.Should().Be(9);
        options.EndPointOrders.Should().Be("custom-orders");
        options.EndPointProducts.Should().Be("custom-products");
        options.EndPointWebhooks.Should().Be("custom-webhooks");
        options.WebhookIsEnabled.Should().BeTrue();
        options.WebhookId.Should().Be("2345");
        options.WebhookSecretKey.Should().Be("webhook-secret");
    }

    [Fact]
    public async Task ResolveAsync_ShouldFallbackToGlobalSettings_WhenCustomerValuesAreMissing()
    {
        var configurationStore = new TestIntegrationConfigurationStore();
        configurationStore.Seed(CustomerId, SystemName, new Dictionary<string, string?>
        {
            ["idEmpresa"] = "3"
        });

        var settingsAccessor = new TestPluginSettingsAccessor();
        settingsAccessor.Seed("Variables:baseUri", "https://global.example.com");
        settingsAccessor.Seed("Variables:apiToken", "global-token");

        var sut = new TemplateConfigurationResolver(configurationStore, settingsAccessor);

        var options = await sut.ResolveAsync(CustomerId, CancellationToken.None);

        options.BaseUri.Should().Be("https://global.example.com/");
        options.ApiToken.Should().Be("global-token");
        options.IdEmpresa.Should().Be(3);
        options.DateFilter.Should().Be(5);
    }

    [Theory]
    [InlineData("https://api.example.com", "https://api.example.com/")]
    [InlineData("https://api.example.com/", "https://api.example.com/")]
    [InlineData("  https://api.example.com/base  ", "https://api.example.com/base/")]
    public void NormalizeBaseUri_ShouldTrimAndEnsureTrailingSlash(string input, string expected)
    {
        TemplateConfigurationResolver.NormalizeBaseUri(input).Should().Be(expected);
    }

    [Fact]
    public async Task ResolveAsync_ShouldUseSafeDefaults_WhenConfigurationDoesNotExist()
    {
        var sut = new TemplateConfigurationResolver(
            new TestIntegrationConfigurationStore(),
            new TestPluginSettingsAccessor());

        var options = await sut.ResolveAsync(CustomerId, CancellationToken.None);

        options.CustomerId.Should().Be(CustomerId);
        options.BaseUri.Should().Be("https://api.example.com/");
        options.EndPointOrders.Should().Be("orders");
        options.EndPointProducts.Should().Be("products");
        options.EndPointWebhooks.Should().Be("webhooks");
        options.DateFilter.Should().Be(5);
        options.IdEmpresa.Should().Be(1);
    }
}
