namespace PolilamInventory.Web.ViewModels;

public class InventoryReportViewModel
{
    public string? PatternFilter { get; set; }
    public List<string> PatternNames { get; set; } = new();
    public List<InventoryReportRow> Rows { get; set; } = new();
}

public class InventoryReportRow
{
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public decimal Thickness { get; set; }
    public int InStock { get; set; }
    public DateTime? LastAdjDate { get; set; }
    public int OnOrder { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? Eta { get; set; }
    public int CommittedBeforeArrival { get; set; }
    public int ProjectedAtArrival { get; set; }
    public int TotalCommitted { get; set; }
    public int ProjectedBalance { get; set; }
    public bool NeedsReorder { get; set; }
    public int ReorderTrigger { get; set; }
}
