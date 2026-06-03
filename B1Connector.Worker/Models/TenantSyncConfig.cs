namespace B1Connector.Worker.Models;

public class TenantSyncConfig
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public bool IsInventorySyncEnabled { get; set; } = false;
    public int SyncIntervalMinutes { get; set; } = 30; // Default to 30 minutes
    public string WarehouseCode { get; set; } = "01"; // Default warehouse code
    public string ItemCodes { get; set; } = string.Empty; // Comma-separated list of item codes to sync, empty means all items
    public string ShopifyLocationId { get; set; } = string.Empty; // Shopify Location ID for inventory sync
    public DateTime? LastInventorySyncAt { get; set; } // Track last sync time
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<string> GetItemCodeList()
    {
        return string.IsNullOrWhiteSpace(ItemCodes)
            ? new List<string>()
            : ItemCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(code => code.Trim()).ToList();
    }
}