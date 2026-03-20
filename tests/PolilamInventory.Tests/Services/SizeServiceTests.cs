using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Services;

namespace PolilamInventory.Tests.Services;

public class SizeServiceTests
{
    [Fact]
    public async Task FindOrCreate_CreatesNewSizeWhenNotExists()
    {
        using var db = TestDb.Create();
        var service = new SizeService(db.Context);

        var size = await service.FindOrCreate(60, 144, 0.75m);

        Assert.NotNull(size);
        Assert.Equal(60, size.Width);
        Assert.Equal(144, size.Length);
        Assert.Equal(0.75m, size.Thickness);
        Assert.Equal(1, db.Context.Sizes.Count());
    }

    [Fact]
    public async Task FindOrCreate_ReturnsExistingSize()
    {
        using var db = TestDb.Create();
        var service = new SizeService(db.Context);

        var first = await service.FindOrCreate(60, 144, 0.75m);
        var second = await service.FindOrCreate(60, 144, 0.75m);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, db.Context.Sizes.Count());
    }
}
