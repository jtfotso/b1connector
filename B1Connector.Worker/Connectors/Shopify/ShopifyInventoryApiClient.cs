namespace B1Connector.Worker.Connectors.Shopify;

public class ShopifyInventoryApiClient 
{
    private readonly ILogger<ShopifyInventoryApiClient> _logger;

    public ShopifyInventoryApiClient(ILogger<ShopifyInventoryApiClient> logger)
    {
        _logger = logger;
    }
    public async Task<bool> UpdateInventoryLevelAsync(
        string shopDomain, 
        string apiKey, 
        string locationId, 
        string inventoryItemId, 
        int quantity,
        CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();
            http.BaseAddress = new Uri($"https://{shopDomain}/");
            http.DefaultRequestHeaders.Add("X-Shopify-Access-Token", apiKey);

            var payload = new
            {
                location_id = locationId,
                inventory_item_id = inventoryItemId,
                available = quantity
            };
            var response = await http.PostAsJsonAsync("admin/api/2024-01/inventory_levels/set.json", payload, ct);
            
            if(response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Updated Shopify inventory for item {ItemId} to {Quantity}",
                    inventoryItemId, quantity);
                return true;
            }
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Failed to update Shopify inventory for item {ItemId}: {Error}",
                inventoryItemId, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception updating Shopify inventory for item {ItemId}", inventoryItemId);
            return false;
        }
    }

    public async Task<string?> GetInventoryItemIdBySkuAsync(
        string shopDomain,
        string apiKey,
        string sku,
        CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();
            http.BaseAddress = new Uri($"https://{shopDomain}/");
            http.DefaultRequestHeaders.Add("X-Shopify-Access-Token", apiKey);

            var response = await http.GetAsync(
                $"admin/api/2024-01/variants.json?sku={sku}", ct);

            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            var doc = System.Text.Json.JsonDocument.Parse(body);
            var variants = doc.RootElement.GetProperty("variants");

            if (variants.GetArrayLength() == 0) return null;

            return variants[0].GetProperty("inventory_item_id").GetInt64().ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inventory item ID for SKU={Sku}", sku);
            return null;
        }
    }
}