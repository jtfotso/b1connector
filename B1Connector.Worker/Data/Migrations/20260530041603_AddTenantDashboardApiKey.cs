using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B1Connector.Worker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantDashboardApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DashboardApiKey",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DashboardApiKey",
                table: "Tenants");
        }
    }
}
