namespace B1Connector.Worker.Models;
public class InventoryStockLog
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public double QuantityInB1 { get; set; }
    public double? QuantityInShopify { get; set; }
    public bool ShopifyUpdated { get; set; } = false;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}