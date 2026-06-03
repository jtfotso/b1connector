using B1Connector.Worker.Connectors.Shopify;
using B1Connector.Worker.Data;
using B1Connector.Worker.Infrastructure;
using B1Connector.Worker.Models;
using B1Connector.Worker.SapB1;
using Microsoft.EntityFrameworkCore;

namespace B1Connector.Worker.Jobs;

public class InventorySyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventorySyncWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private int _syncCount = 0;

    public InventorySyncWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<InventorySyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InventorySyncWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncDueTenantsAsync(stoppingToken);
                _syncCount++;

                // Clean up old logs once a day
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await CleanupOldLogsAsync(db, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error in InventorySyncWorker loop");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("InventorySyncWorker stopped");
    }

    private async Task SyncDueTenantsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantService = scope.ServiceProvider.GetRequiredService<TenantService>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var useMock = config.GetValue<bool>("UseMockServiceLayer");

        // Find tenants due for a sync
        var now = DateTime.UtcNow;
        var configs = await db.TenantSyncConfigs
            .Where(c => c.IsInventorySyncEnabled)
            .ToListAsync(ct);

        foreach (var syncConfig in configs)
        {
            var isDue = syncConfig.LastInventorySyncAt is null ||
                (now - syncConfig.LastInventorySyncAt.Value).TotalMinutes
                    >= syncConfig.SyncIntervalMinutes;

            if (!isDue) continue;

            await SyncTenantInventoryAsync(
                syncConfig, db, tenantService, useMock, ct);
        }
    }

    private async Task SyncTenantInventoryAsync(
        TenantSyncConfig syncConfig,
        AppDbContext db,
        TenantService tenantService,
        bool useMock,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting inventory sync for tenant {TenantId}", syncConfig.TenantId);

        var tenant = await tenantService.GetByTenantIdAsync(syncConfig.TenantId, ct);
        if (tenant is null)
        {
            _logger.LogWarning(
                "Tenant {TenantId} not found, skipping sync", syncConfig.TenantId);
            return;
        }

        var itemCodes = syncConfig.GetItemCodeList();
        if (itemCodes.Count == 0)
        {
            _logger.LogWarning(
                "No item codes configured for tenant {TenantId}", syncConfig.TenantId);
            return;
        }

        // Build SAP client
        ISapServiceLayerClient sapClient;
        HttpClient? httpClient = null;

        if (useMock)
        {
            sapClient = _scopeFactory.CreateScope()
                .ServiceProvider
                .GetRequiredService<ISapServiceLayerClient>();
        }
        else
        {
            var sapOptions = tenantService.GetServiceLayerOptions(tenant);
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(sapOptions.BaseUrl)
            };
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            sapClient = new ServiceLayerClient(
                httpClient,
                Microsoft.Extensions.Options.Options.Create(sapOptions),
                new LoggerFactory().CreateLogger<ServiceLayerClient>());
        }

        var shopifyClient = new ShopifyInventoryApiClient(
            new LoggerFactory().CreateLogger<ShopifyInventoryApiClient>());

        var shopifyApiKey = tenantService.GetShopifyApiKey(tenant);

        await sapClient.LoginAsync();

        try
        {
            foreach (var itemCode in itemCodes)
            {
                var stockLog = new InventoryStockLog
                {
                    TenantId = syncConfig.TenantId,
                    ItemCode = itemCode,
                    WarehouseCode = syncConfig.WarehouseCode,
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    // Get stock from B1
                    var stock = await sapClient.GetStockLevelAsync(
                        itemCode, syncConfig.WarehouseCode);
                    stockLog.QuantityInB1 = stock.Quantity;

                    // Get Shopify inventory item ID by SKU
                    var inventoryItemId = useMock ? "mock-item-id" :
                        await shopifyClient.GetInventoryItemIdBySkuAsync(
                            tenant.ShopDomain,
                            shopifyApiKey,
                            itemCode,
                            ct);

                    if (inventoryItemId is null)
                    {
                        stockLog.ErrorMessage =
                            $"SKU {itemCode} not found in Shopify";
                        _logger.LogWarning(stockLog.ErrorMessage);
                    }
                    else
                    {
                        // Update Shopify
                        var updated = useMock || await shopifyClient.UpdateInventoryLevelAsync(
                            tenant.ShopDomain,
                            shopifyApiKey,
                            syncConfig.ShopifyLocationId,
                            inventoryItemId,
                            (int)stock.Quantity,
                            ct);

                        stockLog.ShopifyUpdated = updated;
                        stockLog.QuantityInShopify = stock.Quantity;

                        _logger.LogInformation(
                            "Synced {ItemCode}: B1={Qty} → Shopify={Updated}",
                            itemCode, stock.Quantity, updated ? "✓" : "✗");
                    }
                }
                catch (Exception ex)
                {
                    stockLog.ErrorMessage = ex.Message;
                    _logger.LogError(ex,
                        "Failed to sync item {ItemCode} for tenant {TenantId}",
                        itemCode, syncConfig.TenantId);
                }

                db.InventoryStockLogs.Add(stockLog);
            }

            // Update last sync timestamp
            syncConfig.LastInventorySyncAt = DateTime.UtcNow;
            syncConfig.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Inventory sync completed for tenant {TenantId}", syncConfig.TenantId);
        }
        finally
        {
            await sapClient.LogoutAsync();
            httpClient?.Dispose();
        }
    }

    private async Task CleanupOldLogsAsync(AppDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var old = await db.InventoryStockLogs
            .Where(l => l.CreatedAt < cutoff)
            .ToListAsync(ct);

        if (old.Count > 0)
        {
            db.InventoryStockLogs.RemoveRange(old);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Cleaned up {Count} old inventory logs", old.Count);
        }
    }
}