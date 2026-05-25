using System.Text.Json.Serialization;

namespace B1Connector.Worker.Connectors.Shopify;

public class ShopifyLineItem
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0";
}