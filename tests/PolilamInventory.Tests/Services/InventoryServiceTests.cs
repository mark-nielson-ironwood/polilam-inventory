using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;

namespace PolilamInventory.Tests.Services;

public class InventoryServiceTests
{
    [Fact]
    public async Task GetCurrentInventory_WithAdjustmentsReceiptsAndPulls_CalculatesCorrectly()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize();
        var service = new InventoryService(db.Context);

        // Add 10 via adjustment
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
            { PatternId = pattern.Id, SizeId = size.Id, Quantity = 10, DateAdded = DateTime.Today });

        // Receive 4 via order+receipt
        var order = new Order
        {
            PatternId = pattern.Id, SizeId = size.Id, QuantityOrdered = 7,
            OrderDate = DateTime.Today.AddDays(-30), EtaDate = DateTime.Today, PoNumber = "PO1"
        };
        db.Context.Orders.Add(order);
        db.Context.SaveChanges();
        db.Context.Receipts.Add(new Receipt { OrderId = order.Id, QuantityReceived = 4, DateReceived = DateTime.Today });

        // Pull 6
        db.Context.ActualPulls.Add(new ActualPull
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 6,
            PullDate = DateTime.Today, SoNumber = "SO1"
        });
        db.Context.SaveChanges();

        var inventory = await service.GetCurrentInventory(pattern.Id, size.Id);

        Assert.Equal(8, inventory); // 10 + 4 - 6
    }

    [Fact]
    public async Task GetCurrentInventory_WithNegativeAdjustment_SubtractsCorrectly()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize();
        var service = new InventoryService(db.Context);

        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
            { PatternId = pattern.Id, SizeId = size.Id, Quantity = 10, DateAdded = DateTime.Today });
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
            { PatternId = pattern.Id, SizeId = size.Id, Quantity = -3, DateAdded = DateTime.Today, Note = "Damaged" });
        db.Context.SaveChanges();

        var inventory = await service.GetCurrentInventory(pattern.Id, size.Id);

        Assert.Equal(7, inventory);
    }

    [Fact]
    public async Task GetProjectedInventory_CalculatesCommittedBeforeArrival()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize();
        var service = new InventoryService(db.Context);

        // 10 in stock
        db.Context.InventoryAdjustments.Add(new InventoryAdjustment
            { PatternId = pattern.Id, SizeId = size.Id, Quantity = 10, DateAdded = DateTime.Today });

        // 9 on order, ETA Jul 1
        var order = new Order
        {
            PatternId = pattern.Id, SizeId = size.Id, QuantityOrdered = 9,
            OrderDate = DateTime.Today, EtaDate = new DateTime(2026, 7, 1), PoNumber = "PO1"
        };
        db.Context.Orders.Add(order);

        // 7 claimed before ETA, 5 claimed after ETA
        db.Context.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 7,
            ScheduledDate = new DateTime(2026, 6, 15), SoNumber = "SO1"
        });
        db.Context.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 5,
            ScheduledDate = new DateTime(2026, 6, 19), SoNumber = "SO2"
        });
        db.Context.SaveChanges();

        var projection = await service.GetProjectedInventory(pattern.Id, size.Id);

        Assert.Equal(12, projection.CommittedBeforeArrival);  // 7 + 5 both before Jul 1
        Assert.Equal(-2, projection.ProjectedAtArrival);       // 10 - 12
        Assert.Equal(12, projection.TotalCommitted);           // 7 + 5
        Assert.Equal(7, projection.ProjectedBalance);          // 10 + 9 - 12
    }
}
