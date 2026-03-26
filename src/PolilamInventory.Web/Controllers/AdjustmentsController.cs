using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Web.Controllers;

public class AdjustmentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly SizeService _sizeService;
    private readonly InventoryService _inventoryService;

    public AdjustmentsController(AppDbContext db, SizeService sizeService, InventoryService inventoryService)
    {
        _db = db;
        _sizeService = sizeService;
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = await BuildViewModel();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdjustInventoryViewModel model)
    {
        if (model.Quantity == 0)
        {
            ModelState.AddModelError("Quantity", "Quantity must not be zero.");
        }

        if (!ModelState.IsValid)
        {
            model.Patterns = await _db.Patterns.OrderBy(p => p.Name).ToListAsync();
            var allDims = await _db.DimensionValues.ToListAsync();
            model.Widths = allDims.Where(d => d.Type == "Width").OrderBy(d => d.Value).Select(d => d.Value).ToList();
            model.Lengths = allDims.Where(d => d.Type == "Length").OrderBy(d => d.Value).Select(d => d.Value).ToList();
            model.Thicknesses = allDims.Where(d => d.Type == "Thickness").OrderBy(d => d.Value).Select(d => d.Value).ToList();
            return View(model);
        }

        var size = await _sizeService.FindOrCreate(model.Width, model.Length, model.Thickness);

        _db.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = model.PatternId,
            SizeId = size.Id,
            Quantity = model.Quantity,
            DateAdded = model.DateAdded,
            Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
            IsDrop = model.IsDrop
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentInventory(int patternId, decimal width, decimal length, decimal thickness)
    {
        var size = await _db.Sizes
            .FirstOrDefaultAsync(s => s.Width == width && s.Length == length && s.Thickness == thickness);

        if (size == null)
            return Json(new { current = 0 });

        var current = await _inventoryService.GetCurrentInventory(patternId, size.Id);
        return Json(new { current });
    }

    private async Task<AdjustInventoryViewModel> BuildViewModel()
    {
        var allDimensions = await _db.DimensionValues.ToListAsync();
        return new AdjustInventoryViewModel
        {
            Patterns = await _db.Patterns.OrderBy(p => p.Name).ToListAsync(),
            Widths = allDimensions.Where(d => d.Type == "Width").OrderBy(d => d.Value).Select(d => d.Value).ToList(),
            Lengths = allDimensions.Where(d => d.Type == "Length").OrderBy(d => d.Value).Select(d => d.Value).ToList(),
            Thicknesses = allDimensions.Where(d => d.Type == "Thickness").OrderBy(d => d.Value).Select(d => d.Value).ToList()
        };
    }
}
