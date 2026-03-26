namespace PolilamInventory.Web.Models;

public class InventoryAdjustment
{
    public int Id { get; set; }
    public int PatternId { get; set; }
    public Pattern Pattern { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTime DateAdded { get; set; }
    public string? Note { get; set; }
    public bool IsDrop { get; set; }
}
