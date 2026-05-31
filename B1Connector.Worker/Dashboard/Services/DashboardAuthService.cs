using B1Connector.Worker.Data;
using B1Connector.Worker.Infrastructure;
using Microsoft.EntityFrameworkCore;


namespace B1Connector.Worker.Dashboard.Services;

public class DashboardAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly EncryptionService _encryption;

    public DashboardAuthService(AppDbContext dbContext, IConfiguration configuration, EncryptionService encryption)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _encryption = encryption;
    }

    public bool IsAdmin(string apiKey) => apiKey == (_configuration["Dashboard:AdminApiKey"] ?? string.Empty);

    public async Task<string?> GetTenantByApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        var tenants = await _dbContext.Tenants
            .Where(t => t.IsActive && t.DashboardApiKey != string.Empty)
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try
            {
                var decrypted = tenant.DashboardApiKey is "dev-client-key"
                or "dev-client-key-2"
                or ""
                ? tenant.DashboardApiKey
                : _encryption.Decrypt(tenant.DashboardApiKey);

                if (decrypted == apiKey)
                    return tenant.TenantId;
            }
            catch (System.Exception)
            {
                
                throw;
            }
        }
        return null;
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