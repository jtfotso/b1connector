using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B1Connector.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShopDomain = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShopifyApiKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ShopifyWebhookSecret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    B1ServiceLayerUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    B1CompanyDb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    B1UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    B1Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ShopDomain",
                table: "Tenants",
                column: "ShopDomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantId",
                table: "Tenants",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
