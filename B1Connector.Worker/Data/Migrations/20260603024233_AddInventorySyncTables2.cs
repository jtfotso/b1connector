using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B1Connector.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventorySyncTables2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "InventoryStockLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<double>(
                name: "QuantityInB1",
                table: "InventoryStockLogs",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<double>(
                name: "QuantityInShopify",
                table: "InventoryStockLogs",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityInShopify",
                table: "InventoryStockLogs");

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "InventoryStockLogs",
                type: "int",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "QuantityInB1",
                table: "InventoryStockLogs",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
