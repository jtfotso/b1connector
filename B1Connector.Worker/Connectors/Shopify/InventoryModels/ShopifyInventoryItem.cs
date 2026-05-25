using System.Text.Json.Serialization;

namespace B1Connector.Worker.Connectors.Shopify.InventoryModels;
public class ShopifyInventoryItem
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("warehouse_code")]
    public string? WarehouseCode { get; set; }
}