using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;

namespace PolilamInventory.Tests.Services;

public class PlannedClaimConversionTests
{
    [Fact]
    public async Task ConvertDueClaims_ConvertsClaimsDueToday()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize();

        db.Context.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 5,
            ScheduledDate = DateTime.Today, SoNumber = "SO1", Note = "Test"
        });
        db.Context.SaveChanges();

        var service = new PlannedClaimConversionService(db.Context);
        var converted = await service.ConvertDueClaims();

        Assert.Equal(1, converted);
        Assert.Empty(db.Context.PlannedClaims.ToList());
        var pull = Assert.Single(db.Context.ActualPulls.ToList());
        Assert.Equal(5, pull.Quantity);
        Assert.Equal("SO1", pull.SoNumber);
        Assert.Equal("Test", pull.Note);
    }

    [Fact]
    public async Task ConvertDueClaims_DoesNotConvertFutureClaims()
    {
        using var db = TestDb.Create();
        var pattern = db.CreatePattern();
        var size = db.CreateSize();

        db.Context.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 5,
            ScheduledDate = DateTime.Today.AddDays(30), SoNumber = "SO1"
        });
        db.Context.SaveChanges();

        var service = new PlannedClaimConversionService(db.Context);
        var converted = await service.ConvertDueClaims();

        Assert.Equal(0, converted);
        Assert.Single(db.Context.PlannedClaims.ToList());
        Assert.Empty(db.Context.ActualPulls.ToList());
    }
}
