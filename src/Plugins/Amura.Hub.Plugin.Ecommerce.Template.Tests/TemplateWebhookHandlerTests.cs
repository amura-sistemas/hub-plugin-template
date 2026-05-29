using System.Text.Json;
using Amura.Hub.Plugin.Abstractions;
using Amura.Hub.Plugin.Application.Events;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Ecommerce.Template.EventHandlers;
using Amura.Hub.Plugin.Ecommerce.Template.Models;
using Amura.Hub.Plugin.Webhooks.Abstractions;
using FluentAssertions;
using Xunit;

namespace Amura.Hub.Plugin.Ecommerce.Template.Tests;

public sealed class TemplateWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRegisterOrderFromWebhookPayload()
    {
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
            Configuration = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["webhookIsEnabled"] = "true"
            }
        };

        var resolver = new TemplateConfigurationResolver(executionContext, new TestPluginSettingsAccessor());
        var orderImportService = new RecordingOrderImportService();
        var logger = new RecordingPluginLogger();
        var sut = new TemplateWebhookHandler(resolver, orderImportService, logger);

        var payload = JsonSerializer.Serialize(new TemplateOrder
        {
            Id = "2001",
            Status = "pending",
            UpdatedAt = new DateTime(2026, 05, 29, 13, 22, 1, DateTimeKind.Utc)
        });

        var result = await sut.HandleAsync(
            new PluginWebhookRequest
            {
                WebhookId = 321,
                Customer = executionContext.Customer,
                Payload = payload
            },
            CancellationToken.None);

        result.StatusCode.Should().Be(200);
        orderImportService.Requests.Should().HaveCount(1);
        orderImportService.Requests[0].ExternalOrderId.Should().Be("2001");
        orderImportService.Requests[0].RawPayload.Should().Be(payload);
        orderImportService.Requests[0].StatusOrder.Should().Be(Amura.Hub.Plugin.Domain.AggregatesModel.ImportAggregate.StatusOrderEnum.aguardando_pagamento);
        logger.Logs.Should().Contain(log => log.Message.Contains("webhook", StringComparison.OrdinalIgnoreCase));
    }
}
