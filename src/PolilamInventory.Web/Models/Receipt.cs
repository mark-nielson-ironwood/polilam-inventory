namespace PolilamInventory.Web.Models;

public class Receipt
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int QuantityReceived { get; set; }
    public DateTime DateReceived { get; set; }
    public string? Note { get; set; }
}
