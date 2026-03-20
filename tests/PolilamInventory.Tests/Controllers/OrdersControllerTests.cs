using Microsoft.AspNetCore.Mvc;
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

        var controller = new OrdersController(db.Context, new SizeService(db.Context));
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

        var controller = new OrdersController(db.Context, new SizeService(db.Context));
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

        var controller = new OrdersController(db.Context, new SizeService(db.Context));
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
}
