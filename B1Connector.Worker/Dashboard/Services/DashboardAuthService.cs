using B1Connector.Worker.Data;
using Microsoft.EntityFrameworkCore;

namespace B1Connector.Worker.Dashboard.Services;

public class DashboardAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public DashboardAuthService(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public bool IsAdmin(string apiKey) => apiKey == (_configuration["Dashboard:AdminApiKey"] ?? string.Empty);

    public async Task<string?> GetTenantByApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.DashboardApiKey == apiKey && t.IsActive, ct);
        return tenant?.TenantId;
    }

    public async Task<(bool isValid, bool isAdmin, string? tenantId)> ValidateAsync(string apiKey, CancellationToken ct = default)
    {
        if (IsAdmin(apiKey))
            return (true, true, null);

        var tenantId = await GetTenantByApiKeyAsync(apiKey, ct);
        if(tenantId is not null)
            return (true, false, tenantId);

        return (false, false, null);
    }

}