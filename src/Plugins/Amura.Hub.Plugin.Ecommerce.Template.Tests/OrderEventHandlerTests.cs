using System.Text.Json;
using Amura.Hub.Plugin.Abstractions;
using Amura.Hub.Plugin.Application.Events;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Ecommerce.Template.EventHandlers;
using Amura.Hub.Plugin.Ecommerce.Template.Models;
using Amura.Hub.Plugin.Ecommerce.Template.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Amura.Hub.Plugin.Ecommerce.Template.Tests;

public sealed class OrderEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRegisterOrdersFromExternalApi()
    {
        var outboundClient = new RecordingPluginOutboundClient();
        outboundClient.AddJsonResponse(
            request => request.Path.StartsWith("orders", StringComparison.OrdinalIgnoreCase),
            new TemplateEnvelope<TemplateOrder>
            {
                Items =
                [
                    new TemplateOrder
                    {
                        Id = "1001",
                        Status = "paid",
                        UpdatedAt = new DateTime(2026, 05, 29, 13, 22, 1, DateTimeKind.Utc)
                    }
                ]
            });

        var executionContext = new PluginExecutionContext
        {
            CustomerId = "customer-template",
            SystemName = "Ecommerce.Template",
            Customer = new PluginCustomerContext
            {
                Id = "customer-template",
                Company = "Amura",
                Email = "contato@amura.test",
                ResponsibleName = "Responsavel"
            },
            Configuration = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            OutboundClient = outboundClient
        };

        var settingsAccessor = new TestPluginSettingsAccessor();
        var resolver = new TemplateConfigurationResolver(executionContext, settingsAccessor);
        var templateService = new TemplateService(outboundClient, NullLogger<TemplateService>.Instance);
        var orderImportService = new RecordingOrderImportService();
        var logger = new RecordingPluginLogger();
        var sut = new OrderEventHandler(resolver, templateService, orderImportService, logger);

        await sut.HandleAsync(
            new OrderEvent
            {
                Customer = executionContext.Customer
            },
            CancellationToken.None);

        orderImportService.Requests.Should().HaveCount(1);
        orderImportService.Requests[0].ExternalOrderId.Should().Be("1001");
        orderImportService.Requests[0].StatusOrder.Should().Be(Amura.Hub.Plugin.Domain.AggregatesModel.ImportAggregate.StatusOrderEnum.pagamento_recebido);
        logger.Logs.Should().Contain(log => log.Level == Amura.Hub.Plugin.Application.Services.PluginLogLevel.Information);
    }
}
