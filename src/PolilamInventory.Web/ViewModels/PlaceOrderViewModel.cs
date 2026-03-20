using PolilamInventory.Web.Models;

namespace PolilamInventory.Web.ViewModels;

public class PlaceOrderViewModel
{
    public List<Pattern> Patterns { get; set; } = new();
    public List<decimal> Widths { get; set; } = new();
    public List<decimal> Lengths { get; set; } = new();
    public List<decimal> Thicknesses { get; set; } = new();

    // Form inputs
    public int PatternId { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public decimal Thickness { get; set; }
    public int QuantityOrdered { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime EtaDate { get; set; } = DateTime.Today.AddDays(30);
    public string PoNumber { get; set; } = string.Empty;
    public string? Note { get; set; }
}
