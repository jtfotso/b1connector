using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using B1Connector.Worker.Connectors.Shopify.InventoryModels;
using B1Connector.Worker.Data;
using B1Connector.Worker.Infrastructure;
using B1Connector.Worker.Models;
using B1Connector.Worker.SapB1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B1Connector.Worker.Connectors.Shopify;

public static class ShopifyInventoryHandler
{
    public static void MapShopifyInventoryEndpoints(this WebApplication app)
    {
        app.MapPost("/webhooks/shopify/inventory", HandleInventoryRequestAsync)
            .WithName("ShopifyInventoryRequest");
    }

    private static async Task<IResult> HandleInventoryRequestAsync(
        HttpRequest request,
        [FromServices] TenantService tenantService,
        [FromServices] IServiceScopeFactory scopeFactory,
        [FromServices] IConfiguration config,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ShopifyInventoryHandler");

        var body = await ReadBodyAsync(request);

        var shopDomain = request.Headers["X-Shopify-Shop-Domain"].ToString();
        if (string.IsNullOrEmpty(shopDomain))
            return Results.BadRequest("Missing shop domain header");

        // Look up tenant
        var tenant = await tenantService.GetByShopDomainAsync(shopDomain, ct);
        if (tenant is null)
        {
            logger.LogWarning("Unknown tenant: {ShopDomain}", shopDomain);
            return Results.Unauthorized();
        }

        // Validate HMAC using tenant-specific secret
        var secret = tenantService.GetShopifyWebhookSecret(tenant);
        if (!IsValidShopifyRequest(request, body, secret))
        {
            logger.LogWarning("Invalid inventory webhook signature for {ShopDomain}", shopDomain);
            return Results.Unauthorized();
        }

        // Parse payload
        ShopifyInventoryRequest? inventoryRequest;
        try
        {
            inventoryRequest = JsonSerializer.Deserialize<ShopifyInventoryRequest>(body);
            if (inventoryRequest is null)
                return Results.BadRequest("Invalid payload");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialise inventory request");
            return Results.BadRequest("Invalid JSON payload");
        }

        logger.LogInformation(
            "Inventory request from {ShopDomain} for {Count} items",
            shopDomain, inventoryRequest.Items.Count);

        // Build a per-tenant SAP B1 client using tenant credentials
         using var scope = scopeFactory.CreateScope();
       /* var sapOptions = tenantService.GetServiceLayerOptions(tenant);
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpFactory.CreateClient();
        httpClient.BaseAddress = new Uri(sapOptions.BaseUrl);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var sapLogger = loggerFactory.CreateLogger<ServiceLayerClient>();
        var sapClient = new ServiceLayerClient(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(sapOptions),
            sapLogger); */
        // Build a per-tenant SAP B1 client — mock or real based on config
        var useMock = config.GetValue<bool>("UseMockServiceLayer");

        ISapServiceLayerClient sapClient;
        HttpClient? httpClient = null;

        if (useMock)
        {
            sapClient = scope.ServiceProvider.GetRequiredService<ISapServiceLayerClient>();
        }
        else
        {
            var sapOptions = tenantService.GetServiceLayerOptions(tenant);
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(sapOptions.BaseUrl)
            };
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            var sapLogger = loggerFactory.CreateLogger<ServiceLayerClient>();
            sapClient = new ServiceLayerClient(
                httpClient,
                Microsoft.Extensions.Options.Options.Create(sapOptions),
                sapLogger);
        }

        await sapClient.LoginAsync();

        try
        {
            var results = new List<ShopifyInventoryResponse>();

            foreach (var item in inventoryRequest.Items)
            {
                try
                {
                    var stock = await sapClient.GetStockLevelAsync(
                        item.Sku,
                        item.WarehouseCode ?? "01");

                    results.Add(new ShopifyInventoryResponse
                    {
                        Sku = item.Sku,
                        WarehouseCode = stock.WarehouseCode,
                        Quantity = (int)stock.Quantity,
                        Available = stock.Quantity > 0
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to get stock for SKU={Sku}", item.Sku);
                    results.Add(new ShopifyInventoryResponse
                    {
                        Sku = item.Sku,
                        WarehouseCode = item.WarehouseCode ?? "01",
                        Quantity = 0,
                        Available = false
                    });
                }
            }

            return Results.Ok(results);
        }
        finally
        {
            await sapClient.LogoutAsync();
            httpClient?.Dispose();
        }
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static bool IsValidShopifyRequest(
        HttpRequest request,
        string body,
        string secret)
    {
        // Dev bypass
        if (secret == "dev-secret-placeholder") return true;

        // Admin test tool bypass — only allowed from localhost
        var testBypass = request.Headers["X-B1Connector-Test"].ToString();
        if (testBypass == "true" && request.Host.Host == "localhost") return true;

        var hmacHeader = request.Headers["X-Shopify-Hmac-Sha256"].ToString();
        if (string.IsNullOrEmpty(hmacHeader)) return false;

        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        var computedHmac = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHmac),
            Encoding.UTF8.GetBytes(hmacHeader));
    }
}