using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Services;

namespace PolilamInventory.Web.Controllers.Api;

[Route("api/context")]
[ApiController]
public class ContextApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventoryService;

    public ContextApiController(AppDbContext db, InventoryService inventoryService)
    {
        _db = db;
        _inventoryService = inventoryService;
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

        var inventoryRows = new List<object>();
        foreach (var size in sizes)
        {
            var qty = await _inventoryService.GetCurrentInventory(patternId, size.Id);
            inventoryRows.Add(new
            {
                sizeDisplay = size.DisplayName,
                inStock = qty
            });
        }

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
