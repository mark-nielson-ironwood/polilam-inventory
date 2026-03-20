using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Web.Services;

public class SizeService
{
    private readonly AppDbContext _db;

    public SizeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Size> FindOrCreate(decimal width, decimal length, decimal thickness)
    {
        var existing = await _db.Sizes
            .FirstOrDefaultAsync(s => s.Width == width && s.Length == length && s.Thickness == thickness);

        if (existing != null) return existing;

        var size = new Size { Width = width, Length = length, Thickness = thickness };
        _db.Sizes.Add(size);
        await _db.SaveChangesAsync();
        return size;
    }
}
