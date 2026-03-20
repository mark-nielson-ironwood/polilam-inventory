using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;

namespace PolilamInventory.Web.Controllers.Api;

[Route("api/context")]
[ApiController]
public class ContextApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public ContextApiController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("pattern/{patternId:int}")]
    public async Task<IActionResult> GetPatternContext(int patternId)
    {
        // Get all sizes that have any activity for this pattern
        var sizeIds = await _db.Orders.Where(o => o.PatternId == patternId).Select(o => o.SizeId)
            .Union(_db.ActualPulls.Where(p => p.PatternId == patternId).Select(p => p.SizeId))
            .Union(_db.PlannedClaims.Where(c => c.PatternId == patternId).Select(c => c.SizeId))
            .Union(_db.InventoryAdjustments.Where(a => a.PatternId == patternId).Select(a => a.SizeId))
            .Distinct()
            .ToListAsync();

        var sizes = await _db.Sizes
            .Where(s => sizeIds.Contains(s.Id))
            .OrderBy(s => s.Thickness)
            .ToListAsync();

        // Batch: sum adjustments, receipts, pulls per size in one query each
        var adjustmentsBySize = await _db.InventoryAdjustments
            .Where(a => a.PatternId == patternId && sizeIds.Contains(a.SizeId))
            .GroupBy(a => a.SizeId)
            .Select(g => new { SizeId = g.Key, Total = g.Sum(a => a.Quantity) })
            .ToListAsync();

        var receiptsBySize = await _db.Receipts
            .Where(r => r.Order.PatternId == patternId && sizeIds.Contains(r.Order.SizeId))
            .GroupBy(r => r.Order.SizeId)
            .Select(g => new { SizeId = g.Key, Total = g.Sum(r => r.QuantityReceived) })
            .ToListAsync();

        var pullsBySize = await _db.ActualPulls
            .Where(p => p.PatternId == patternId && sizeIds.Contains(p.SizeId))
            .GroupBy(p => p.SizeId)
            .Select(g => new { SizeId = g.Key, Total = g.Sum(p => p.Quantity) })
            .ToListAsync();

        var inventoryRows = sizes.Select(size =>
        {
            var adj = adjustmentsBySize.FirstOrDefault(a => a.SizeId == size.Id)?.Total ?? 0;
            var rec = receiptsBySize.FirstOrDefault(r => r.SizeId == size.Id)?.Total ?? 0;
            var pul = pullsBySize.FirstOrDefault(p => p.SizeId == size.Id)?.Total ?? 0;
            return new { sizeDisplay = size.DisplayName, inStock = adj + rec - pul };
        }).ToList<object>();

        // Open orders for this pattern
        var openOrders = await _db.Orders
            .Include(o => o.Receipts)
            .Include(o => o.Size)
            .Where(o => o.PatternId == patternId)
            .ToListAsync();

        var orderRows = openOrders
            .Where(o => !o.IsFilled)
            .OrderBy(o => o.EtaDate)
            .Select(o => new
            {
                poNumber = o.PoNumber,
                sizeDisplay = o.Size.DisplayName,
                ordered = o.QuantityOrdered,
                received = o.QuantityReceived,
                outstanding = o.QuantityOutstanding,
                eta = o.EtaDate.ToString("MM/dd/yyyy")
            })
            .ToList<object>();

        return Ok(new { inventory = inventoryRows, openOrders = orderRows });
    }
}
