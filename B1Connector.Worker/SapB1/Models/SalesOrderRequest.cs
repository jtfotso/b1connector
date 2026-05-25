namespace B1Connector.Worker.SapB1.Models;
public class SalesOrderRequest
{
    public string CardCode { get; set; } = string.Empty;
    public string DocDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string DocDueDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string Comments { get; set; } = string.Empty;
    public List<SalesOrderLine> DocumentLines { get; set; } = new();
}