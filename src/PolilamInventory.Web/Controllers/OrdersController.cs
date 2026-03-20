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

    public async Task<IActionResult> Create()
    {
        var vm = await BuildViewModel();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int patternId,
        decimal width, decimal length, decimal thickness,
        int quantityOrdered,
        DateTime orderDate,
        DateTime etaDate,
        string poNumber,
        string? note)
    {
        if (!ModelState.IsValid)
            return View(await BuildViewModel());

        var size = await _sizeService.FindOrCreate(width, length, thickness);

        _db.Orders.Add(new Order
        {
            PatternId = patternId,
            SizeId = size.Id,
            QuantityOrdered = quantityOrdered,
            OrderDate = orderDate,
            EtaDate = etaDate,
            PoNumber = poNumber?.Trim() ?? string.Empty,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
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
