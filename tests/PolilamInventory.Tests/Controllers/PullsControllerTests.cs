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

public class PullsControllerTests
{
    private static PullsController CreateController(TestDb db, ITempDataDictionary? tempData = null)
    {
        var inventoryService = new InventoryService(db.Context);
        var sizeService = new SizeService(db.Context);
        var pricingService = new PricingService(db.Context);
        var controller = new PullsController(db.Context, inventoryService, sizeService, pricingService);

        var td = tempData ?? new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.TempData = td;

        return controller;
    }

    [Fact]
    public async Task PullNow_WithSufficientInventory_CreatesActualPull()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);

        // Set inventory to 5 via adjustment
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 5,
            DateAdded = DateTime.Today
        });
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var model = new PullSheetsViewModel
        {
            PatternId = pattern.Id,
            Width = 60,
            Length = 144,
            Thickness = 0.75m,
            Quantity = 3,
            SoNumber = "SO-001",
            PullDate = DateTime.Today,
            Mode = "PullNow"
        };

        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal(1, db.Context.ActualPulls.Count());
        var pull = db.Context.ActualPulls.First();
        Assert.Equal(pattern.Id, pull.PatternId);
        Assert.Equal(3, pull.Quantity);
        Assert.Equal("SO-001", pull.SoNumber);
    }

    [Fact]
    public async Task PullNow_WithInsufficientInventory_ReturnsViewWithError()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);

        // Set inventory to 2 via adjustment
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 2,
            DateAdded = DateTime.Today
        });
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var model = new PullSheetsViewModel
        {
            PatternId = pattern.Id,
            Width = 60,
            Length = 144,
            Thickness = 0.75m,
            Quantity = 5,
            SoNumber = "SO-002",
            PullDate = DateTime.Today,
            Mode = "PullNow"
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, db.Context.ActualPulls.Count());
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task ScheduleFuture_CreatesPlannedClaim()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);

        // Set inventory to 10 so no deficit
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 10,
            DateAdded = DateTime.Today
        });
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var model = new PullSheetsViewModel
        {
            PatternId = pattern.Id,
            Width = 60,
            Length = 144,
            Thickness = 0.75m,
            Quantity = 3,
            SoNumber = "SO-003",
            PullDate = DateTime.Today.AddDays(7),
            Mode = "ScheduleFuture"
        };

        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal(1, db.Context.PlannedClaims.Count());
        var claim = db.Context.PlannedClaims.First();
        Assert.Equal(pattern.Id, claim.PatternId);
        Assert.Equal(3, claim.Quantity);
        Assert.Equal("SO-003", claim.SoNumber);
    }

    [Fact]
    public async Task ScheduleFuture_WithDeficit_RedirectsWithWarning()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern(name: "Espresso");
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);

        // Inventory of 2, scheduling a pull of 5 → projected balance goes negative
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 2,
            DateAdded = DateTime.Today
        });
        db.Context.SaveChanges();

        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        var controller = CreateController(db, tempData);
        var model = new PullSheetsViewModel
        {
            PatternId = pattern.Id,
            Width = 60,
            Length = 144,
            Thickness = 0.75m,
            Quantity = 5,
            SoNumber = "SO-004",
            PullDate = DateTime.Today.AddDays(7),
            Mode = "ScheduleFuture"
        };

        var result = await controller.Create(model);

        // PlannedClaim is still saved
        Assert.Equal(1, db.Context.PlannedClaims.Count());

        // Redirects to Dashboard
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);

        // Warning is set
        Assert.True(tempData.ContainsKey("Warning"));
        Assert.Contains("deficit", tempData["Warning"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Edit_ValidUpdate_SavesAndRedirects()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern(name: "Espresso");
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);

        var claim = new PlannedClaim
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 3,
            ScheduledDate = DateTime.Today.AddDays(7),
            SoNumber = "SO-100",
            Note = "original note"
        };
        db.Context.PlannedClaims.Add(claim);
        db.Context.SaveChanges();

        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        var controller = CreateController(db, tempData);

        var model = new EditPlannedPullViewModel
        {
            Id = claim.Id,
            PatternName = pattern.Name,
            SizeDisplay = size.DisplayName,
            Quantity = 10,
            ScheduledDate = DateTime.Today.AddDays(14),
            SoNumber = "SO-200",
            Note = "updated note"
        };

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var updated = db.Context.PlannedClaims.First(c => c.Id == claim.Id);
        Assert.Equal(10, updated.Quantity);
        Assert.Equal("SO-200", updated.SoNumber);
        Assert.Equal("updated note", updated.Note);
        Assert.True(tempData.ContainsKey("Success"));
    }

    [Fact]
    public async Task Edit_InvalidModel_ReturnsView()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);

        var claim = new PlannedClaim
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 3,
            ScheduledDate = DateTime.Today.AddDays(7),
            SoNumber = "SO-100"
        };
        db.Context.PlannedClaims.Add(claim);
        db.Context.SaveChanges();

        var controller = CreateController(db);
        controller.ModelState.AddModelError("SoNumber", "Required");

        var model = new EditPlannedPullViewModel
        {
            Id = claim.Id,
            PatternName = pattern.Name,
            SizeDisplay = size.DisplayName,
            Quantity = 5,
            ScheduledDate = DateTime.Today.AddDays(14),
            SoNumber = ""
        };

        var result = await controller.Edit(model);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Cancel_DeletesPlannedClaim()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);

        var claim = new PlannedClaim
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 3,
            ScheduledDate = DateTime.Today.AddDays(7),
            SoNumber = "SO-100"
        };
        db.Context.PlannedClaims.Add(claim);
        db.Context.SaveChanges();

        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        var controller = CreateController(db, tempData);

        var result = await controller.Cancel(claim.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(0, db.Context.PlannedClaims.Count());
        Assert.True(tempData.ContainsKey("Success"));
    }
}
