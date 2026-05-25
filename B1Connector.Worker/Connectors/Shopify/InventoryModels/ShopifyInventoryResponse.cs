using System.Text.Json.Serialization;

namespace B1Connector.Worker.Connectors.Shopify.InventoryModels;
public class ShopifyInventoryResponse
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("warehouse_code")]
    public string WarehouseCode { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("available")]
    public bool Available { get; set; }
}