using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Web.Controllers;

public class SettingsController : Controller
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var patterns = await _db.Patterns.OrderBy(p => p.Name).ToListAsync();
        var dimensionValues = await _db.DimensionValues.OrderBy(d => d.Value).ToListAsync();

        var patternIds = patterns.Select(p => p.Id).ToList();
        var patternsWithTransactions = await _db.Orders.Where(o => patternIds.Contains(o.PatternId)).Select(o => o.PatternId)
            .Union(_db.ActualPulls.Where(p => patternIds.Contains(p.PatternId)).Select(p => p.PatternId))
            .Union(_db.PlannedClaims.Where(c => patternIds.Contains(c.PatternId)).Select(c => c.PatternId))
            .Union(_db.InventoryAdjustments.Where(a => patternIds.Contains(a.PatternId)).Select(a => a.PatternId))
            .Distinct()
            .ToListAsync();

        var usedWidths = await _db.Sizes.Select(s => s.Width).Distinct().ToListAsync();
        var usedLengths = await _db.Sizes.Select(s => s.Length).Distinct().ToListAsync();
        var usedThicknesses = await _db.Sizes.Select(s => s.Thickness).Distinct().ToListAsync();

        var vm = new SettingsViewModel
        {
            Patterns = patterns.Select(p => new PatternRow
            {
                Id = p.Id,
                Name = p.Name,
                ReorderTrigger = p.ReorderTrigger,
                HasTransactions = patternsWithTransactions.Contains(p.Id)
            }).ToList(),

            Widths = dimensionValues.Where(d => d.Type == "Width").Select(d => new DimensionValueRow
            {
                Id = d.Id,
                Value = d.Value,
                HasTransactions = usedWidths.Contains(d.Value)
            }).ToList(),

            Lengths = dimensionValues.Where(d => d.Type == "Length").Select(d => new DimensionValueRow
            {
                Id = d.Id,
                Value = d.Value,
                HasTransactions = usedLengths.Contains(d.Value)
            }).ToList(),

            Thicknesses = dimensionValues.Where(d => d.Type == "Thickness").Select(d => new ThicknessRow
            {
                Id = d.Id,
                Value = d.Value,
                MaterialType = d.Value == Size.PlasticLaminateThickness ? "Plastic Laminate" : "Compact Laminate",
                HasTransactions = usedThicknesses.Contains(d.Value)
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> AddPattern(string name, int reorderTrigger)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Pattern name is required.");

        if (await _db.Patterns.AnyAsync(p => p.Name == name))
            return BadRequest($"Pattern '{name}' already exists.");

        _db.Patterns.Add(new Pattern { Name = name.Trim(), ReorderTrigger = reorderTrigger > 0 ? reorderTrigger : 5 });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> EditPattern(int id, string name, int reorderTrigger)
    {
        var pattern = await _db.Patterns.FindAsync(id);
        if (pattern == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Pattern name is required.");

        pattern.Name = name.Trim();
        pattern.ReorderTrigger = reorderTrigger > 0 ? reorderTrigger : 5;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeletePattern(int id)
    {
        var pattern = await _db.Patterns.FindAsync(id);
        if (pattern == null) return NotFound();

        bool hasTransactions =
            await _db.Orders.AnyAsync(o => o.PatternId == id) ||
            await _db.ActualPulls.AnyAsync(p => p.PatternId == id) ||
            await _db.PlannedClaims.AnyAsync(c => c.PatternId == id) ||
            await _db.InventoryAdjustments.AnyAsync(a => a.PatternId == id);

        if (hasTransactions)
            return BadRequest("Cannot delete a pattern that has transactions.");

        _db.Patterns.Remove(pattern);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddDimensionValue(string type, decimal value)
    {
        if (!new[] { "Width", "Length", "Thickness" }.Contains(type))
            return BadRequest("Invalid dimension type.");

        if (await _db.DimensionValues.AnyAsync(d => d.Type == type && d.Value == value))
            return BadRequest($"{type} {value} already exists.");

        _db.DimensionValues.Add(new DimensionValue { Type = type, Value = value });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDimensionValue(int id)
    {
        var dv = await _db.DimensionValues.FindAsync(id);
        if (dv == null) return NotFound();

        // Check if this value is used in any existing Size record
        bool isUsed = dv.Type switch
        {
            "Width" => await _db.Sizes.AnyAsync(s => s.Width == dv.Value),
            "Length" => await _db.Sizes.AnyAsync(s => s.Length == dv.Value),
            "Thickness" => await _db.Sizes.AnyAsync(s => s.Thickness == dv.Value),
            _ => false
        };

        if (isUsed)
            return BadRequest($"Cannot delete {dv.Type} {dv.Value} — it is used in existing inventory records.");

        _db.DimensionValues.Remove(dv);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
