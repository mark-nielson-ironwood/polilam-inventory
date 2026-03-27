using System.ComponentModel.DataAnnotations;

namespace PolilamInventory.Web.ViewModels;

public class EditOrderViewModel
{
    public int Id { get; set; }

    // Display only (not editable)
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }

    // Editable fields
    [Required]
    public string PoNumber { get; set; } = string.Empty;

    [Required]
    [Range(1, 9999)]
    public int QuantityOrdered { get; set; }

    [Required]
    public DateTime EtaDate { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public decimal? CostPerSheet { get; set; }
}
