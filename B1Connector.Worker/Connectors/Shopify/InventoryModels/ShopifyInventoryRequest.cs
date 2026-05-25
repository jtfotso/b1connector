using System.Text.Json.Serialization;

namespace B1Connector.Worker.Connectors.Shopify.InventoryModels;

public class ShopifyInventoryRequest
{
    [JsonPropertyName("items")]
    public List<ShopifyInventoryItem> Items { get; set; } = new();
}