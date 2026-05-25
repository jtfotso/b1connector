namespace B1Connector.Worker.SapB1.Models;
public class SalesOrderLine
{
    public string ItemCode { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public double UnitPrice { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
}