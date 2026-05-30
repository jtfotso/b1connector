using B1Connector.Worker.Models;
using B1Connector.Worker.SapB1.Models;

namespace B1Connector.Worker.SapB1;

public class MockServiceLayerClient : ISapServiceLayerClient
{
    private readonly ILogger<MockServiceLayerClient> _logger;

    public MockServiceLayerClient(ILogger<MockServiceLayerClient> logger)
    {
        _logger = logger;
    }

    public Task LoginAsync()
    {
        _logger.LogInformation("Mock Login to SAP B1 Service Layer.");
        return Task.CompletedTask;
    }

    public Task LogoutAsync()
    {
        _logger.LogInformation("Mock Logout from SAP B1 Service Layer.");
        return Task.CompletedTask;
    }

    public Task<string> CreateSalesOrderAsync(SalesOrderRequest order)
    {
        var fakeDocEntry = new Random().Next(1000, 9999).ToString();
        _logger.LogInformation("Mock creating sales order for customer {CustomerCode} with {ItemCount} items. Assigned DocEntry: {DocEntry}", order.CardCode, order.DocumentLines.Count, fakeDocEntry);
        return Task.FromResult(fakeDocEntry); // Return a mock DocEntry
    }

    public Task<StockLevel> GetStockLevelAsync(string itemCode, string warehouseCode)
    {
        _logger.LogInformation("Mock getting stock level for item {ItemCode} in warehouse {WarehouseCode}.", itemCode, warehouseCode);
        var stockInfo = new StockLevel
        {
             ItemCode = itemCode,
             WarehouseCode = warehouseCode,
             Quantity = 100 // Return a mock stock level
         };
        return Task.FromResult(stockInfo);
    }
}