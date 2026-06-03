using B1Connector.Worker.Models;
using Microsoft.EntityFrameworkCore;

namespace B1Connector.Worker.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<SyncJob> SyncJobs => Set<SyncJob>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSyncConfig> TenantSyncConfigs => Set<TenantSyncConfig>();
    public DbSet<InventoryStockLog> InventoryStockLogs => Set<InventoryStockLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Payload).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ConnectorType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.LastError).HasMaxLength(2000);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasMany(e => e.Logs)
                .WithOne(l => l.Job)
                .HasForeignKey(l => l.SyncJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Level).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Detail).HasColumnType("nvarchar(max)");

            entity.HasIndex(e => e.SyncJobId);
            entity.HasIndex(e => e.CreatedAt);
        });
    
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ShopDomain)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ShopifyApiKey)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ShopifyWebhookSecret)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.B1ServiceLayerUrl)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.B1CompanyDb)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.B1UserName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.B1Password)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(e => e.TenantId)
                .IsUnique();

            entity.HasIndex(e => e.ShopDomain)
                .IsUnique();
        });

        modelBuilder.Entity<TenantSyncConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WarehouseCode).HasMaxLength(50);
            entity.Property(e => e.ItemCodes).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ShopifyLocationId).HasMaxLength(100);

            entity.HasIndex(e => e.TenantId).IsUnique();
        });

        modelBuilder.Entity<InventoryStockLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ItemCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WarehouseCode).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}