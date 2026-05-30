using B1Connector.Worker.SapB1.Models;

namespace B1Connector.Worker.SapB1;

public interface ISapServiceLayerClient
{
    Task LoginAsync();
    Task LogoutAsync();
    Task<string> CreateSalesOrderAsync(SalesOrderRequest order);
    Task<StockLevel> GetStockLevelAsync(string itemCode, string warehouseCode);
}