namespace B1Connector.Worker.Models;
public enum SyncJobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum ConnectorType
{
    Shopify,
    Salesforce,
    WooCommerce,
    Magento,
    BigCommerce,
    SapAriba
}

public class SyncJob
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public ConnectorType ConnectorType { get; set; }
    public string Payload { get; set; } = string.Empty;
    public SyncJobStatus Status { get; set; } = SyncJobStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public ICollection<SyncLog> Logs { get; set; } = new List<SyncLog>();
}