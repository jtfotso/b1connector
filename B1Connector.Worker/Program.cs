using B1Connector.Worker;
using B1Connector.Worker.Connectors.Shopify;
using B1Connector.Worker.Data;
using B1Connector.Worker.Jobs;
using B1Connector.Worker.SapB1;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database context with retry policy for transient faults
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    )
);

// SAP B1 Service Layer client configuration
builder.Services.Configure<ServiceLayerOptions>(builder.Configuration.GetSection("ServiceLayer"));

var useMock = builder.Configuration.GetValue<bool>("UseMockServiceLayer");
if (useMock)
{
    builder.Services.AddScoped<ISapB1ServiceLayerClient, MockServiceLayerClient>();
}
else
{
    builder.Services.AddHttpClient<ServiceLayerClient>(client =>
    {
        var baseUrl = builder.Configuration["ServiceLayer:BaseUrl"];
        client.BaseAddress = new Uri(baseUrl!);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).AddStandardResilienceHandler();

    builder.Services.AddScoped<ISapB1ServiceLayerClient, ServiceLayerClient>();
}

// Jobs
builder.Services.AddScoped<SyncJobQueue>();
builder.Services.AddScoped<ShopifyOrderMapper>(); 
builder.Services.AddHostedService<SyncJobWorker>();

//Logging for webhook handler
builder.Services.AddLogging();


var app = builder.Build();

// Apply any pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
} 
    
// Map Shopify webhook endpoints
app.MapShopifyEndpoints();
app.MapShopifyInventoryEndpoints();
 
app.Run();
