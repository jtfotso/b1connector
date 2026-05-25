using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using B1Connector.Worker.Data;
using B1Connector.Worker.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B1Connector.Worker.Connectors.Shopify;

public static class ShopifyWebhookHandler
{
    public static void MapShopifyEndpoints(this WebApplication app)
    {
        app.MapPost("/webhooks/shopify/orders", HandleOrderCreatedAsync)
        .WithName("ShopifyOrderCreated");
    }

    private static async Task<IResult> HandleOrderCreatedAsync(
        HttpRequest request,
        [FromServices] AppDbContext db,
        [FromServices] IConfiguration config,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("ShopifyWebhookHandler");
        var body = await ReadBodyAsync(request);

        var secret = config["Shopify:WebhookSecret"]?? string.Empty;
        if (!IsValidShopifyRequest(request, body, secret))
        {
            logger.LogWarning("Invalid Shopify webhook signature rejected.");

            return Results.Unauthorized();
        }

        var shopDomain = request.Headers["X-Shopify-Shop-Domain"].ToString();
        if (string.IsNullOrEmpty(shopDomain))
        {
            logger.LogWarning("Missing X-Shopify-Shop-Domain header.");

            return Results.BadRequest("Missing shop domain header.");
        }

        var job = new SyncJob
        {
            TenantId = shopDomain,
            ConnectorType = ConnectorType.Shopify,
            Payload = body,
            Status = SyncJobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        db.SyncJobs.Add(job);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Shopify order webhook enqueued - Job ID: {JobId}, Tenant= {TenantId}.", job.Id, job.TenantId);
        
        return Results.Ok(new {JobId = job.Id});
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static bool IsValidShopifyRequest(HttpRequest request, string body, string secret)
    {
        // TODO: remove this before going to production
        if (secret == "shopify-dev-secret") return true;

        var hmacHeader = request.Headers["X-Shopify-Hmac-Sha256"].ToString();
        if (string.IsNullOrEmpty(hmacHeader)) return false;

        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        var computedHmac = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHmac),
            Encoding.UTF8.GetBytes(hmacHeader));
    }
}
