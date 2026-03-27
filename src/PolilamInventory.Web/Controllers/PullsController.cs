using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Web.Controllers;

public class PullsController : Controller
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventoryService;
    private readonly SizeService _sizeService;
    private readonly PricingService _pricingService;

    public PullsController(AppDbContext db, InventoryService inventoryService, SizeService sizeService, PricingService pricingService)
    {
        _db = db;
        _inventoryService = inventoryService;
        _sizeService = sizeService;
        _pricingService = pricingService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var actualPulls = await _db.ActualPulls
            .Include(p => p.Pattern)
            .Include(p => p.Size)
            .ToListAsync();

        var plannedClaims = await _db.PlannedClaims
            .Include(c => c.Pattern)
            .Include(c => c.Size)
            .ToListAsync();

        // Collect unique pattern+size combos and compute WAC for each
        var combos = actualPulls.Select(p => (p.PatternId, p.SizeId))
            .Concat(plannedClaims.Select(c => (c.PatternId, c.SizeId)))
            .Distinct()
            .ToList();

        var wacLookup = new Dictionary<(int, int), decimal>();
        foreach (var (patternId, sizeId) in combos)
        {
            var (wac, _) = await _pricingService.GetWeightedAverageCost(patternId, sizeId);
            wacLookup[(patternId, sizeId)] = wac;
        }

        decimal? toSqFt(decimal wac, Models.Size size)
        {
            var sqFt = size.Width * size.Length / 144.0m;
            return wac > 0 && sqFt > 0 ? Math.Round(wac / sqFt, 2) : null;
        }

        var rows = actualPulls.Select(p => new PullRow
        {
            Id = p.Id,
            Type = "Pulled",
            PatternName = p.Pattern.Name,
            SizeDisplay = p.Size.DisplayName,
            Quantity = p.Quantity,
            Date = p.PullDate,
            SoNumber = p.SoNumber,
            Note = p.Note,
            CanEdit = false,
            CostPerSheet = wacLookup.GetValueOrDefault((p.PatternId, p.SizeId)),
            CostPerSqFt = toSqFt(wacLookup.GetValueOrDefault((p.PatternId, p.SizeId)), p.Size)
        })
        .Concat(plannedClaims.Select(c => new PullRow
        {
            Id = c.Id,
            Type = "Will Pull",
            PatternName = c.Pattern.Name,
            SizeDisplay = c.Size.DisplayName,
            Quantity = c.Quantity,
            Date = c.ScheduledDate,
            SoNumber = c.SoNumber,
            Note = c.Note,
            CanEdit = true,
            CostPerSheet = wacLookup.GetValueOrDefault((c.PatternId, c.SizeId)),
            CostPerSqFt = toSqFt(wacLookup.GetValueOrDefault((c.PatternId, c.SizeId)), c.Size)
        }))
        .OrderByDescending(r => r.Date)
        .ToList();

        return View(new PullsIndexViewModel { Rows = rows });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var claim = await _db.PlannedClaims
            .Include(c => c.Pattern)
            .Include(c => c.Size)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim == null) return NotFound();

        var vm = new EditPlannedPullViewModel
        {
            Id = claim.Id,
            PatternName = claim.Pattern.Name,
            SizeDisplay = claim.Size.DisplayName,
            Quantity = claim.Quantity,
            ScheduledDate = claim.ScheduledDate,
            SoNumber = claim.SoNumber,
            Note = claim.Note
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditPlannedPullViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var claim = await _db.PlannedClaims
            .Include(c => c.Pattern)
            .Include(c => c.Size)
            .FirstOrDefaultAsync(c => c.Id == model.Id);

        if (claim == null) return NotFound();

        claim.Quantity = model.Quantity;
        claim.ScheduledDate = model.ScheduledDate;
        claim.SoNumber = model.SoNumber.Trim();
        claim.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();

        await _db.SaveChangesAsync();

        TempData["Success"] = "Planned pull updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var claim = await _db.PlannedClaims.FindAsync(id);
        if (claim == null) return NotFound();

        _db.PlannedClaims.Remove(claim);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Planned pull cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = await BuildViewModel();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PullSheetsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await RepopulateDropdowns(model);
            return View(model);
        }

        var size = await _sizeService.FindOrCreate(model.Width, model.Length, model.Thickness);

        if (model.Mode == "ScheduleFuture")
        {
            var claim = new PlannedClaim
            {
                PatternId = model.PatternId,
                SizeId = size.Id,
                Quantity = model.Quantity,
                ScheduledDate = model.PullDate,
                SoNumber = model.SoNumber.Trim(),
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
                IsDrop = model.IsDrop
            };
            _db.PlannedClaims.Add(claim);
            await _db.SaveChangesAsync();

            var projection = await _inventoryService.GetProjectedInventory(model.PatternId, size.Id);
            if (projection.ProjectedBalance < 0)
            {
                var pattern = await _db.Patterns.FindAsync(model.PatternId);
                var deficit = Math.Abs(projection.ProjectedBalance);
                TempData["Warning"] = $"This claim would result in a deficit of {deficit} sheets for {pattern!.Name} {size.DisplayName}.";
            }

            return RedirectToAction("Index", "Dashboard");
        }
        else // PullNow
        {
            var currentInventory = await _inventoryService.GetCurrentInventory(model.PatternId, size.Id);
            if (model.Quantity > currentInventory)
            {
                ModelState.AddModelError("Quantity",
                    $"Insufficient inventory. Current stock: {currentInventory}, requested: {model.Quantity}.");
                await RepopulateDropdowns(model);
                return View(model);
            }

            _db.ActualPulls.Add(new ActualPull
            {
                PatternId = model.PatternId,
                SizeId = size.Id,
                Quantity = model.Quantity,
                PullDate = model.PullDate,
                SoNumber = model.SoNumber.Trim(),
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
                IsDrop = model.IsDrop
            });
            await _db.SaveChangesAsync();
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetInventoryImpact(int patternId, decimal width, decimal length, decimal thickness, int quantity)
    {
        var size = await _sizeService.FindOrCreate(width, length, thickness);
        var current = await _inventoryService.GetCurrentInventory(patternId, size.Id);
        var (wac, hasHistory) = await _pricingService.GetWeightedAverageCost(patternId, size.Id);
        var sqFt = width * length / 144.0m;
        var wacPerSqFt = sqFt > 0 ? Math.Round(wac / sqFt, 2) : 0m;
        return Json(new { current, afterPull = current - quantity, wac = Math.Round(wac, 2), wacPerSqFt, hasWacHistory = hasHistory });
    }

    private async Task<PullSheetsViewModel> BuildViewModel()
    {
        var allDimensions = await _db.DimensionValues.ToListAsync();
        return new PullSheetsViewModel
        {
            Patterns = await _db.Patterns.OrderBy(p => p.Name).ToListAsync(),
            Widths = allDimensions.Where(d => d.Type == "Width").OrderBy(d => d.Value).Select(d => d.Value).ToList(),
            Lengths = allDimensions.Where(d => d.Type == "Length").OrderBy(d => d.Value).Select(d => d.Value).ToList(),
            Thicknesses = allDimensions.Where(d => d.Type == "Thickness").OrderBy(d => d.Value).Select(d => d.Value).ToList()
        };
    }

    private async Task RepopulateDropdowns(PullSheetsViewModel model)
    {
        var allDimensions = await _db.DimensionValues.ToListAsync();
        model.Patterns = await _db.Patterns.OrderBy(p => p.Name).ToListAsync();
        model.Widths = allDimensions.Where(d => d.Type == "Width").OrderBy(d => d.Value).Select(d => d.Value).ToList();
        model.Lengths = allDimensions.Where(d => d.Type == "Length").OrderBy(d => d.Value).Select(d => d.Value).ToList();
        model.Thicknesses = allDimensions.Where(d => d.Type == "Thickness").OrderBy(d => d.Value).Select(d => d.Value).ToList();
    }
}
