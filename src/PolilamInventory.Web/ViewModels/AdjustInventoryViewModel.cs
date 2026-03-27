using System.ComponentModel.DataAnnotations;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Web.ViewModels;

public class AdjustInventoryViewModel
{
    // Dropdown data
    public List<Pattern> Patterns { get; set; } = new();
    public List<decimal> Widths { get; set; } = new();
    public List<decimal> Lengths { get; set; } = new();
    public List<decimal> Thicknesses { get; set; } = new();

    // Form inputs
    [Required]
    public int PatternId { get; set; }

    [Required]
    public decimal Width { get; set; }

    [Required]
    public decimal Length { get; set; }

    [Required]
    public decimal Thickness { get; set; }

    [Required]
    [Range(-9999, 9999, ErrorMessage = "Quantity must be between -9999 and 9999.")]
    public int Quantity { get; set; }

    [Required]
    public DateTime DateAdded { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string? Note { get; set; }

    public bool IsDrop { get; set; }

    public decimal? CostPerSheet { get; set; }
}
