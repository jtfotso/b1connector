using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using B1Connector.Worker.Connectors.Shopify.InventoryModels;
using B1Connector.Worker.Data;
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
        // Shopify calls this when it needs to know stock levels
        app.MapPost("/webhooks/shopify/inventory", HandleInventoryRequestAsync)
            .WithName("ShopifyInventoryRequest");
    }

    private static async Task<IResult> HandleInventoryRequestAsync(
        HttpRequest request,
        [FromServices] ISapB1ServiceLayerClient sapClient,
        [FromServices] IConfiguration config,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ShopifyInventoryHandler");

        // Read and validate body
        var body = await ReadBodyAsync(request);
        var secret = config["Shopify:WebhookSecret"] ?? string.Empty;

        if (!IsValidShopifyRequest(request, body, secret))
        {
            logger.LogWarning("Invalid Shopify inventory webhook signature rejected");
            return Results.Unauthorized();
        }

        var shopDomain = request.Headers["X-Shopify-Shop-Domain"].ToString();
        if (string.IsNullOrEmpty(shopDomain))
            return Results.BadRequest("Missing shop domain header");

        // Parse the inventory request payload
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

        // Query B1 for each item
        logger.LogInformation(
            "Inventory request from {ShopDomain} for {Count} items",
            shopDomain, inventoryRequest.Items.Count);

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

                    logger.LogInformation(
                        "Stock for SKU={Sku} Warehouse={Warehouse}: {Quantity}",
                        item.Sku, stock.WarehouseCode, stock.Quantity);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to get stock for SKU={Sku}", item.Sku);

                    // Return 0 for failed items rather than failing the whole request
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
        if (secret == "shopify-dev-secret") return true;

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