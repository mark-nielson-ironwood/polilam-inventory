using System.ComponentModel.DataAnnotations;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Web.ViewModels;

public class ReceiveShipmentViewModel
{
    // Dropdown data
    public List<Order> OpenOrders { get; set; } = new();

    // Selected order details (populated via AJAX)
    public int? SelectedPatternId { get; set; }
    public string? SelectedPatternName { get; set; }
    public string? SelectedSizeDisplay { get; set; }
    public int? SelectedQuantityOrdered { get; set; }
    public int? SelectedQuantityReceived { get; set; }
    public int? SelectedQuantityOutstanding { get; set; }
    public DateTime? SelectedEtaDate { get; set; }

    // Form inputs
    [Required]
    public int OrderId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int QuantityReceived { get; set; }

    [Required]
    public DateTime DateReceived { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string? Note { get; set; }

    // Receipt history for the selected order
    public List<Receipt> ReceiptHistory { get; set; } = new();
}
