using System.Text.Json;
using B1Connector.Worker.Connectors.Shopify;
using B1Connector.Worker.Models;
using B1Connector.Worker.SapB1;
using B1Connector.Worker.Infrastructure;

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
        var tenantService = scope.ServiceProvider.GetRequiredService<TenantService>();
        var mapper = scope.ServiceProvider.GetRequiredService<ShopifyOrderMapper>();

        // Recover any jobs stuck in Processing from a previous crash
        await queue.RecoverStuckJobsAsync(ct);

        SyncJob? job;
        while ((job = await queue.DequeueAsync(ct)) is not null)
        {
            await ProcessJobAsync(job, queue, tenantService, mapper, ct);
        }
    }

    private async Task ProcessJobAsync(
    SyncJob job,
    SyncJobQueue queue,
    TenantService tenantService,
    ShopifyOrderMapper mapper,
    CancellationToken ct)
{
    _logger.LogInformation(
        "Processing Job {JobId} | Tenant={TenantId} | Type={ConnectorType}",
        job.Id, job.TenantId, job.ConnectorType);

    try
    {
        // Resolve tenant
        var tenant = await tenantService.GetByTenantIdAsync(job.TenantId, ct);
        if (tenant is null)
            throw new InvalidOperationException(
                $"Tenant {job.TenantId} not found or inactive");

        // Resolve SAP client — mock or real based on config
        var config = _scopeFactory.CreateScope()
            .ServiceProvider
            .GetRequiredService<IConfiguration>();
        var useMock = config.GetValue<bool>("UseMockServiceLayer");

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
                _logger as ILogger<ServiceLayerClient>
                    ?? new LoggerFactory().CreateLogger<ServiceLayerClient>());
        }

        await sapClient.LoginAsync();

        try
        {
            var result = job.ConnectorType switch
            {
                ConnectorType.Shopify => await HandleShopifyJobAsync(
                    job, sapClient, mapper),
                _ => throw new NotSupportedException(
                    $"Connector type {job.ConnectorType} is not supported yet")
            };

            await queue.MarkCompletedAsync(job, result, ct);
            _logger.LogInformation(
                "Job {JobId} completed — Result: {Result}", job.Id, result);
        }
        finally
        {
            await sapClient.LogoutAsync();
            httpClient?.Dispose();
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Job {JobId} failed", job.Id);
        await queue.MarkFailedAsync(job, ex, ct);
    }
}
    private async Task<string> HandleShopifyJobAsync(SyncJob job, ISapServiceLayerClient sapClient, ShopifyOrderMapper mapper)
    {
        var shopifyOrder = JsonSerializer.Deserialize<ShopifyOrder>(job.Payload)
        ?? throw new InvalidOperationException("Failed to deserialize Shopify order data.");

        var salesOrder = mapper.Map(shopifyOrder);
        var docEntry = await sapClient.CreateSalesOrderAsync(salesOrder);

        return $"SAP B1 DocEntry= {docEntry}";
    }
}
