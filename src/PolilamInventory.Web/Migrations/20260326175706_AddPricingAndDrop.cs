using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolilamInventory.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAndDrop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDrop",
                table: "PlannedClaims",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDrop",
                table: "InventoryAdjustments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerSqFt",
                table: "DimensionValues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDrop",
                table: "ActualPulls",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDrop",
                table: "PlannedClaims");

            migrationBuilder.DropColumn(
                name: "IsDrop",
                table: "InventoryAdjustments");

            migrationBuilder.DropColumn(
                name: "PricePerSqFt",
                table: "DimensionValues");

            migrationBuilder.DropColumn(
                name: "IsDrop",
                table: "ActualPulls");
        }
    }
}
