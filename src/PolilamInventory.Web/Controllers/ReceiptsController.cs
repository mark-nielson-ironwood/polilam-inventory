using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Web.Controllers;

public class ReceiptsController : Controller
{
    private readonly AppDbContext _db;

    public ReceiptsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new ReceiveShipmentViewModel
        {
            OpenOrders = await GetOpenOrders()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Pattern)
            .Include(o => o.Size)
            .Include(o => o.Receipts)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        return Json(new
        {
            patternName = order.Pattern.Name,
            sizeDisplay = order.Size.DisplayName,
            quantityOrdered = order.QuantityOrdered,
            quantityReceived = order.QuantityReceived,
            quantityOutstanding = order.QuantityOutstanding,
            eta = order.EtaDate.ToString("MM/dd/yyyy"),
            receipts = order.Receipts.OrderByDescending(r => r.DateReceived).Select(r => new
            {
                date = r.DateReceived.ToString("MM/dd/yyyy"),
                qty = r.QuantityReceived,
                note = r.Note
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReceiveShipmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.OpenOrders = await GetOpenOrders();
            return View(model);
        }

        // Validate quantity doesn't exceed outstanding
        var order = await _db.Orders
            .Include(o => o.Receipts)
            .FirstOrDefaultAsync(o => o.Id == model.OrderId);

        if (order == null)
        {
            ModelState.AddModelError("", "Order not found.");
            model.OpenOrders = await GetOpenOrders();
            return View(model);
        }

        if (model.QuantityReceived > order.QuantityOutstanding)
        {
            ModelState.AddModelError("QuantityReceived",
                $"Quantity cannot exceed outstanding balance of {order.QuantityOutstanding}.");
            model.OpenOrders = await GetOpenOrders();
            return View(model);
        }

        _db.Receipts.Add(new Receipt
        {
            OrderId = model.OrderId,
            QuantityReceived = model.QuantityReceived,
            DateReceived = model.DateReceived,
            Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim()
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Dashboard");
    }

    private async Task<List<Order>> GetOpenOrders()
    {
        return await _db.Orders
            .Include(o => o.Pattern)
            .Include(o => o.Size)
            .Include(o => o.Receipts)
            .Where(o => o.QuantityOrdered > o.Receipts.Sum(r => r.QuantityReceived))
            .OrderBy(o => o.EtaDate)
            .ToListAsync();
    }
}
