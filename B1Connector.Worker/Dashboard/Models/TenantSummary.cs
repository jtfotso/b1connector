
namespace B1Connector.Worker.Dashboard.Models;
public class TenantSummary
{
    public string TenantId { get; set; } = string.Empty;
    public string ShopDomain { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TotalJobs { get; set; }
    public int PendingJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public DateTime? LastJobAt { get; set; }
}