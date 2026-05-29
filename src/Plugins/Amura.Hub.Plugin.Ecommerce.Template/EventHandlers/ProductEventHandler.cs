using Amura.Hub.Plugin.Application.Events;
using Amura.Hub.Plugin.Application.Services;
using Amura.Hub.Plugin.Ecommerce.Template.Configuration;
using Amura.Hub.Plugin.Ecommerce.Template.Services;
using Amura.Hub.Plugin.Products.Abstractions;

namespace Amura.Hub.Plugin.Ecommerce.Template.EventHandlers;

public sealed class ProductEventHandler : IProductPublishHandler
{
    private const string SystemName = "Ecommerce.Template";

    private readonly TemplateConfigurationResolver _configurationResolver;
    private readonly TemplateService _templateService;
    private readonly IPluginLogger _logger;

    public ProductEventHandler(
        TemplateConfigurationResolver configurationResolver,
        TemplateService templateService,
        IPluginLogger logger)
    {
        _configurationResolver = configurationResolver;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task HandleAsync(ProductEvent notification, CancellationToken cancellationToken = default)
    {
        var customer = notification.Customer;
        if (customer is null)
        {
            return;
        }

        try
        {
            var options = await _configurationResolver.ResolveAsync(cancellationToken);
            if (!options.HasIntegrationProductsEnabled)
            {
                notification.SetDispatchFailure(
                    SystemName,
                    "Integracao de produtos desabilitada para este plugin.",
                    "PLUGIN_PRODUCT_INTEGRATION_DISABLED");
                return;
            }

            var results = new List<ProductDispatchItemResult>();

            foreach (var product in notification.Products)
            {
                var reference = product.GetProductReference(options.SimpleProduct, options.HasCreateProductCodRefMaisCodCor);
                if (string.IsNullOrWhiteSpace(reference))
                {
                    results.Add(new ProductDispatchItemResult
                    {
                        ProductId = string.Empty,
                        ProductName = product.Descricao,
                        Success = false,
                        ErrorCode = "PRODUCT_REFERENCE_MISSING",
                        Error = "Referência do produto inválida."
                    });

                    continue;
                }

                var existing = await _templateService.GetProductBySkuAsync(options, reference, cancellationToken);
                if (existing is null)
                {
                    results.Add(new ProductDispatchItemResult
                    {
                        ProductId = reference,
                        ProductName = product.Descricao,
                        Success = false,
                        ErrorCode = "PRODUCT_NOT_FOUND",
                        Error = "Produto não localizado na origem."
                    });

                    continue;
                }

                results.Add(new ProductDispatchItemResult
                {
                    ProductId = reference,
                    ProductName = existing.Name,
                    Success = true,
                    ExternalId = existing.Sku,
                    Action = "Verificado"
                });
            }

            if (results.Count == 0)
            {
                notification.SetDispatchFailure(
                    SystemName,
                    "Nenhum produto válido foi encontrado no lote.",
                    "PLUGIN_NO_VALID_PRODUCTS");
                return;
            }

            var hasFailures = results.Any(result => !result.Success);
            notification.SetDispatchResult(
                SystemName,
                results,
                hasFailures
                    ? "Template: alguns produtos não foram encontrados"
                    : "Template: produtos verificados com sucesso",
                hasFailures ? "PRODUCTS_NOT_FOUND" : null);

            await _logger.WriteAsync(
                hasFailures ? PluginLogLevel.Warning : PluginLogLevel.Information,
                hasFailures
                    ? $"Template: {results.Count(result => result.Success)}/{results.Count} produtos verificados"
                    : $"Template: {results.Count} produtos verificados com sucesso",
                $"Cliente: {customer.Company} (ID: {customer.Id})",
                cancellationToken);
        }
        catch (Exception ex)
        {
            notification.SetDispatchFailure(SystemName, ex.Message);
            await _logger.WriteAsync(
                PluginLogLevel.Error,
                "Erro ao processar produtos Template",
                ex.ToString(),
                cancellationToken);
        }
    }
}
