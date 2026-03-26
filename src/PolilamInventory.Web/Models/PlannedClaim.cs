namespace PolilamInventory.Web.Models;

public class PlannedClaim
{
    public int Id { get; set; }
    public int PatternId { get; set; }
    public Pattern Pattern { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string SoNumber { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsDrop { get; set; }
}
