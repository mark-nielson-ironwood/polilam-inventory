namespace PolilamInventory.Web.ViewModels;

public class SettingsViewModel
{
    public List<PatternRow> Patterns { get; set; } = new();
    public List<DimensionValueRow> Widths { get; set; } = new();
    public List<DimensionValueRow> Lengths { get; set; } = new();
    public List<ThicknessRow> Thicknesses { get; set; } = new();
    public string AppVersion { get; set; } = "1.0.0";
}

public class PatternRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ReorderTrigger { get; set; }
    public bool HasTransactions { get; set; }
}

public class DimensionValueRow
{
    public int Id { get; set; }
    public decimal Value { get; set; }
    public bool HasTransactions { get; set; }
}

public class ThicknessRow
{
    public int Id { get; set; }
    public decimal Value { get; set; }
    public string MaterialType { get; set; } = string.Empty;
    public bool HasTransactions { get; set; }
}
