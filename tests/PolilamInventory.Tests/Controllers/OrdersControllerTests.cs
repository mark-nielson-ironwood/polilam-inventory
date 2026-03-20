using Microsoft.AspNetCore.Mvc;
using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Controllers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;

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
        var result = await controller.Create(
            patternId: pattern.Id,
            width: 60, length: 144, thickness: 0.75m,
            quantityOrdered: 10,
            orderDate: DateTime.Today,
            etaDate: DateTime.Today.AddDays(30),
            poNumber: "PO-001",
            note: null
        );

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
        await controller.Create(
            patternId: pattern.Id,
            width: 60, length: 144, thickness: 0.75m,
            quantityOrdered: 5,
            orderDate: DateTime.Today,
            etaDate: DateTime.Today.AddDays(14),
            poNumber: "PO-002",
            note: null
        );

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
        controller.ModelState.AddModelError("poNumber", "PO Number is required.");

        var result = await controller.Create(
            patternId: pattern.Id,
            width: 60, length: 144, thickness: 0.75m,
            quantityOrdered: 5,
            orderDate: DateTime.Today,
            etaDate: DateTime.Today.AddDays(14),
            poNumber: "",
            note: null
        );

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, db.Context.Orders.Count());
    }
}
