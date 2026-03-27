using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Controllers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Tests.Controllers;

public class OrdersControllerTests
{
    [Fact]
    public async Task Create_WithValidData_SavesOrderAndRedirects()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        db.Context.DimensionValues.Add(new DimensionValue { Type = "Width", Value = 60 });
        db.Context.DimensionValues.Add(new DimensionValue { Type = "Length", Value = 144 });
        db.Context.DimensionValues.Add(new DimensionValue { Type = "Thickness", Value = 0.75m });
        db.Context.SaveChanges();

        var controller = new OrdersController(db.Context, new SizeService(db.Context), new PricingService(db.Context));
        var model = new PlaceOrderViewModel
        {
            PatternId = pattern.Id,
            Width = 60, Length = 144, Thickness = 0.75m,
            QuantityOrdered = 10,
            OrderDate = DateTime.Today,
            EtaDate = DateTime.Today.AddDays(30),
            PoNumber = "PO-001",
            Note = null
        };
        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal(1, db.Context.Orders.Count());
        var order = db.Context.Orders.First();
        Assert.Equal(pattern.Id, order.PatternId);
        Assert.Equal(10, order.QuantityOrdered);
        Assert.Equal("PO-001", order.PoNumber);
    }

    [Fact]
    public async Task Create_FindsOrCreatesSize()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        db.Context.DimensionValues.AddRange(
            new DimensionValue { Type = "Width", Value = 60 },
            new DimensionValue { Type = "Length", Value = 144 },
            new DimensionValue { Type = "Thickness", Value = 0.75m }
        );
        db.Context.SaveChanges();

        var controller = new OrdersController(db.Context, new SizeService(db.Context), new PricingService(db.Context));
        var model = new PlaceOrderViewModel
        {
            PatternId = pattern.Id,
            Width = 60, Length = 144, Thickness = 0.75m,
            QuantityOrdered = 5,
            OrderDate = DateTime.Today,
            EtaDate = DateTime.Today.AddDays(14),
            PoNumber = "PO-002",
            Note = null
        };
        await controller.Create(model);

        Assert.Equal(1, db.Context.Sizes.Count());
        Assert.Equal(60, db.Context.Sizes.First().Width);
    }

    [Fact]
    public async Task Create_WithMissingPoNumber_ReturnsViewWithError()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        db.Context.DimensionValues.AddRange(
            new DimensionValue { Type = "Width", Value = 60 },
            new DimensionValue { Type = "Length", Value = 144 },
            new DimensionValue { Type = "Thickness", Value = 0.75m }
        );
        db.Context.SaveChanges();

        var controller = new OrdersController(db.Context, new SizeService(db.Context), new PricingService(db.Context));
        controller.ModelState.AddModelError("PoNumber", "PO Number is required.");

        var model = new PlaceOrderViewModel
        {
            PatternId = pattern.Id,
            Width = 60, Length = 144, Thickness = 0.75m,
            QuantityOrdered = 5,
            OrderDate = DateTime.Today,
            EtaDate = DateTime.Today.AddDays(14),
            PoNumber = "",
            Note = null
        };
        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, db.Context.Orders.Count());
    }

    private static OrdersController CreateControllerWithTempData(TestDb db)
    {
        var controller = new OrdersController(db.Context, new SizeService(db.Context), new PricingService(db.Context));
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static Order CreateOrder(TestDb db, int qtyOrdered = 10, string poNumber = "PO-100")
    {
        var pattern = db.CreatePattern();
        var size = db.CreateSize();
        var order = new Order
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            QuantityOrdered = qtyOrdered,
            OrderDate = DateTime.Today,
            EtaDate = DateTime.Today.AddDays(30),
            PoNumber = poNumber
        };
        db.Context.Orders.Add(order);
        db.Context.SaveChanges();
        return order;
    }

    [Fact]
    public async Task Edit_ValidUpdate_SavesAndRedirects()
    {
        using var db = TestDb.Create();
        var order = CreateOrder(db, qtyOrdered: 10, poNumber: "PO-100");
        var controller = CreateControllerWithTempData(db);

        var model = new EditOrderViewModel
        {
            Id = order.Id,
            PoNumber = "PO-200",
            QuantityOrdered = 20,
            EtaDate = DateTime.Today.AddDays(60),
            Note = "Updated note"
        };
        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        var saved = db.Context.Orders.First();
        Assert.Equal("PO-200", saved.PoNumber);
        Assert.Equal(20, saved.QuantityOrdered);
    }

    [Fact]
    public async Task Edit_QuantityBelowReceived_ReturnsError()
    {
        using var db = TestDb.Create();
        var order = CreateOrder(db, qtyOrdered: 10);
        // Add 3 receipts
        db.Context.Receipts.Add(new Receipt { OrderId = order.Id, QuantityReceived = 3, DateReceived = DateTime.Today });
        db.Context.SaveChanges();

        var controller = CreateControllerWithTempData(db);
        var model = new EditOrderViewModel
        {
            Id = order.Id,
            PoNumber = "PO-100",
            QuantityOrdered = 1,
            EtaDate = DateTime.Today.AddDays(30)
        };
        var result = await controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains("3 sheets already received", controller.ModelState[nameof(EditOrderViewModel.QuantityOrdered)]!.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Cancel_OrderWithNoReceipts_DeletesOrder()
    {
        using var db = TestDb.Create();
        var order = CreateOrder(db);
        var controller = CreateControllerWithTempData(db);

        var result = await controller.Cancel(order.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(0, db.Context.Orders.Count());
    }

    [Fact]
    public async Task Cancel_OrderWithReceipts_ReturnsError()
    {
        using var db = TestDb.Create();
        var order = CreateOrder(db);
        db.Context.Receipts.Add(new Receipt { OrderId = order.Id, QuantityReceived = 2, DateReceived = DateTime.Today });
        db.Context.SaveChanges();

        var controller = CreateControllerWithTempData(db);
        var result = await controller.Cancel(order.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, db.Context.Orders.Count());
        Assert.Equal("Cannot cancel order PO-100 — it has receipts.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task CloseOut_SetsQuantityToReceived()
    {
        using var db = TestDb.Create();
        var order = CreateOrder(db, qtyOrdered: 3);
        db.Context.Receipts.Add(new Receipt { OrderId = order.Id, QuantityReceived = 2, DateReceived = DateTime.Today });
        db.Context.SaveChanges();

        var controller = CreateControllerWithTempData(db);
        var result = await controller.CloseOut(order.Id);

        Assert.IsType<RedirectToActionResult>(result);
        var updated = db.Context.Orders.First();
        Assert.Equal(2, updated.QuantityOrdered);
    }
}
