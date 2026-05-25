using B1Connector.Worker.Models;
using B1Connector.Worker.SapB1.Models;

namespace B1Connector.Worker.Connectors.Shopify;

public class ShopifyOrderMapper
{
    private readonly string DefaultCardCode = "C00001"; // fallback B1 customer
    private readonly string DefaultWarehouse = "WH01"; // fallback B1 warehouse
    public SalesOrderRequest Map(ShopifyOrder order)
    {
        return new SalesOrderRequest
        {
            CardCode = DefaultCardCode, // In a real implementation, you'd map this based on the Shopify order's customer info
            DocDate = order.CreatedAt.ToString("yyyy-MM-dd"),
            DocDueDate = order.CreatedAt.AddDays(7).ToString("yyyy-MM-dd"), // Example: due date
            Comments = $"Shopify Order #{order.Id} - {order.Note?.Trim()}",
            DocumentLines = order.LineItems.Select(li => new SalesOrderLine
            {
                ItemCode = li.Sku, // Assuming SKU in Shopify matches ItemCode in B1
                Quantity = li.Quantity,
                UnitPrice = double.TryParse(li.Price, out var price) ? price : 0,
                WarehouseCode = DefaultWarehouse // In a real implementation, you might determine this based on the item or other logic
            }).ToList()
        };
    }
}