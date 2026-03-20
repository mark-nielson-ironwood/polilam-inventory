namespace PolilamInventory.Web.Models;

public class Size
{
    public int Id { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public decimal Thickness { get; set; }

    public string MaterialType => Thickness == 0.039m ? "Plastic Laminate" : "Compact Laminate";
    public string DisplayName => $"{Width}×{Length}×{Thickness}";
}
