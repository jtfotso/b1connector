namespace B1Connector.Worker.Models;

public enum SyncLogLevel
{
    Info,
    Warning,
    Error
}

public class SyncLog
{
    public int Id { get; set; }
    public int SyncJobId { get; set; }
    public SyncLogLevel Level { get; set; } = SyncLogLevel.Info;
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }

    public SyncJob Job { get; set; } = null!;

}