using Amura.Hub.Plugin.Abstractions;
using Amura.Hub.Plugin.Application.Events;
using Amura.Hub.Plugin.Application.DTOs;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Ecommerce.Template.EventHandlers;
using Amura.Hub.Plugin.Ecommerce.Template.Models;
using Amura.Hub.Plugin.Ecommerce.Template.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Amura.Hub.Plugin.Ecommerce.Template.Tests;

public sealed class ProductEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldMarkKnownProductsAsSuccessful()
    {
        var outboundClient = new RecordingPluginOutboundClient();
        outboundClient.AddJsonResponse(
            request => request.Path.StartsWith("products/sku-1", StringComparison.OrdinalIgnoreCase),
            new TemplateProduct
            {
                Sku = "sku-1",
                Name = "Produto 1"
            });

        var executionContext = new PluginExecutionContext
        {
            CustomerId = "customer-template",
            SystemName = "Ecommerce.Template",
            Customer = new PluginCustomerContext
            {
                Id = "customer-template",
                Company = "Amura",
                Email = "contato@amura.test"
            },
            Configuration = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["simpleProduct"] = "true",
                ["webhookIsEnabled"] = "true"
            },
            OutboundClient = outboundClient
        };

        var resolver = new TemplateConfigurationResolver(executionContext, new TestPluginSettingsAccessor());
        var templateService = new TemplateService(outboundClient, NullLogger<TemplateService>.Instance);
        var logger = new RecordingPluginLogger();
        var sut = new ProductEventHandler(resolver, templateService, logger);

        var product = new ProductIntegration
        {
            Sku = "sku-1",
            Referencia = "ref-1",
            Descricao = "Produto 1",
            NomeCategoria = "Categoria",
            PrecoCusto = "10",
            PrecoVenda = "20",
            Estoque = "1"
        };

        var notification = new ProductEvent
        {
            Customer = executionContext.Customer,
            Products = [product]
        };

        await sut.HandleAsync(notification, CancellationToken.None);

        notification.DispatchReport.TryGet("Ecommerce.Template", out var report).Should().BeTrue();
        report.Success.Should().BeTrue();
        report.TotalItems.Should().Be(1);
        report.Items.Should().ContainSingle(item => item.Success && item.ExternalId == "sku-1");
        logger.Logs.Should().Contain(log => log.Level == Amura.Hub.Plugin.Application.Services.PluginLogLevel.Information);
    }
}
