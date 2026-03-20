using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;

namespace PolilamInventory.Web.Services;

public class InventoryProjection
{
    public int CurrentInventory { get; set; }
    public int OnOrder { get; set; }
    public DateTime? EarliestEta { get; set; }
    public DateTime? EarliestOrderDate { get; set; }
    public int CommittedBeforeArrival { get; set; }
    public int ProjectedAtArrival { get; set; }
    public int TotalCommitted { get; set; }
    public int ProjectedBalance { get; set; }
}

public class InventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetCurrentInventory(int patternId, int sizeId)
    {
        var adjustments = await _db.InventoryAdjustments
            .Where(a => a.PatternId == patternId && a.SizeId == sizeId)
            .SumAsync(a => a.Quantity);

        var receipts = await _db.Receipts
            .Where(r => r.Order.PatternId == patternId && r.Order.SizeId == sizeId)
            .SumAsync(r => r.QuantityReceived);

        var pulls = await _db.ActualPulls
            .Where(p => p.PatternId == patternId && p.SizeId == sizeId)
            .SumAsync(p => p.Quantity);

        return adjustments + receipts - pulls;
    }

    public async Task<InventoryProjection> GetProjectedInventory(int patternId, int sizeId)
    {
        var currentInventory = await GetCurrentInventory(patternId, sizeId);

        var openOrders = await _db.Orders
            .Include(o => o.Receipts)
            .Where(o => o.PatternId == patternId && o.SizeId == sizeId)
            .ToListAsync();

        var unfilledOrders = openOrders.Where(o => !o.IsFilled).ToList();
        var onOrder = unfilledOrders.Sum(o => o.QuantityOutstanding);
        var earliestEta = unfilledOrders.Any() ? unfilledOrders.Min(o => o.EtaDate) : (DateTime?)null;
        var earliestOrderDate = unfilledOrders.Any() ? unfilledOrders.Min(o => o.OrderDate) : (DateTime?)null;

        var allClaims = await _db.PlannedClaims
            .Where(c => c.PatternId == patternId && c.SizeId == sizeId)
            .ToListAsync();

        var totalCommitted = allClaims.Sum(c => c.Quantity);
        var committedBeforeArrival = earliestEta.HasValue
            ? allClaims.Where(c => c.ScheduledDate < earliestEta.Value).Sum(c => c.Quantity)
            : 0;

        return new InventoryProjection
        {
            CurrentInventory = currentInventory,
            OnOrder = onOrder,
            EarliestEta = earliestEta,
            EarliestOrderDate = earliestOrderDate,
            CommittedBeforeArrival = committedBeforeArrival,
            ProjectedAtArrival = currentInventory - committedBeforeArrival,
            TotalCommitted = totalCommitted,
            ProjectedBalance = currentInventory + onOrder - totalCommitted
        };
    }
}
