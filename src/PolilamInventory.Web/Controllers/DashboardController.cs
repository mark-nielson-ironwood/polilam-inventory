using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Services;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Web.Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventoryService;

    public DashboardController(AppDbContext db, InventoryService inventoryService)
    {
        _db = db;
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index()
    {
        var patterns = await _db.Patterns.OrderBy(p => p.Name).ToListAsync();

        // Collect all distinct pattern+size combos that have any transaction
        var orderCombos = await _db.Orders
            .Select(o => new { o.PatternId, o.SizeId })
            .Distinct()
            .ToListAsync();

        var pullCombos = await _db.ActualPulls
            .Select(p => new { p.PatternId, p.SizeId })
            .Distinct()
            .ToListAsync();

        var claimCombos = await _db.PlannedClaims
            .Select(c => new { c.PatternId, c.SizeId })
            .Distinct()
            .ToListAsync();

        var adjustmentCombos = await _db.InventoryAdjustments
            .Select(a => new { a.PatternId, a.SizeId })
            .Distinct()
            .ToListAsync();

        // Union all combos
        var allCombos = orderCombos
            .Union(pullCombos)
            .Union(claimCombos)
            .Union(adjustmentCombos)
            .Select(c => (c.PatternId, c.SizeId))
            .ToHashSet();

        // Load all sizes for lookup
        var allSizes = await _db.Sizes.ToListAsync();
        var sizeById = allSizes.ToDictionary(s => s.Id);

        // Load open orders for OnOrder calculation (IsFilled is computed in C#, filter in memory)
        var allOrders = await _db.Orders
            .Include(o => o.Receipts)
            .ToListAsync();
        var openOrders = allOrders.Where(o => !o.IsFilled).ToList();

        // Load all planned claims for upcoming section
        var allPlannedClaims = await _db.PlannedClaims
            .Include(c => c.Pattern)
            .Include(c => c.Size)
            .OrderBy(c => c.ScheduledDate)
            .ToListAsync();

        var vm = new DashboardViewModel();

        foreach (var pattern in patterns)
        {
            // Get combos for this pattern
            var patternCombos = allCombos
                .Where(c => c.PatternId == pattern.Id)
                .Select(c => c.SizeId)
                .ToList();

            var sizeBreakdowns = new List<SizeBreakdown>();

            if (patternCombos.Count == 0)
            {
                // Pattern has no transactions — show empty card
                var card = new PatternCard
                {
                    PatternName = pattern.Name,
                    TotalSheets = 0,
                    Status = "healthy",
                    Sizes = new List<SizeBreakdown>()
                };
                vm.PatternCards.Add(card);
                continue;
            }

            foreach (var sizeId in patternCombos)
            {
                if (!sizeById.TryGetValue(sizeId, out var size))
                    continue;

                var inStock = await _inventoryService.GetCurrentInventory(pattern.Id, sizeId);

                var onOrder = openOrders
                    .Where(o => o.PatternId == pattern.Id && o.SizeId == sizeId)
                    .Sum(o => o.QuantityOutstanding);

                string stockStatus;
                if (inStock < 0)
                    stockStatus = "deficit";
                else if (inStock <= pattern.ReorderTrigger)
                    stockStatus = "low-stock";
                else
                    stockStatus = "healthy";

                sizeBreakdowns.Add(new SizeBreakdown
                {
                    SizeDisplay = size.DisplayName,
                    InStock = inStock,
                    OnOrder = onOrder,
                    StockStatus = stockStatus
                });

                // Build alerts for deficit/low-stock
                if (stockStatus == "deficit")
                {
                    vm.Alerts.Add(new AlertItem
                    {
                        PatternName = pattern.Name,
                        SizeDisplay = size.DisplayName,
                        Message = $"Deficit: {inStock} in stock",
                        Severity = "danger"
                    });
                }
                else if (stockStatus == "low-stock")
                {
                    vm.Alerts.Add(new AlertItem
                    {
                        PatternName = pattern.Name,
                        SizeDisplay = size.DisplayName,
                        Message = $"Low stock: {inStock} in stock (reorder trigger: {pattern.ReorderTrigger})",
                        Severity = "warning"
                    });
                }
            }

            // Sort sizes by ascending Thickness
            var orderedSizes = sizeBreakdowns
                .Join(allSizes, sb => sb.SizeDisplay, s => s.DisplayName, (sb, s) => new { sb, s.Thickness })
                .OrderBy(x => x.Thickness)
                .Select(x => x.sb)
                .ToList();

            // Determine worst status
            string patternStatus = "healthy";
            if (orderedSizes.Any(s => s.StockStatus == "deficit"))
                patternStatus = "deficit";
            else if (orderedSizes.Any(s => s.StockStatus == "low-stock"))
                patternStatus = "low-stock";

            vm.PatternCards.Add(new PatternCard
            {
                PatternName = pattern.Name,
                TotalSheets = orderedSizes.Sum(s => s.InStock),
                Status = patternStatus,
                Sizes = orderedSizes
            });
        }

        // Build upcoming claims
        foreach (var claim in allPlannedClaims)
        {
            var projection = await _inventoryService.GetProjectedInventory(claim.PatternId, claim.SizeId);
            vm.UpcomingClaims.Add(new UpcomingClaim
            {
                PatternName = claim.Pattern.Name,
                SizeDisplay = claim.Size.DisplayName,
                Quantity = claim.Quantity,
                ScheduledDate = claim.ScheduledDate,
                SoNumber = claim.SoNumber,
                IsDeficit = projection.ProjectedBalance < 0
            });
        }

        return View(vm);
    }
}
