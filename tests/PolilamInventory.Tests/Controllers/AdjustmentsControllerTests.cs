using Microsoft.AspNetCore.Mvc;
using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Controllers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Tests.Controllers;

public class AdjustmentsControllerTests
{
    private static AdjustmentsController CreateController(TestDb db)
    {
        var sizeService = new SizeService(db.Context);
        var inventoryService = new InventoryService(db.Context);
        return new AdjustmentsController(db.Context, sizeService, inventoryService);
    }

    [Fact]
    public async Task Create_WithPositiveQuantity_SavesAdjustmentAndRedirects()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        db.Context.DimensionValues.AddRange(
            new DimensionValue { Type = "Width", Value = 60 },
            new DimensionValue { Type = "Length", Value = 144 },
            new DimensionValue { Type = "Thickness", Value = 0.75m }
        );
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var model = new AdjustInventoryViewModel
        {
            PatternId = pattern.Id,
            Width = 60,
            Length = 144,
            Thickness = 0.75m,
            Quantity = 5,
            DateAdded = DateTime.Today,
            Note = null
        };

        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal(1, db.Context.InventoryAdjustments.Count());
        var adj = db.Context.InventoryAdjustments.First();
        Assert.Equal(pattern.Id, adj.PatternId);
        Assert.Equal(5, adj.Quantity);
    }

    [Fact]
    public async Task Create_WithNegativeQuantity_SavesAdjustmentAndRedirects()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        db.Context.DimensionValues.AddRange(
            new DimensionValue { Type = "Width", Value = 60 },
            new DimensionValue { Type = "Length", Value = 144 },
            new DimensionValue { Type = "Thickness", Value = 0.75m }
        );
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var model = new AdjustInventoryViewModel
        {
            PatternId = pattern.Id,
            Width = 60,
            Length = 144,
            Thickness = 0.75m,
            Quantity = -3,
            DateAdded = DateTime.Today,
            Note = "Correction"
        };

        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal(1, db.Context.InventoryAdjustments.Count());
        var adj = db.Context.InventoryAdjustments.First();
        Assert.Equal(-3, adj.Quantity);
    }

    [Fact]
    public async Task Create_WithZeroQuantity_ReturnsViewWithError()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        db.Context.DimensionValues.AddRange(
            new DimensionValue { Type = "Width", Value = 60 },
            new DimensionValue { Type = "Length", Value = 144 },
            new DimensionValue { Type = "Thickness", Value = 0.75m }
        );
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var model = new AdjustInventoryViewModel
        {
            PatternId = pattern.Id,
            Width = 60,
            Length = 144,
            Thickness = 0.75m,
            Quantity = 0,
            DateAdded = DateTime.Today,
            Note = null
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, db.Context.InventoryAdjustments.Count());
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_WithInvalidModel_ReturnsView()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        db.Context.DimensionValues.AddRange(
            new DimensionValue { Type = "Width", Value = 60 },
            new DimensionValue { Type = "Length", Value = 144 },
            new DimensionValue { Type = "Thickness", Value = 0.75m }
        );
        db.Context.SaveChanges();

        var controller = CreateController(db);
        controller.ModelState.AddModelError("PatternId", "Pattern is required.");

        var model = new AdjustInventoryViewModel
        {
            PatternId = 0,
            Width = 60,
            Length = 144,
            Thickness = 0.75m,
            Quantity = 5,
            DateAdded = DateTime.Today,
            Note = null
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, db.Context.InventoryAdjustments.Count());
    }

    [Fact]
    public async Task AdjustInventory_WithIsDrop_SavesFlag()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        db.Context.DimensionValues.AddRange(
            new DimensionValue { Type = "Width", Value = 60 },
            new DimensionValue { Type = "Length", Value = 144 },
            new DimensionValue { Type = "Thickness", Value = 0.75m }
        );
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var model = new AdjustInventoryViewModel
        {
            PatternId = pattern.Id,
            Width = 60,
            Length = 144,
            Thickness = 0.75m,
            Quantity = 3,
            DateAdded = DateTime.Today,
            Note = "Drop piece",
            IsDrop = true
        };

        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(1, db.Context.InventoryAdjustments.Count());
        var adj = db.Context.InventoryAdjustments.First();
        Assert.True(adj.IsDrop);
        Assert.Equal(3, adj.Quantity);
    }
}
