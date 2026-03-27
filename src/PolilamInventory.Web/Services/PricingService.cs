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
    /// Perpetual weighted average cost for a pattern+size combination.
    /// Replays all transactions in chronological order:
    ///   NewWAC = (OnHand * OldWAC + IncomingQty * IncomingCost) / (OnHand + IncomingQty)
    /// When on-hand reaches zero, WAC resets on the next incoming event.
    /// Drop transactions are excluded from the WAC calculation.
    /// </summary>
    public async Task<(decimal Wac, bool HasHistory)> GetWeightedAverageCost(int patternId, int sizeId)
    {
        // Build a timeline of all non-drop events that affect purchased on-hand
        var events = new List<(DateTime Date, DateTime CreatedAt, int Qty, decimal? CostPerSheet)>();

        // Receipts: incoming at order's cost
        var receipts = await _db.Receipts
            .Include(r => r.Order)
            .Where(r => r.Order.PatternId == patternId && r.Order.SizeId == sizeId)
            .ToListAsync();

        foreach (var r in receipts)
        {
            events.Add((r.DateReceived, r.CreatedAt, r.QuantityReceived,
                r.Order.CostPerSheet > 0 ? r.Order.CostPerSheet : null));
        }

        // Non-drop adjustments: positive with cost = incoming, negative = outgoing
        var adjustments = await _db.InventoryAdjustments
            .Where(a => a.PatternId == patternId && a.SizeId == sizeId && !a.IsDrop)
            .ToListAsync();

        foreach (var a in adjustments)
        {
            events.Add((a.DateAdded, a.CreatedAt, a.Quantity,
                a.Quantity > 0 && a.CostPerSheet > 0 ? a.CostPerSheet : null));
        }

        // Non-drop pulls: outgoing (reduce on-hand, no cost change)
        var pulls = await _db.ActualPulls
            .Where(p => p.PatternId == patternId && p.SizeId == sizeId && !p.IsDrop)
            .ToListAsync();

        foreach (var p in pulls)
        {
            events.Add((p.PullDate, p.CreatedAt, -p.Quantity, null));
        }

        if (events.Count == 0)
            return (0m, false);

        // Sort by CreatedAt for true chronological order
        events.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));

        // Replay to compute perpetual WAC
        decimal onHand = 0;
        decimal wac = 0;
        bool hasHistory = false;

        foreach (var (_, _, qty, costPerSheet) in events)
        {
            if (qty > 0 && costPerSheet.HasValue)
            {
                // Incoming with cost: recalculate WAC
                var incomingCost = costPerSheet.Value;
                var newTotal = onHand + qty;
                if (newTotal > 0)
                {
                    wac = (onHand * wac + qty * incomingCost) / newTotal;
                }
                onHand = newTotal;
                hasHistory = true;
            }
            else
            {
                // Outgoing (pull or negative adjustment) or incoming without cost
                onHand += qty;
                if (onHand <= 0)
                    onHand = 0; // WAC stays until next incoming event
            }
        }

        return (hasHistory ? Math.Round(wac, 2) : 0m, hasHistory);
    }
}
