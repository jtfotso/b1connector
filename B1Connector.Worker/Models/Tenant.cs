namespace B1Connector.Worker.Models;

public class Tenant
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;        // shopify domain
    public string ShopDomain { get; set; } = string.Empty;
    public string ShopifyApiKey { get; set; } = string.Empty;   // encrypted
    public string ShopifyWebhookSecret { get; set; } = string.Empty; // encrypted
    public string B1ServiceLayerUrl { get; set; } = string.Empty;
    public string B1CompanyDb { get; set; } = string.Empty;
    public string B1UserName { get; set; } = string.Empty;
    public string B1Password { get; set; } = string.Empty;      // encrypted
    public bool IsActive { get; set; } = true;
    public string DashboardApiKey { get; set; } = string.Empty; // encrypted
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}