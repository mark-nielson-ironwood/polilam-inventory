using Microsoft.AspNetCore.Mvc;
using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Controllers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Tests.Controllers;

public class ReportsControllerTests
{
    private static ReportsController CreateController(TestDb db)
        => new ReportsController(db.Context, new InventoryService(db.Context), new ReportExportService());

    [Fact]
    public async Task Inventory_CalculatesColumnsCorrectly()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern("Espresso", reorderTrigger: 5);
        var size = db.CreateSize();

        // adjustment(10)
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 10,
            DateAdded = DateTime.Today.AddDays(-10)
        });

        // open order qty=6
        var order = new Order
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            QuantityOrdered = 6,
            OrderDate = DateTime.Today.AddDays(-5),
            EtaDate = DateTime.Today.AddDays(10),
            PoNumber = "PO-001"
        };
        db.Context.Orders.Add(order);
        db.Context.SaveChanges();

        // receipt(4) against the order
        db.Context.Receipts.Add(new Receipt
        {
            OrderId = order.Id,
            QuantityReceived = 4,
            DateReceived = DateTime.Today.AddDays(-3)
        });

        // pull(2)
        db.Context.ActualPulls.Add(new ActualPull
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 2,
            PullDate = DateTime.Today.AddDays(-2),
            SoNumber = "SO-001"
        });

        // planned claim(qty=3)
        db.Context.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 3,
            ScheduledDate = DateTime.Today.AddDays(20),
            SoNumber = "SO-002"
        });

        db.Context.SaveChanges();

        var controller = CreateController(db);
        var result = await controller.Inventory(null);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<InventoryReportViewModel>(view.Model);

        Assert.Single(vm.Rows);
        var row = vm.Rows[0];

        // InStock = adjustments(10) + receipts(4) - pulls(2) = 12
        Assert.Equal(12, row.InStock);

        // OnOrder = QuantityOutstanding = QuantityOrdered(6) - QuantityReceived(4) = 2
        // Wait, order has qty=6 ordered and 4 received => outstanding=2
        Assert.Equal(2, row.OnOrder);

        // TotalCommitted = 3
        Assert.Equal(3, row.TotalCommitted);

        // ProjectedBalance = inStock(12) + onOrder(2) - totalCommitted(3) = 11
        Assert.Equal(11, row.ProjectedBalance);

        // NeedsReorder: 11 > 5 => false
        Assert.False(row.NeedsReorder);
    }

    [Fact]
    public async Task Inventory_PatternFilter_ExcludesOtherPatterns()
    {
        using var db = TestDb.Create();
        var patternA = db.CreatePattern("Espresso");
        var patternB = db.CreatePattern("Walnut");
        var size = db.CreateSize();

        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = patternA.Id,
            SizeId = size.Id,
            Quantity = 5,
            DateAdded = DateTime.Today
        });
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = patternB.Id,
            SizeId = size.Id,
            Quantity = 3,
            DateAdded = DateTime.Today
        });
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var result = await controller.Inventory("Espresso");

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<InventoryReportViewModel>(view.Model);

        Assert.All(vm.Rows, r => Assert.Equal("Espresso", r.PatternName));
        Assert.DoesNotContain(vm.Rows, r => r.PatternName == "Walnut");
    }

    [Fact]
    public async Task Removal_CountsPullsInDateRange()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize();

        var start = new DateTime(2026, 3, 1);
        var end = new DateTime(2026, 3, 31);

        // 2 pulls in range
        db.Context.ActualPulls.Add(new ActualPull
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 3,
            PullDate = new DateTime(2026, 3, 10),
            SoNumber = "SO-A"
        });
        db.Context.ActualPulls.Add(new ActualPull
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 5,
            PullDate = new DateTime(2026, 3, 20),
            SoNumber = "SO-B"
        });
        // 1 pull outside range
        db.Context.ActualPulls.Add(new ActualPull
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 10,
            PullDate = new DateTime(2026, 4, 5),
            SoNumber = "SO-C"
        });
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var result = await controller.Removal(start, end);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<RemovalReportViewModel>(view.Model);

        Assert.Single(vm.Rows);
        Assert.Equal(8, vm.Rows[0].SheetsRemoved); // 3 + 5
    }

    [Fact]
    public async Task Removal_IncludeInactive_IncludesZeroRows()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize();

        // No pulls — but have an adjustment so the pattern+size exists
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 10,
            DateAdded = DateTime.Today
        });
        db.Context.SaveChanges();

        var start = new DateTime(2026, 3, 1);
        var end = new DateTime(2026, 3, 31);

        var controller = CreateController(db);

        // Without includeInactive: not included
        var resultExclude = await controller.Removal(start, end, includeInactive: false);
        var vmExclude = Assert.IsType<RemovalReportViewModel>(Assert.IsType<ViewResult>(resultExclude).Model);
        Assert.Empty(vmExclude.Rows);

        // With includeInactive: included with SheetsRemoved=0
        var resultInclude = await controller.Removal(start, end, includeInactive: true);
        var vmInclude = Assert.IsType<RemovalReportViewModel>(Assert.IsType<ViewResult>(resultInclude).Model);
        Assert.Single(vmInclude.Rows);
        Assert.Equal(0, vmInclude.Rows[0].SheetsRemoved);
    }

    [Fact]
    public async Task Transactions_CombinesAllTypes()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize();

        var today = DateTime.Today;

        // Order
        var order = new Order
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            QuantityOrdered = 10,
            OrderDate = today,
            EtaDate = today.AddDays(14),
            PoNumber = "PO-T01"
        };
        db.Context.Orders.Add(order);
        db.Context.SaveChanges();

        // Receipt
        db.Context.Receipts.Add(new Receipt
        {
            OrderId = order.Id,
            QuantityReceived = 4,
            DateReceived = today
        });

        // Pull
        db.Context.ActualPulls.Add(new ActualPull
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 2,
            PullDate = today,
            SoNumber = "SO-T01"
        });

        // Adjustment
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 1,
            DateAdded = today
        });

        // Planned Claim
        db.Context.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id,
            SizeId = size.Id,
            Quantity = 3,
            ScheduledDate = today,
            SoNumber = "SO-T02"
        });

        db.Context.SaveChanges();

        var controller = CreateController(db);
        var result = await controller.Transactions(null, today.AddDays(-1), today.AddDays(1));

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<TransactionReportViewModel>(view.Model);

        Assert.Contains(vm.Rows, r => r.Type == "Order");
        Assert.Contains(vm.Rows, r => r.Type == "Receipt");
        Assert.Contains(vm.Rows, r => r.Type == "Pull");
        Assert.Contains(vm.Rows, r => r.Type == "Adjustment");
        Assert.Contains(vm.Rows, r => r.Type == "Planned");
    }

    [Fact]
    public async Task Transactions_PatternFilter_Works()
    {
        using var db = TestDb.Create();
        var patternA = db.CreatePattern("Espresso");
        var patternB = db.CreatePattern("Walnut");
        var size = db.CreateSize();

        var today = DateTime.Today;

        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = patternA.Id,
            SizeId = size.Id,
            Quantity = 5,
            DateAdded = today
        });
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
        {
            PatternId = patternB.Id,
            SizeId = size.Id,
            Quantity = 3,
            DateAdded = today
        });
        db.Context.SaveChanges();

        var controller = CreateController(db);
        var result = await controller.Transactions("Espresso", today.AddDays(-1), today.AddDays(1));

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<TransactionReportViewModel>(view.Model);

        Assert.All(vm.Rows, r => Assert.Equal("Espresso", r.PatternName));
        Assert.DoesNotContain(vm.Rows, r => r.PatternName == "Walnut");
    }
}
