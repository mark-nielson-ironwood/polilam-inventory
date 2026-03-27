using System.ComponentModel.DataAnnotations;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Web.ViewModels;

public class PlaceOrderViewModel
{
    public List<Pattern> Patterns { get; set; } = new();
    public List<decimal> Widths { get; set; } = new();
    public List<decimal> Lengths { get; set; } = new();
    public List<decimal> Thicknesses { get; set; } = new();

    [Required]
    public int PatternId { get; set; }

    [Required]
    public decimal Width { get; set; }

    [Required]
    public decimal Length { get; set; }

    [Required]
    public decimal Thickness { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int QuantityOrdered { get; set; }

    [Required]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime EtaDate { get; set; } = DateTime.Today.AddDays(30);

    [Required(ErrorMessage = "PO Number is required.")]
    [MaxLength(100)]
    public string PoNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Note { get; set; }

    public decimal? CostPerSqFt { get; set; }
}
