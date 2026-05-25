using B1Connector.Worker.Data;
using B1Connector.Worker.Models;
using Microsoft.EntityFrameworkCore;

namespace B1Connector.Worker.Jobs;

public class SyncJobQueue 
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SyncJobQueue> _logger;
    private const int MaxRetries = 3;
    public SyncJobQueue(AppDbContext dbContext, ILogger<SyncJobQueue> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EnqueueAsync(SyncJob job)
    {
        await _dbContext.SyncJobs.AddAsync(job);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<SyncJob?> DequeueAsync(CancellationToken ct)
    {
        var job = await _dbContext.SyncJobs
            .Where(j => j.Status == SyncJobStatus.Pending && j.RetryCount < MaxRetries)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if(job is null) return null;

        job.Status = SyncJobStatus.Processing;
        await _dbContext.SaveChangesAsync();

        return job;
    }

    public async Task MarkCompletedAsync(SyncJob job, string result, CancellationToken ct)
    {
        job.Status = SyncJobStatus.Completed;
        job.ProcessedAt = DateTime.UtcNow;

        _dbContext.SyncLogs.Add(new SyncLog
        {
            SyncJobId = job.Id,
            Level = SyncLogLevel.Info,
            Message = "Job completed successfully.",
            Detail = result
        });
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(SyncJob job, Exception ex, CancellationToken ct)
    {
        job.RetryCount++;
        job.LastError = ex.Message;

        if (job.RetryCount >= MaxRetries)
        {
            job.Status = SyncJobStatus.Failed;
            job.ProcessedAt = DateTime.UtcNow;
            _logger.LogError("Job {JobId} permanently failed after {RetryCount} attempts.", job.Id, job.RetryCount);
        }
        else
        {
            job.Status = SyncJobStatus.Pending; // Re-queue for retry
            _logger.LogWarning("Job {JobId} failed, retry {Retry}/{Max}", 
            job.Id, job.RetryCount, MaxRetries);
        }
        _dbContext.SyncLogs.Add(new SyncLog
        {
            SyncJobId = job.Id,
            Level = SyncLogLevel.Error,
            Message = $"Job failed: {ex.Message}",
            Detail = ex.ToString()
        });
        await _dbContext.SaveChangesAsync(ct);
    }
       
}