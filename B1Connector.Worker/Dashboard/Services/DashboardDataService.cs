using B1Connector.Worker.Dashboard.Models;
using B1Connector.Worker.Data;
using B1Connector.Worker.Models;
using Microsoft.EntityFrameworkCore; 

namespace B1Connector.Worker.Dashboard.Services;

public class DashboardDataService
{
    private readonly AppDbContext _dbContext;

    public DashboardDataService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Admin - all tenants summary
    public async Task<List<TenantSummary>> GetAllTenantsSummariesAsync(CancellationToken ct = default)
    {
        var tenants = await _dbContext.Tenants.ToListAsync(ct);
        var result = new List<TenantSummary>();

        foreach(var tenant in tenants)
        {
            var jobs = await _dbContext.SyncJobs.Where(j => j.TenantId == tenant.TenantId).ToListAsync(ct);
            result.Add(new TenantSummary
            {
                TenantId = tenant.TenantId,
                ShopDomain = tenant.ShopDomain,
                IsActive = tenant.IsActive,
                TotalJobs = jobs.Count,
                PendingJobs = jobs.Count(j => j.Status == SyncJobStatus.Pending),
                CompletedJobs = jobs.Count(j => j.Status == SyncJobStatus.Completed),
                FailedJobs = jobs.Count(j => j.Status == SyncJobStatus.Failed),
                LastJobAt = jobs.OrderByDescending(j => j.CreatedAt).FirstOrDefault()?.CreatedAt
            });
        }
        return result;
    }

    // Admin - all jobs, client - filtered by tenantId
    public async Task<List<SyncJob>> GetJobsAsync(string? tenantId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = _dbContext.SyncJobs.AsQueryable();
        if(tenantId is not null)
            query = query.Where(j => j.TenantId == tenantId);
    
            return await query
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
    }

    public async Task<int> GetJobCountAsync(string? tenantId = null, CancellationToken ct = default)
    {
        var query = _dbContext.SyncJobs.AsQueryable();
        if(tenantId is not null)
            query = query.Where(j => j.TenantId == tenantId);
    
        return await query.CountAsync(ct);
    }

    // Logs for a specific job
    public async Task<List<SyncLog>> GetLogsForJobAsync(
        int jobId,
        CancellationToken ct = default)
    {
        return await _dbContext.SyncLogs
            .Where(l => l.SyncJobId == jobId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    // Admin stats for overview cards
    public async Task<DashboardStats> GetStatsAsync(
        string? tenantId = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.SyncJobs.AsQueryable();
        if (tenantId is not null)
            query = query.Where(j => j.TenantId == tenantId);

        var jobs = await query.ToListAsync(ct);

        return new DashboardStats
        {
            Total = jobs.Count,
            Pending = jobs.Count(j => j.Status == SyncJobStatus.Pending),
            Processing = jobs.Count(j => j.Status == SyncJobStatus.Processing),
            Completed = jobs.Count(j => j.Status == SyncJobStatus.Completed),
            Failed = jobs.Count(j => j.Status == SyncJobStatus.Failed)
        };
    }
}