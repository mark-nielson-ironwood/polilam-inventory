namespace PolilamInventory.Web.Models;

public class SheetPricing
{
    public int Id { get; set; }
    public string Category { get; set; } = "Solid"; // "Solid" or "Woodgrain"
    public decimal Thickness { get; set; }
    public decimal Tier1Price { get; set; } // 1-49 sheets, $/sqft
    public decimal Tier2Price { get; set; } // 50-99 sheets, $/sqft
    public decimal Tier3Price { get; set; } // 100+ sheets, $/sqft
}
