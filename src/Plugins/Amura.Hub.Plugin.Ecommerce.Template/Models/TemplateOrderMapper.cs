using System.Globalization;
using Amura.Hub.Plugin.Abstractions;
using Amura.Hub.Plugin.Application.DTOs;
using Amura.Hub.Plugin.Domain.AggregatesModel.ImportAggregate;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;

namespace Amura.Hub.Plugin.Ecommerce.Template.Models;

internal static class TemplateOrderMapper
{
    public static (OrderIntegrationDto Order, StatusOrderEnum Status) Build(
        TemplateOrder source,
        TemplateIntegrationOptions options,
        PluginCustomerContext customer)
    {
        var orderId = source.Id.Trim();
        var customerName = ResolveCustomerName(customer);
        var status = MapStatus(source.Status);
        var updatedAt = source.UpdatedAt ?? DateTime.UtcNow;

        var integration = new OrderIntegrationDto
        {
            Order = new OrderDto
            {
                Pedido = orderId,
                Loja = options.IdEmpresa,
                Emissao = updatedAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                HoraEmissao = updatedAt.ToString("HHmmss", CultureInfo.InvariantCulture),
                TipoCliente = "J",
                DocCliente = string.Empty,
                NomeCliente = customerName,
                TipoEntrega = string.Empty,
                DataEntrega = string.Empty,
                Obs = string.IsNullOrWhiteSpace(source.Status) ? "Template order" : source.Status.Trim(),
                TotalPedido = FormatMoney(0m),
                TotalFrete = FormatMoney(0m),
                TotalDesconto = FormatMoney(0m),
                CodStatus = status.Code,
                Status = status.Description
            },
            Customer = new OrderCustomerDto
            {
                Email = customer.Email,
                Tipo = "J",
                Nome = customerName,
                CPF_CNPJ = string.Empty,
                DDD = string.Empty,
                Telefone = string.Empty,
                DtNasc = string.Empty,
                IE = string.Empty,
                Genero = string.Empty
            }
        };

        return (integration, status.EnumValue);
    }

    public static (StatusOrderEnum EnumValue, string Code, string Description) MapStatus(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "paid" or "payment_received" or "approved" or "aprovado" => (StatusOrderEnum.pagamento_recebido, "pagamento_recebido", "Pagamento Recebido"),
            "pending" or "awaiting_payment" or "aguardando_pagamento" => (StatusOrderEnum.aguardando_pagamento, "aguardando_pagamento", "Aguardando Pagamento"),
            "processing" or "faturado" or "pedido_faturado" => (StatusOrderEnum.pedido_faturado, "pedido_faturado", "Pedido Faturado"),
            "shipped" or "sent" or "enviado" or "pedido_enviado" => (StatusOrderEnum.pedido_enviado, "pedido_enviado", "Pedido Enviado"),
            "delivered" or "closed" or "pedido_entregue" => (StatusOrderEnum.pedido_entregue, "pedido_entregue", "Pedido Entregue"),
            "cancelled" or "canceled" or "pedido_cancelado" => (StatusOrderEnum.pedido_cancelado, "pedido_cancelado", "Pedido Cancelado"),
            "refunded" or "pedido_devolvido" => (StatusOrderEnum.pedido_devolvido, "pedido_devolvido", "Pedido Devolvido"),
            "analysis" or "analisys" or "pedido_analise" => (StatusOrderEnum.pedido_analise, "pedido_analise", "Pedido Em Análise"),
            _ => (StatusOrderEnum.pedido_recebido, "pedido_recebido", "Pedido Recebido")
        };
    }

    private static string ResolveCustomerName(PluginCustomerContext customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.ResponsibleName))
        {
            return customer.ResponsibleName.Trim();
        }

        return string.IsNullOrWhiteSpace(customer.Company)
            ? "Cliente Template"
            : customer.Company.Trim();
    }

    private static string FormatMoney(decimal value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
