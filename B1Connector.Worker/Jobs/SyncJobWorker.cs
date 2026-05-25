using System.Text.Json;
using B1Connector.Worker.Connectors.Shopify;
using B1Connector.Worker.Models;
using B1Connector.Worker.SapB1;

namespace B1Connector.Worker.Jobs;

public class SyncJobWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncJobWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

    public SyncJobWorker(IServiceScopeFactory scopeFactory, ILogger<SyncJobWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SyncJobWorker started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing orders: {Message}", ex.Message);
            }
            await Task.Delay(_pollInterval, stoppingToken);
        }
        _logger.LogInformation("SyncJobWorker stopping at: {time}", DateTimeOffset.Now);
    } 

    private async Task ProcessPendingJobsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var queue = scope.ServiceProvider.GetRequiredService<SyncJobQueue>();
        var sapClient = scope.ServiceProvider.GetRequiredService<ISapB1ServiceLayerClient>();
        var mapper = scope.ServiceProvider.GetRequiredService<ShopifyOrderMapper>();

        await sapClient.LoginAsync();

        try
        {
            SyncJob? job;
            while((job = await queue.DequeueAsync(ct)) is not null)
            {
                await ProcessJobAsync(job, queue, sapClient, mapper, ct);
            }
        }
        finally
        {
            await sapClient.LogoutAsync();
        }
    }

    private async Task ProcessJobAsync(
        SyncJob job, 
        SyncJobQueue queue, 
        ISapB1ServiceLayerClient sapClient, 
        ShopifyOrderMapper mapper,
        CancellationToken ct)
    {
        _logger.LogInformation("Processing job {JobId} | Tenant={TenantId} | Type={ConnectorType}", 
        job.Id, job.TenantId, job.ConnectorType);
        try
        {
            var result = job.ConnectorType switch
            {
                ConnectorType.Shopify => await HandleShopifyJobAsync(job, sapClient, mapper),
                _ => throw new NotSupportedException($"Connector type {job.ConnectorType} is not supported.")
            };

            await queue.MarkCompletedAsync(job, result, ct);

            _logger.LogInformation("Job {JobId} completed successfully - Result: {Result}", job.Id, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", job.Id);
            await queue.MarkFailedAsync(job, ex, ct);
        }
    }

    private async Task<string> HandleShopifyJobAsync(SyncJob job, ISapB1ServiceLayerClient sapClient, ShopifyOrderMapper mapper)
    {
        var shopifyOrder = JsonSerializer.Deserialize<ShopifyOrder>(job.Payload)
        ?? throw new InvalidOperationException("Failed to deserialize Shopify order data.");

        var salesOrder = mapper.Map(shopifyOrder);
        var docEntry = await sapClient.CreateSalesOrderAsync(salesOrder);

        return $"SAP B1 DocEntry= {docEntry}";
    }
}
