using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B1Connector.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventorySyncTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryStockLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", maxLength: 100, nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WarehouseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuantityInB1 = table.Column<int>(type: "int", nullable: false),
                    ShopifyUpdated = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryStockLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSyncConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsInventorySyncEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    WarehouseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemCodes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShopifyLocationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastInventorySyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSyncConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStockLogs_CreatedAt",
                table: "InventoryStockLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStockLogs_TenantId",
                table: "InventoryStockLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSyncConfigs_TenantId",
                table: "TenantSyncConfigs",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryStockLogs");

            migrationBuilder.DropTable(
                name: "TenantSyncConfigs");
        }
    }
}
