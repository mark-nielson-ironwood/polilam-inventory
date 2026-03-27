using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Web.Services;

public class PricingService
{
    private readonly AppDbContext _db;

    public PricingService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Calculate the cost per sheet from the pricing table.
    /// tierPrice * (width * length / 144)
    /// </summary>
    public async Task<decimal?> CalculateCostPerSheet(int patternId, decimal width, decimal length, decimal thickness, int quantity)
    {
        var pattern = await _db.Patterns.FindAsync(patternId);
        if (pattern == null) return null;

        var pricing = (await _db.SheetPricings.ToListAsync())
            .FirstOrDefault(p => p.Category == pattern.Category && p.Thickness == thickness);

        if (pricing == null) return null;

        var tierPrice = GetTierPrice(pricing, quantity);
        return (width * length / 144.0m) * tierPrice;
    }

    /// <summary>
    /// Get the tier price per sqft based on quantity.
    /// </summary>
    public static decimal GetTierPrice(SheetPricing pricing, int quantity)
    {
        if (quantity >= 100) return pricing.Tier3Price;
        if (quantity >= 50) return pricing.Tier2Price;
        return pricing.Tier1Price;
    }

    /// <summary>
    /// Weighted average cost for a pattern+size combination.
    /// WAC = sum(qty * costPerSheet) / sum(qty)
    /// Sources: Receipts (via Order.CostPerSheet) and non-drop InventoryAdjustments with CostPerSheet > 0.
    /// Only positive quantities included.
    /// </summary>
    public async Task<(decimal Wac, bool HasHistory)> GetWeightedAverageCost(int patternId, int sizeId)
    {
        var weightedItems = new List<(int Qty, decimal Cost)>();

        // Receipts: qty = QuantityReceived, cost = Order.CostPerSheet
        var receipts = await _db.Receipts
            .Include(r => r.Order)
            .Where(r => r.Order.PatternId == patternId && r.Order.SizeId == sizeId)
            .Where(r => r.Order.CostPerSheet != null && r.Order.CostPerSheet > 0)
            .ToListAsync();

        foreach (var r in receipts)
        {
            if (r.QuantityReceived > 0)
                weightedItems.Add((r.QuantityReceived, r.Order.CostPerSheet!.Value));
        }

        // Non-drop adjustments with CostPerSheet > 0 and positive quantity
        var adjustments = await _db.InventoryAdjustments
            .Where(a => a.PatternId == patternId && a.SizeId == sizeId)
            .Where(a => !a.IsDrop && a.CostPerSheet != null && a.CostPerSheet > 0 && a.Quantity > 0)
            .ToListAsync();

        foreach (var a in adjustments)
        {
            weightedItems.Add((a.Quantity, a.CostPerSheet!.Value));
        }

        if (weightedItems.Count == 0)
            return (0m, false);

        var totalQty = weightedItems.Sum(x => x.Qty);
        if (totalQty == 0)
            return (0m, false);

        var totalCost = weightedItems.Sum(x => (decimal)x.Qty * x.Cost);
        return (totalCost / totalQty, true);
    }
}
