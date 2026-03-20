namespace PolilamInventory.Web.ViewModels;

public class RemovalReportViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IncludeInactive { get; set; }
    public List<string> PatternNames { get; set; } = new();
    public List<RemovalReportRow> Rows { get; set; } = new();
}

public class RemovalReportRow
{
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public decimal Thickness { get; set; }
    public int SheetsRemoved { get; set; }
    public DateTime? LastRemovalDate { get; set; }
}
