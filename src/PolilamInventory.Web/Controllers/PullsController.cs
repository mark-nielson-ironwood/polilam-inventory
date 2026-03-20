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

    public PullsController(AppDbContext db, InventoryService inventoryService, SizeService sizeService)
    {
        _db = db;
        _inventoryService = inventoryService;
        _sizeService = sizeService;
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
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim()
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
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim()
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
        return Json(new { current, afterPull = current - quantity });
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
