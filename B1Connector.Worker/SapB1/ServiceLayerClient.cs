using System.Net.Http.Json;
using B1Connector.Worker.SapB1.Models;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace B1Connector.Worker.SapB1;

public class ServiceLayerClient : ISapServiceLayerClient
{
    private readonly HttpClient _httpClient;
    private readonly ServiceLayerOptions _options;

    private readonly ILogger<ServiceLayerClient> _logger;

    public ServiceLayerClient(HttpClient httpClient, IOptions<ServiceLayerOptions> options, ILogger<ServiceLayerClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task LoginAsync()
    {
        var payload = new
        {
            CompanyDB = _options.CompanyDb,
            UserName = _options.UserName,
            Password = _options.Password
        };

        var response = await _httpClient.PostAsJsonAsync($"{_options.BaseUrl}/Login", payload);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Successfully logged into SAP B1 Service Layer.");
    }

    public async Task LogoutAsync()
    {
        var response = await _httpClient.PostAsync($"{_options.BaseUrl}/Logout", null);
        //response.EnsureSuccessStatusCode();
        _logger.LogInformation("Successfully logged out of SAP B1 Service Layer.");
    }

    public async Task<string> CreateSalesOrderAsync(SalesOrderRequest order)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_options.BaseUrl}/Orders", order);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var docEntry = doc.RootElement.GetProperty("DocEntry").GetInt32();
        _logger.LogInformation("Successfully created sales order with DocEntry: {DocEntry}", docEntry);

        return docEntry.ToString();
    }

    public async Task<StockLevel> GetStockLevelAsync(string itemCode, string warehouseCode)
    {
        // OData query to get stock level for specific item and warehouse
        // Exemple: $"ItemWarehouseInfoCollection?$filter=ItemCode eq '{itemCode}' and WarehouseCode eq '{warehouseCode}'"
        var url = $"{_options.BaseUrl}/Items('{itemCode}')/ItemWarehouseInfoCollection?$filter=WarehouseCode eq '{warehouseCode}'";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var stockInfo = doc.RootElement.GetProperty("value")[0];
        
        return new StockLevel
        {
            ItemCode = itemCode,
            WarehouseCode = warehouseCode,
            Quantity = stockInfo.GetProperty("OnHand").GetDouble()
            // Quantity = stockInfo.GetProperty("InStock").GetDouble() // Depending on the actual property name in the response
        };
    }
}