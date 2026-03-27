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
        var dimensionValues = (await _db.DimensionValues.ToListAsync()).OrderBy(d => d.Value).ToList();

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
                Category = p.Category,
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
            }).ToList(),

            SheetPricings = (await _db.SheetPricings.ToListAsync())
                .OrderBy(sp => sp.Category == "Solid" ? 0 : 1)
                .ThenBy(sp => sp.Thickness)
                .ToList(),

            AppVersion = FormatVersion()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> AddPattern(string name, int reorderTrigger, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Pattern name is required.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.Patterns.AnyAsync(p => p.Name == name.Trim()))
        {
            TempData["Error"] = $"Pattern '{name}' already exists.";
            return RedirectToAction(nameof(Index));
        }

        var validCategory = category == "Woodgrain" ? "Woodgrain" : "Solid";
        _db.Patterns.Add(new Pattern { Name = name.Trim(), ReorderTrigger = reorderTrigger > 0 ? reorderTrigger : 5, Category = validCategory });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> EditPattern(int id, string name, int reorderTrigger, string category)
    {
        var pattern = await _db.Patterns.FindAsync(id);
        if (pattern == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Pattern name is required.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.Patterns.AnyAsync(p => p.Name == name.Trim() && p.Id != id))
        {
            TempData["Error"] = $"Pattern '{name}' already exists.";
            return RedirectToAction(nameof(Index));
        }

        pattern.Name = name.Trim();
        pattern.ReorderTrigger = reorderTrigger > 0 ? reorderTrigger : 5;
        pattern.Category = category == "Woodgrain" ? "Woodgrain" : "Solid";
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
        {
            TempData["Error"] = "Cannot delete a pattern that has transactions.";
            return RedirectToAction(nameof(Index));
        }

        _db.Patterns.Remove(pattern);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddDimensionValue(string type, decimal value)
    {
        if (!new[] { "Width", "Length", "Thickness" }.Contains(type))
        {
            TempData["Error"] = "Invalid dimension type.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.DimensionValues.AnyAsync(d => d.Type == type && d.Value == value))
        {
            TempData["Error"] = $"{type} {value} already exists.";
            return RedirectToAction(nameof(Index));
        }

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
        {
            TempData["Error"] = $"Cannot delete {dv.Type} {dv.Value} — it is used in existing inventory records.";
            return RedirectToAction(nameof(Index));
        }

        _db.DimensionValues.Remove(dv);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddSheetPricing(string category, decimal thickness, decimal tier1Price, decimal tier2Price, decimal tier3Price)
    {
        var validCategory = category == "Woodgrain" ? "Woodgrain" : "Solid";
        var existing = (await _db.SheetPricings.ToListAsync())
            .Any(sp => sp.Category == validCategory && sp.Thickness == thickness);

        if (existing)
        {
            TempData["Error"] = $"Sheet pricing for {validCategory} at thickness {thickness} already exists.";
            return RedirectToAction(nameof(Index));
        }

        _db.SheetPricings.Add(new SheetPricing
        {
            Category = validCategory,
            Thickness = thickness,
            Tier1Price = tier1Price,
            Tier2Price = tier2Price,
            Tier3Price = tier3Price
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSheetPricing(int id, decimal tier1Price, decimal tier2Price, decimal tier3Price)
    {
        var pricing = await _db.SheetPricings.FindAsync(id);
        if (pricing == null) return NotFound();

        pricing.Tier1Price = tier1Price;
        pricing.Tier2Price = tier2Price;
        pricing.Tier3Price = tier3Price;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSheetPricing(int id)
    {
        var pricing = await _db.SheetPricings.FindAsync(id);
        if (pricing == null) return NotFound();

        _db.SheetPricings.Remove(pricing);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static string FormatVersion()
    {
        var sha = Environment.GetEnvironmentVariable("APP_BUILD_SHA");
        if (!string.IsNullOrEmpty(sha) && sha != "dev")
        {
            var shortSha = sha.Length > 7 ? sha[..7] : sha;
            return $"1.1 (build {shortSha})";
        }
        return "1.1 (dev)";
    }
}
