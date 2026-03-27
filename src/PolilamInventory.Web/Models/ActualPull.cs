namespace PolilamInventory.Web.Models;

public class ActualPull
{
    public int Id { get; set; }
    public int PatternId { get; set; }
    public Pattern Pattern { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTime PullDate { get; set; }
    public string SoNumber { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsDrop { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
