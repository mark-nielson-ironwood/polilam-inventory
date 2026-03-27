using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolilamInventory.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddWACPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerSqFt",
                table: "DimensionValues");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Patterns",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CostPerSheet",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPerSheet",
                table: "InventoryAdjustments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SheetPricings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Thickness = table.Column<decimal>(type: "TEXT", nullable: false),
                    Tier1Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Tier2Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Tier3Price = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetPricings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SheetPricings");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Patterns");

            migrationBuilder.DropColumn(
                name: "CostPerSheet",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CostPerSheet",
                table: "InventoryAdjustments");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerSqFt",
                table: "DimensionValues",
                type: "TEXT",
                nullable: true);
        }
    }
}
