using System.ComponentModel.DataAnnotations;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Web.ViewModels;

public class PullSheetsViewModel
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

    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "SO Number is required.")]
    [MaxLength(100)]
    public string SoNumber { get; set; } = string.Empty;

    [Required]
    public DateTime PullDate { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string? Note { get; set; }

    // Mode: "PullNow" or "ScheduleFuture"
    public string Mode { get; set; } = "PullNow";
}
