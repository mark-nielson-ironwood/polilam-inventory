using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Services;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Web.Controllers;

public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventoryService;
    private readonly ReportExportService _exportService;

    public ReportsController(AppDbContext db, InventoryService inventoryService, ReportExportService exportService)
    {
        _db = db;
        _inventoryService = inventoryService;
        _exportService = exportService;
    }

    // ─── Display actions ──────────────────────────────────────────────────

    public async Task<IActionResult> Inventory(string? patternFilter)
    {
        var rows = await GetInventoryRows(patternFilter);
        var patternNames = await _db.Patterns.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync();

        var vm = new InventoryReportViewModel
        {
            PatternFilter = patternFilter,
            PatternNames = patternNames,
            Rows = rows
        };

        return View(vm);
    }

    public async Task<IActionResult> Removal(DateTime? startDate, DateTime? endDate, bool includeInactive = false)
    {
        var today = DateTime.Today;
        startDate ??= new DateTime(today.Year, today.Month, 1);
        endDate ??= today;

        var rows = await GetRemovalRows(startDate, endDate, includeInactive);
        var patternNames = await _db.Patterns.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync();

        var vm = new RemovalReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            IncludeInactive = includeInactive,
            PatternNames = patternNames,
            Rows = rows
        };

        return View(vm);
    }

    public async Task<IActionResult> Transactions(string? patternFilter, DateTime? startDate, DateTime? endDate)
    {
        var today = DateTime.Today;
        startDate ??= today.AddDays(-30);
        endDate ??= today;

        var rows = await GetTransactionRows(patternFilter, startDate, endDate);
        var patternNames = await _db.Patterns.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync();

        var vm = new TransactionReportViewModel
        {
            PatternFilter = patternFilter,
            StartDate = startDate,
            EndDate = endDate,
            PatternNames = patternNames,
            Rows = rows
        };

        return View(vm);
    }

    // ─── Export actions ───────────────────────────────────────────────────

    public async Task<IActionResult> InventoryCsv(string? patternFilter)
    {
        var rows = await GetInventoryRows(patternFilter);
        var bytes = _exportService.GenerateInventoryCsv(rows);
        return File(bytes, "text/csv", "inventory-report.csv");
    }

    public async Task<IActionResult> InventoryPdf(string? patternFilter)
    {
        var rows = await GetInventoryRows(patternFilter);
        var bytes = _exportService.GenerateInventoryPdf(rows, patternFilter);
        return File(bytes, "application/pdf", "inventory-report.pdf");
    }

    public async Task<IActionResult> RemovalCsv(DateTime? startDate, DateTime? endDate, bool includeInactive = false)
    {
        var today = DateTime.Today;
        startDate ??= new DateTime(today.Year, today.Month, 1);
        endDate ??= today;

        var rows = await GetRemovalRows(startDate, endDate, includeInactive);
        var bytes = _exportService.GenerateRemovalCsv(rows);
        return File(bytes, "text/csv", "removal-report.csv");
    }

    public async Task<IActionResult> RemovalPdf(DateTime? startDate, DateTime? endDate, bool includeInactive = false)
    {
        var today = DateTime.Today;
        startDate ??= new DateTime(today.Year, today.Month, 1);
        endDate ??= today;

        var rows = await GetRemovalRows(startDate, endDate, includeInactive);
        var bytes = _exportService.GenerateRemovalPdf(rows, startDate, endDate);
        return File(bytes, "application/pdf", "removal-report.pdf");
    }

    public async Task<IActionResult> TransactionCsv(string? patternFilter, DateTime? startDate, DateTime? endDate)
    {
        var today = DateTime.Today;
        startDate ??= today.AddDays(-30);
        endDate ??= today;

        var rows = await GetTransactionRows(patternFilter, startDate, endDate);
        var bytes = _exportService.GenerateTransactionCsv(rows);
        return File(bytes, "text/csv", "transactions-report.csv");
    }

    public async Task<IActionResult> TransactionPdf(string? patternFilter, DateTime? startDate, DateTime? endDate)
    {
        var today = DateTime.Today;
        startDate ??= today.AddDays(-30);
        endDate ??= today;

        var rows = await GetTransactionRows(patternFilter, startDate, endDate);
        var bytes = _exportService.GenerateTransactionPdf(rows, patternFilter, startDate, endDate);
        return File(bytes, "application/pdf", "transactions-report.pdf");
    }

    // ─── Private query helpers ────────────────────────────────────────────

    private async Task<List<InventoryReportRow>> GetInventoryRows(string? patternFilter)
    {
        var patterns = await _db.Patterns.OrderBy(p => p.Name).ToListAsync();

        var orderCombos = await _db.Orders.Select(o => new { o.PatternId, o.SizeId }).Distinct().ToListAsync();
        var pullCombos = await _db.ActualPulls.Select(p => new { p.PatternId, p.SizeId }).Distinct().ToListAsync();
        var claimCombos = await _db.PlannedClaims.Select(c => new { c.PatternId, c.SizeId }).Distinct().ToListAsync();
        var adjustmentCombos = await _db.InventoryAdjustments.Select(a => new { a.PatternId, a.SizeId }).Distinct().ToListAsync();

        var allCombos = orderCombos
            .Union(pullCombos)
            .Union(claimCombos)
            .Union(adjustmentCombos)
            .Select(c => (c.PatternId, c.SizeId))
            .ToHashSet();

        var allSizes = await _db.Sizes.ToListAsync();
        var sizeById = allSizes.ToDictionary(s => s.Id);

        var orderDates = await _db.Orders.Select(o => new { o.PatternId, o.SizeId, Date = o.OrderDate }).ToListAsync();
        var receiptDates = await _db.Receipts.Include(r => r.Order)
            .Select(r => new { r.Order.PatternId, r.Order.SizeId, Date = r.DateReceived }).ToListAsync();
        var pullDates = await _db.ActualPulls.Select(p => new { p.PatternId, p.SizeId, Date = p.PullDate }).ToListAsync();
        var adjDates = await _db.InventoryAdjustments.Select(a => new { a.PatternId, a.SizeId, Date = a.DateAdded }).ToListAsync();
        var claimDates = await _db.PlannedClaims.Select(c => new { c.PatternId, c.SizeId, Date = c.ScheduledDate }).ToListAsync();

        var allDates = orderDates
            .Concat(receiptDates.Select(r => new { r.PatternId, r.SizeId, r.Date }))
            .Concat(pullDates.Select(p => new { p.PatternId, p.SizeId, p.Date }))
            .Concat(adjDates.Select(a => new { a.PatternId, a.SizeId, a.Date }))
            .Concat(claimDates.Select(c => new { c.PatternId, c.SizeId, c.Date }))
            .GroupBy(x => (x.PatternId, x.SizeId))
            .ToDictionary(g => g.Key, g => g.Max(x => x.Date));

        var openOrders = (await _db.Orders
            .Include(o => o.Receipts)
            .ToListAsync())
            .Where(o => !o.IsFilled)
            .ToList();

        var allClaims = await _db.PlannedClaims.ToListAsync();

        var rows = new List<InventoryReportRow>();

        var filteredPatterns = string.IsNullOrWhiteSpace(patternFilter)
            ? patterns
            : patterns.Where(p => p.Name == patternFilter).ToList();

        foreach (var pattern in filteredPatterns)
        {
            var patternCombos = allCombos
                .Where(c => c.PatternId == pattern.Id)
                .Select(c => c.SizeId)
                .ToList();

            foreach (var sizeId in patternCombos)
            {
                if (!sizeById.TryGetValue(sizeId, out var size))
                    continue;

                var inStock = await _inventoryService.GetCurrentInventory(pattern.Id, sizeId);

                var patternOpenOrders = openOrders
                    .Where(o => o.PatternId == pattern.Id && o.SizeId == sizeId)
                    .ToList();

                var onOrder = patternOpenOrders.Sum(o => o.QuantityOutstanding);
                var orderDate = patternOpenOrders.Any() ? patternOpenOrders.Min(o => o.OrderDate) : (DateTime?)null;
                var eta = patternOpenOrders.Any() ? patternOpenOrders.Min(o => o.EtaDate) : (DateTime?)null;

                var patternClaims = allClaims
                    .Where(c => c.PatternId == pattern.Id && c.SizeId == sizeId)
                    .ToList();

                var totalCommitted = patternClaims.Sum(c => c.Quantity);
                var committedBeforeArrival = eta.HasValue
                    ? patternClaims.Where(c => c.ScheduledDate < eta.Value).Sum(c => c.Quantity)
                    : 0;
                var projectedAtArrival = inStock - committedBeforeArrival;
                var projectedBalance = inStock + onOrder - totalCommitted;

                allDates.TryGetValue((pattern.Id, sizeId), out var lastAdjDate);

                rows.Add(new InventoryReportRow
                {
                    PatternName = pattern.Name,
                    SizeDisplay = size.DisplayName,
                    Thickness = size.Thickness,
                    InStock = inStock,
                    LastAdjDate = lastAdjDate == default ? null : lastAdjDate,
                    OnOrder = onOrder,
                    OrderDate = orderDate,
                    Eta = eta,
                    CommittedBeforeArrival = committedBeforeArrival,
                    ProjectedAtArrival = projectedAtArrival,
                    TotalCommitted = totalCommitted,
                    ProjectedBalance = projectedBalance,
                    NeedsReorder = projectedBalance <= pattern.ReorderTrigger,
                    ReorderTrigger = pattern.ReorderTrigger
                });
            }
        }

        return rows.OrderBy(r => r.PatternName).ThenBy(r => r.Thickness).ToList();
    }

    private async Task<List<RemovalReportRow>> GetRemovalRows(DateTime? startDate, DateTime? endDate, bool includeInactive)
    {
        var pulls = await _db.ActualPulls
            .Include(p => p.Pattern)
            .Include(p => p.Size)
            .Where(p => p.PullDate >= startDate!.Value && p.PullDate <= endDate!.Value)
            .ToListAsync();

        var grouped = pulls
            .GroupBy(p => (p.PatternId, p.SizeId, p.Pattern.Name, p.Size.DisplayName, p.Size.Thickness))
            .Select(g => new RemovalReportRow
            {
                PatternName = g.Key.Name,
                SizeDisplay = g.Key.DisplayName,
                Thickness = g.Key.Thickness,
                SheetsRemoved = g.Sum(p => p.Quantity),
                LastRemovalDate = g.Max(p => p.PullDate)
            })
            .ToList();

        if (!includeInactive)
        {
            grouped = grouped.Where(r => r.SheetsRemoved > 0).ToList();
        }
        else
        {
            var allCombos = await _db.Orders.Select(o => new { o.PatternId, o.SizeId })
                .Union(_db.ActualPulls.Select(p => new { p.PatternId, p.SizeId }))
                .Union(_db.PlannedClaims.Select(c => new { c.PatternId, c.SizeId }))
                .Union(_db.InventoryAdjustments.Select(a => new { a.PatternId, a.SizeId }))
                .Distinct()
                .ToListAsync();

            var existingKeys = grouped.Select(r => (r.PatternName, r.SizeDisplay)).ToHashSet();

            var patterns = await _db.Patterns.ToListAsync();
            var sizes = await _db.Sizes.ToDictionaryAsync(s => s.Id);
            var patternById = patterns.ToDictionary(p => p.Id);

            foreach (var combo in allCombos)
            {
                if (!patternById.TryGetValue(combo.PatternId, out var pattern)) continue;
                if (!sizes.TryGetValue(combo.SizeId, out var size)) continue;

                if (!existingKeys.Contains((pattern.Name, size.DisplayName)))
                {
                    grouped.Add(new RemovalReportRow
                    {
                        PatternName = pattern.Name,
                        SizeDisplay = size.DisplayName,
                        Thickness = size.Thickness,
                        SheetsRemoved = 0,
                        LastRemovalDate = null
                    });
                }
            }
        }

        return grouped.OrderBy(r => r.PatternName).ThenBy(r => r.Thickness).ToList();
    }

    private async Task<List<TransactionReportRow>> GetTransactionRows(string? patternFilter, DateTime? startDate, DateTime? endDate)
    {
        var rows = new List<TransactionReportRow>();

        // Orders
        var ordersQuery = _db.Orders
            .Include(o => o.Pattern)
            .Include(o => o.Size)
            .Where(o => o.OrderDate >= startDate!.Value && o.OrderDate <= endDate!.Value);
        if (!string.IsNullOrWhiteSpace(patternFilter))
            ordersQuery = ordersQuery.Where(o => o.Pattern.Name == patternFilter);

        var orders = await ordersQuery.ToListAsync();
        rows.AddRange(orders.Select(o => new TransactionReportRow
        {
            Date = o.OrderDate,
            Type = "Order",
            PatternName = o.Pattern.Name,
            SizeDisplay = o.Size.DisplayName,
            Quantity = o.QuantityOrdered,
            PoSoNumber = o.PoNumber,
            Note = o.Note
        }));

        // Receipts
        var receiptsQuery = _db.Receipts
            .Include(r => r.Order).ThenInclude(o => o.Pattern)
            .Include(r => r.Order).ThenInclude(o => o.Size)
            .Where(r => r.DateReceived >= startDate!.Value && r.DateReceived <= endDate!.Value);
        if (!string.IsNullOrWhiteSpace(patternFilter))
            receiptsQuery = receiptsQuery.Where(r => r.Order.Pattern.Name == patternFilter);

        var receipts = await receiptsQuery.ToListAsync();
        rows.AddRange(receipts.Select(r => new TransactionReportRow
        {
            Date = r.DateReceived,
            Type = "Receipt",
            PatternName = r.Order.Pattern.Name,
            SizeDisplay = r.Order.Size.DisplayName,
            Quantity = r.QuantityReceived,
            PoSoNumber = r.Order.PoNumber,
            Note = r.Note
        }));

        // ActualPulls
        var pullsQuery = _db.ActualPulls
            .Include(p => p.Pattern)
            .Include(p => p.Size)
            .Where(p => p.PullDate >= startDate!.Value && p.PullDate <= endDate!.Value);
        if (!string.IsNullOrWhiteSpace(patternFilter))
            pullsQuery = pullsQuery.Where(p => p.Pattern.Name == patternFilter);

        var pulls = await pullsQuery.ToListAsync();
        rows.AddRange(pulls.Select(p => new TransactionReportRow
        {
            Date = p.PullDate,
            Type = "Pull",
            PatternName = p.Pattern.Name,
            SizeDisplay = p.Size.DisplayName,
            Quantity = -p.Quantity,
            PoSoNumber = p.SoNumber,
            Note = p.Note
        }));

        // InventoryAdjustments
        var adjQuery = _db.InventoryAdjustments
            .Include(a => a.Pattern)
            .Include(a => a.Size)
            .Where(a => a.DateAdded >= startDate!.Value && a.DateAdded <= endDate!.Value);
        if (!string.IsNullOrWhiteSpace(patternFilter))
            adjQuery = adjQuery.Where(a => a.Pattern.Name == patternFilter);

        var adjustments = await adjQuery.ToListAsync();
        rows.AddRange(adjustments.Select(a => new TransactionReportRow
        {
            Date = a.DateAdded,
            Type = "Initial",
            PatternName = a.Pattern.Name,
            SizeDisplay = a.Size.DisplayName,
            Quantity = a.Quantity,
            PoSoNumber = null,
            Note = a.Note
        }));

        // PlannedClaims
        var claimsQuery = _db.PlannedClaims
            .Include(c => c.Pattern)
            .Include(c => c.Size)
            .Where(c => c.ScheduledDate >= startDate!.Value && c.ScheduledDate <= endDate!.Value);
        if (!string.IsNullOrWhiteSpace(patternFilter))
            claimsQuery = claimsQuery.Where(c => c.Pattern.Name == patternFilter);

        var claims = await claimsQuery.ToListAsync();
        rows.AddRange(claims.Select(c => new TransactionReportRow
        {
            Date = c.ScheduledDate,
            Type = "Planned",
            PatternName = c.Pattern.Name,
            SizeDisplay = c.Size.DisplayName,
            Quantity = -c.Quantity,
            PoSoNumber = c.SoNumber,
            Note = c.Note
        }));

        return rows.OrderByDescending(r => r.Date).ToList();
    }
}
