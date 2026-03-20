namespace PolilamInventory.Web.Models;

public class Pattern
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ReorderTrigger { get; set; } = 5;
}
