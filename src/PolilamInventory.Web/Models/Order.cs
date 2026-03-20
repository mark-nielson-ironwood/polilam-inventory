namespace PolilamInventory.Web.Models;

public class Order
{
    public int Id { get; set; }
    public int PatternId { get; set; }
    public Pattern Pattern { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;
    public int QuantityOrdered { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime EtaDate { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public string? Note { get; set; }

    public List<Receipt> Receipts { get; set; } = new();

    public int QuantityReceived => Receipts.Sum(r => r.QuantityReceived);
    public int QuantityOutstanding => QuantityOrdered - QuantityReceived;
    public bool IsFilled => QuantityOutstanding <= 0;
}
