using Microsoft.AspNetCore.Mvc;
using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Controllers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Tests.Services;

public class PricingServiceTests
{
    [Fact]
    public async Task WAC_PerpetualCalculation()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern(name: "Espresso", reorderTrigger: 5);
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);
        var service = new PricingService(db.Context);

        // Day 1: Receipt of 10 sheets at $500/sheet
        var order1 = new Order
        {
            PatternId = pattern.Id, SizeId = size.Id, QuantityOrdered = 10,
            OrderDate = DateTime.Today.AddDays(-30), EtaDate = DateTime.Today.AddDays(-15),
            PoNumber = "PO1", CostPerSheet = 500m
        };
        db.Context.Orders.Add(order1);
        db.Context.SaveChanges();
        db.Context.Receipts.Add(new Receipt { OrderId = order1.Id, QuantityReceived = 10, DateReceived = DateTime.Today.AddDays(-15), CreatedAt = DateTime.UtcNow.AddHours(-10) });

        // Day 2: Receipt of 20 sheets at $400/sheet
        var order2 = new Order
        {
            PatternId = pattern.Id, SizeId = size.Id, QuantityOrdered = 20,
            OrderDate = DateTime.Today.AddDays(-20), EtaDate = DateTime.Today.AddDays(-5),
            PoNumber = "PO2", CostPerSheet = 400m
        };
        db.Context.Orders.Add(order2);
        db.Context.SaveChanges();
        db.Context.Receipts.Add(new Receipt { OrderId = order2.Id, QuantityReceived = 20, DateReceived = DateTime.Today.AddDays(-5), CreatedAt = DateTime.UtcNow.AddHours(-5) });

        // Day 3: Adjustment of 5 sheets at $450/sheet
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 5,
            DateAdded = DateTime.Today, CostPerSheet = 450m, IsDrop = false,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });

        db.Context.SaveChanges();

        var (wac, hasHistory) = await service.GetWeightedAverageCost(pattern.Id, size.Id);

        Assert.True(hasHistory);
        // Perpetual WAC:
        // After receipt 1: onHand=10, WAC = 500
        // After receipt 2: WAC = (10*500 + 20*400) / 30 = 13000/30 = 433.33...
        // After adjustment: WAC = (30*433.33 + 5*450) / 35 = (13000+2250) / 35 = 435.714...
        var expected = Math.Round(15250m / 35m, 2);
        Assert.Equal(expected, wac);
    }

    [Fact]
    public async Task WAC_ResetsWhenStockHitsZero()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern(name: "Espresso", reorderTrigger: 5);
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);
        var service = new PricingService(db.Context);

        // Day 1: Receive 10 sheets at $500/sheet
        var order1 = new Order
        {
            PatternId = pattern.Id, SizeId = size.Id, QuantityOrdered = 10,
            OrderDate = DateTime.Today.AddDays(-30), EtaDate = DateTime.Today.AddDays(-20),
            PoNumber = "PO1", CostPerSheet = 500m
        };
        db.Context.Orders.Add(order1);
        db.Context.SaveChanges();
        db.Context.Receipts.Add(new Receipt { OrderId = order1.Id, QuantityReceived = 10, DateReceived = DateTime.Today.AddDays(-20), CreatedAt = DateTime.UtcNow.AddHours(-10) });

        // Day 2: Pull all 10 sheets (stock goes to zero)
        db.Context.ActualPulls.Add(new ActualPull
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 10,
            PullDate = DateTime.Today.AddDays(-10), SoNumber = "SO1", IsDrop = false,
            CreatedAt = DateTime.UtcNow.AddHours(-5)
        });

        // Day 3: Receive 5 sheets at $900/sheet (new price)
        var order2 = new Order
        {
            PatternId = pattern.Id, SizeId = size.Id, QuantityOrdered = 5,
            OrderDate = DateTime.Today.AddDays(-8), EtaDate = DateTime.Today.AddDays(-3),
            PoNumber = "PO2", CostPerSheet = 900m
        };
        db.Context.Orders.Add(order2);
        db.Context.SaveChanges();
        db.Context.Receipts.Add(new Receipt { OrderId = order2.Id, QuantityReceived = 5, DateReceived = DateTime.Today.AddDays(-3), CreatedAt = DateTime.UtcNow.AddHours(-1) });

        db.Context.SaveChanges();

        var (wac, hasHistory) = await service.GetWeightedAverageCost(pattern.Id, size.Id);

        Assert.True(hasHistory);
        // Stock was zeroed out, then new stock at $900 — WAC should be $900, not blended with old $500
        Assert.Equal(900m, wac);
    }

    [Fact]
    public async Task WAC_ExcludesDropAdjustments()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern(name: "Espresso", reorderTrigger: 5);
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);
        var service = new PricingService(db.Context);

        // Non-drop adjustment: 10 sheets at $500
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 10,
            DateAdded = DateTime.Today, CostPerSheet = 500m, IsDrop = false,
            CreatedAt = DateTime.UtcNow.AddHours(-10)
        });

        // Drop adjustment: 5 sheets at $300 (should be excluded)
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 5,
            DateAdded = DateTime.Today, CostPerSheet = 300m, IsDrop = true,
            CreatedAt = DateTime.UtcNow.AddHours(-5)
        });

        db.Context.SaveChanges();

        var (wac, hasHistory) = await service.GetWeightedAverageCost(pattern.Id, size.Id);

        Assert.True(hasHistory);
        // Only the non-drop adjustment counts: WAC = (10 * 500) / 10 = $500
        Assert.Equal(500m, wac);
    }

    [Fact]
    public async Task Order_AutoCalculatesCostPerSheet()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern(name: "Espresso", reorderTrigger: 5);
        pattern.Category = "Woodgrain";
        db.Context.SaveChanges();

        // Add pricing: Woodgrain, 0.75" thickness
        db.Context.SheetPricings.Add(new SheetPricing
        {
            Category = "Woodgrain", Thickness = 0.75m,
            Tier1Price = 11.75m, Tier2Price = 10.75m, Tier3Price = 10.50m
        });
        db.Context.DimensionValues.AddRange(
            new DimensionValue { Type = "Width", Value = 60 },
            new DimensionValue { Type = "Length", Value = 144 },
            new DimensionValue { Type = "Thickness", Value = 0.75m }
        );
        db.Context.SaveChanges();

        var service = new PricingService(db.Context);

        // Tier 1 (qty < 50)
        var cost = await service.CalculateCostPerSheet(pattern.Id, 60, 144, 0.75m, 25);
        Assert.NotNull(cost);
        // sqft = 60 * 144 / 144 = 60, cost = 60 * 11.75 = 705.00
        Assert.Equal(705.00m, cost.Value);

        // Tier 2 (qty 50-99)
        var cost2 = await service.CalculateCostPerSheet(pattern.Id, 60, 144, 0.75m, 75);
        Assert.NotNull(cost2);
        // cost = 60 * 10.75 = 645.00
        Assert.Equal(645.00m, cost2.Value);

        // Tier 3 (qty 100+)
        var cost3 = await service.CalculateCostPerSheet(pattern.Id, 60, 144, 0.75m, 100);
        Assert.NotNull(cost3);
        // cost = 60 * 10.50 = 630.00
        Assert.Equal(630.00m, cost3.Value);
    }

    [Fact]
    public async Task InventoryReport_UsesWACForStockValue()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern("Espresso", reorderTrigger: 5);
        var size = db.CreateSize(width: 60, length: 144, thickness: 0.75m);

        // Adjustment: 10 sheets at $600/sheet
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 10,
            DateAdded = DateTime.Today, IsDrop = false, CostPerSheet = 600m,
            CreatedAt = DateTime.UtcNow.AddHours(-10)
        });
        db.Context.SaveChanges();

        var controller = new ReportsController(
            db.Context, new InventoryService(db.Context),
            new ReportExportService(), new PricingService(db.Context));

        var result = await controller.Inventory(null);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<InventoryReportViewModel>(view.Model);
        Assert.Single(vm.Rows);
        var row = vm.Rows[0];

        // WAC = $600
        Assert.Equal(600m, row.SheetValue);
        // StockValue = 10 * 600 = $6000
        Assert.Equal(6000m, row.StockValue);
    }
}
