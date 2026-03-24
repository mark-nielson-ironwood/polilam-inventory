namespace PolilamInventory.Web.ViewModels;

public class TransactionReportViewModel
{
    public string? PatternFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> PatternNames { get; set; } = new();
    public List<TransactionReportRow> Rows { get; set; } = new();
}

public class TransactionReportRow
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? PoSoNumber { get; set; }
    public DateTime? EtaDate { get; set; }
    public string? Note { get; set; }
}
