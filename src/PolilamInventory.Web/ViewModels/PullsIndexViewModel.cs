namespace PolilamInventory.Web.ViewModels;

public class PullsIndexViewModel
{
    public List<PullRow> Rows { get; set; } = new();
}

public class PullRow
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // "Pulled" or "Will Pull"
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Date { get; set; }
    public string SoNumber { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool CanEdit { get; set; } // true only for PlannedClaims
    public decimal? CostPerSheet { get; set; }
    public decimal? CostPerSqFt { get; set; }
}
