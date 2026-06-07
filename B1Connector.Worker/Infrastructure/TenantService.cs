using B1Connector.Worker.Data;
using B1Connector.Worker.Models;
using B1Connector.Worker.SapB1;
using Microsoft.EntityFrameworkCore;

namespace B1Connector.Worker.Infrastructure;

public class TenantService
{
    private readonly AppDbContext _db;
    private readonly EncryptionService _encryption;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        AppDbContext db,
        EncryptionService encryption,
        ILogger<TenantService> logger)
    {
        _db = db;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<Tenant?> GetByShopDomainAsync(string shopDomain, CancellationToken ct = default)
    {
        return await _db.Tenants
            .FirstOrDefaultAsync(t => t.ShopDomain == shopDomain && t.IsActive, ct);
    }

    public async Task<Tenant?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default)
    {
        return await _db.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.IsActive, ct);
    }

    public ServiceLayerOptions GetServiceLayerOptions(Tenant tenant)
    {
        var password = tenant.B1Password == "encrypted-placeholder"
        ? tenant.B1Password
        : _encryption.Decrypt(tenant.B1Password);

        return new ServiceLayerOptions
        {
            BaseUrl = tenant.B1ServiceLayerUrl,
            CompanyDb = tenant.B1CompanyDb,
            UserName = tenant.B1UserName,
            Password = password
        };
    }

    public string GetShopifyWebhookSecret(Tenant tenant)
    {
        // Dev tenants inserted with plain text placeholder — skip decryption
        if (tenant.ShopifyWebhookSecret == "dev-secret-placeholder")
            return tenant.ShopifyWebhookSecret;

        return _encryption.Decrypt(tenant.ShopifyWebhookSecret);
    }

    public async Task<Tenant> CreateTenantAsync(
        string shopDomain,
        string shopifyApiKey,
        string shopifyWebhookSecret,
        string b1Url,
        string b1CompanyDb,
        string b1UserName,
        string b1Password,
        CancellationToken ct = default)
    {
        var tenant = new Tenant
        {
            TenantId = shopDomain,
            ShopDomain = shopDomain,
            ShopifyApiKey = _encryption.Encrypt(shopifyApiKey),
            ShopifyWebhookSecret = _encryption.Encrypt(shopifyWebhookSecret),
            B1ServiceLayerUrl = b1Url,
            B1CompanyDb = b1CompanyDb,
            B1UserName = b1UserName,
            B1Password = _encryption.Encrypt(b1Password),
            IsActive = true
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Tenant created: {ShopDomain}", shopDomain);

        //Auto-create default sync config for new tenant
        var syncConfig = new TenantSyncConfig
        {
            TenantId = tenant.TenantId,
            IsInventorySyncEnabled = false,
            SyncIntervalMinutes = 15,
            WarehouseCode = "01",
            ItemCodes = string.Empty,
            ShopifyLocationId = string.Empty,
            LastInventorySyncAt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _db.TenantSyncConfigs.Add(syncConfig);
        await _db.SaveChangesAsync(ct);

        return tenant;
    }

    public async Task UpdateDashboardApiKeyAsync(
    string tenantId,
    string plainApiKey,
    CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found");

        tenant.DashboardApiKey = _encryption.Encrypt(plainApiKey);
        tenant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public string GetShopifyApiKey(Tenant tenant)
    {
        if (tenant.ShopifyApiKey == "encrypted-placeholder")
            return tenant.ShopifyApiKey;

        return _encryption.Decrypt(tenant.ShopifyApiKey);
    }
}