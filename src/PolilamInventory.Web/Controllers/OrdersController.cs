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
    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders
            .Include(o => o.Pattern)
            .Include(o => o.Size)
            .Include(o => o.Receipts)
            .ToListAsync();

        var sorted = orders.OrderByDescending(o => o.EtaDate).ToList();
        return View(sorted);
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

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Pattern)
            .Include(o => o.Size)
            .Include(o => o.Receipts)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        var vm = new EditOrderViewModel
        {
            Id = order.Id,
            PatternName = order.Pattern.Name,
            SizeDisplay = order.Size.DisplayName,
            QuantityReceived = order.QuantityReceived,
            PoNumber = order.PoNumber,
            QuantityOrdered = order.QuantityOrdered,
            EtaDate = order.EtaDate,
            Note = order.Note
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditOrderViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var order = await _db.Orders
            .Include(o => o.Pattern)
            .Include(o => o.Size)
            .Include(o => o.Receipts)
            .FirstOrDefaultAsync(o => o.Id == model.Id);

        if (order == null)
            return NotFound();

        // Populate display fields for re-render
        model.PatternName = order.Pattern.Name;
        model.SizeDisplay = order.Size.DisplayName;
        model.QuantityReceived = order.QuantityReceived;

        if (model.QuantityOrdered < order.QuantityReceived)
        {
            ModelState.AddModelError(nameof(model.QuantityOrdered),
                $"Quantity cannot be less than the {order.QuantityReceived} sheets already received.");
            return View(model);
        }

        order.PoNumber = model.PoNumber.Trim();
        order.QuantityOrdered = model.QuantityOrdered;
        order.EtaDate = model.EtaDate;
        order.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Order {order.PoNumber} updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Receipts)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        if (order.Receipts.Count > 0)
        {
            TempData["Error"] = $"Cannot cancel order {order.PoNumber} — it has receipts.";
            return RedirectToAction(nameof(Index));
        }

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Order {order.PoNumber} cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseOut(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Receipts)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        order.QuantityOrdered = order.QuantityReceived;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Order {order.PoNumber} closed out.";
        return RedirectToAction(nameof(Index));
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
