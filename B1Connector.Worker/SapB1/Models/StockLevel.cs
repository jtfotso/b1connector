namespace B1Connector.Worker.SapB1.Models;

public class StockLevel
{
    public string ItemCode { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public double Quantity { get; set; }
    
}