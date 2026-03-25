using System.ComponentModel.DataAnnotations;

namespace PolilamInventory.Web.ViewModels;

public class EditPlannedPullViewModel
{
    public int Id { get; set; }

    // Display only
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;

    // Editable
    [Required]
    [Range(1, 9999)]
    public int Quantity { get; set; }

    [Required]
    public DateTime ScheduledDate { get; set; }

    [Required]
    public string SoNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Note { get; set; }
}
