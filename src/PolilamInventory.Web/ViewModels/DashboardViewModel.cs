namespace PolilamInventory.Web.ViewModels;

public class DashboardViewModel
{
    public List<AlertItem> Alerts { get; set; } = new();
    public List<PatternCard> PatternCards { get; set; } = new();
    public List<UpcomingClaim> UpcomingClaims { get; set; } = new();
}

public class AlertItem
{
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // "danger" or "warning"
}

public class PatternCard
{
    public string PatternName { get; set; } = string.Empty;
    public int TotalSheets { get; set; }
    public string Status { get; set; } = string.Empty; // "healthy", "low-stock", "deficit"
    public List<SizeBreakdown> Sizes { get; set; } = new();
}

public class SizeBreakdown
{
    public string SizeDisplay { get; set; } = string.Empty;
    public int InStock { get; set; }
    public int OnOrder { get; set; }
    public string StockStatus { get; set; } = string.Empty; // "healthy", "low-stock", "deficit"
}

public class UpcomingClaim
{
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string SoNumber { get; set; } = string.Empty;
    public bool IsDeficit { get; set; }
}
