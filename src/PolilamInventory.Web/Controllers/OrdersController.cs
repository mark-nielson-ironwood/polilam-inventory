using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Web.Controllers;

public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly SizeService _sizeService;

    public OrdersController(AppDbContext db, SizeService sizeService)
    {
        _db = db;
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
    public async Task<IActionResult> Create(PlaceOrderViewModel model)
    {
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

        _db.Orders.Add(new Order
        {
            PatternId = model.PatternId,
            SizeId = size.Id,
            QuantityOrdered = model.QuantityOrdered,
            OrderDate = model.OrderDate,
            EtaDate = model.EtaDate,
            PoNumber = model.PoNumber?.Trim() ?? string.Empty,
            Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim()
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Dashboard");
    }

    private async Task<PlaceOrderViewModel> BuildViewModel()
    {
        var allDimensions = await _db.DimensionValues.ToListAsync();
        return new PlaceOrderViewModel
        {
            Patterns = await _db.Patterns.OrderBy(p => p.Name).ToListAsync(),
            Widths = allDimensions.Where(d => d.Type == "Width").OrderBy(d => d.Value).Select(d => d.Value).ToList(),
            Lengths = allDimensions.Where(d => d.Type == "Length").OrderBy(d => d.Value).Select(d => d.Value).ToList(),
            Thicknesses = allDimensions.Where(d => d.Type == "Thickness").OrderBy(d => d.Value).Select(d => d.Value).ToList()
        };
    }
}
