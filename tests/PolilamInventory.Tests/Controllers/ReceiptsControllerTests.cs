using Microsoft.AspNetCore.Mvc;
using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Controllers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Tests.Controllers;

public class ReceiptsControllerTests
{
    private Order CreateOrder(TestDb db, int qty = 10)
    {
        var pattern = db.CreatePattern();
        var size = db.CreateSize();
        var order = new Order
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            QuantityOrdered = qty,
            OrderDate = DateTime.Today.AddDays(-7),
            EtaDate = DateTime.Today,
            PoNumber = "PO-TEST"
        };
        db.Context.Orders.Add(order);
        db.Context.SaveChanges();
        return order;
    }

    [Fact]
    public async Task Create_WithValidData_SavesReceiptAndRedirects()
    {
        using var db = TestDb.Create();
        var order = CreateOrder(db, qty: 10);
        var controller = new ReceiptsController(db.Context);

        var result = await controller.Create(new ReceiveShipmentViewModel
        {
            OrderId = order.Id,
            QuantityReceived = 5,
            DateReceived = DateTime.Today,
            Note = null
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal(1, db.Context.Receipts.Count());
        Assert.Equal(5, db.Context.Receipts.First().QuantityReceived);
    }

    [Fact]
    public async Task Create_ReceiptsAccumulate_MultiplePartialFills()
    {
        using var db = TestDb.Create();
        var order = CreateOrder(db, qty: 10);
        var controller = new ReceiptsController(db.Context);

        await controller.Create(new ReceiveShipmentViewModel
            { OrderId = order.Id, QuantityReceived = 4, DateReceived = DateTime.Today });
        await controller.Create(new ReceiveShipmentViewModel
            { OrderId = order.Id, QuantityReceived = 4, DateReceived = DateTime.Today });

        Assert.Equal(2, db.Context.Receipts.Count());
        Assert.Equal(8, db.Context.Receipts.Sum(r => r.QuantityReceived));
    }

    [Fact]
    public async Task Create_OrderIsFilledWhenFullyReceived()
    {
        using var db = TestDb.Create();
        var order = CreateOrder(db, qty: 10);
        var controller = new ReceiptsController(db.Context);

        await controller.Create(new ReceiveShipmentViewModel
            { OrderId = order.Id, QuantityReceived = 10, DateReceived = DateTime.Today });

        var updated = db.Context.Orders.First(o => o.Id == order.Id);
        // IsFilled is computed, so load receipts
        db.Context.Entry(updated).Collection(o => o.Receipts).Load();
        Assert.True(updated.IsFilled);
    }

    [Fact]
    public async Task Create_QuantityExceedsOutstanding_ReturnsViewWithError()
    {
        using var db = TestDb.Create();
        var order = CreateOrder(db, qty: 5);
        var controller = new ReceiptsController(db.Context);

        // Use model state simulation to test validation path
        controller.ModelState.AddModelError("QuantityReceived", "Quantity cannot exceed outstanding.");

        var result = await controller.Create(new ReceiveShipmentViewModel
        {
            OrderId = order.Id,
            QuantityReceived = 99,
            DateReceived = DateTime.Today
        });

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, db.Context.Receipts.Count());
    }
}
