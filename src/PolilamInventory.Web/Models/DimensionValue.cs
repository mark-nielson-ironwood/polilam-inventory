namespace PolilamInventory.Web.Models;

public class DimensionValue
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // "Width", "Length", "Thickness"
    public decimal Value { get; set; }
}
