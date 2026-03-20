using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Web.Services;

public class PlannedClaimConversionService
{
    private readonly AppDbContext _db;

    public PlannedClaimConversionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> ConvertDueClaims()
    {
        var dueClaims = await _db.PlannedClaims
            .Where(c => c.ScheduledDate <= DateTime.Today)
            .ToListAsync();

        foreach (var claim in dueClaims)
        {
            _db.ActualPulls.Add(new ActualPull
            {
                PatternId = claim.PatternId,
                SizeId = claim.SizeId,
                Quantity = claim.Quantity,
                PullDate = claim.ScheduledDate,
                SoNumber = claim.SoNumber,
                Note = claim.Note
            });
            _db.PlannedClaims.Remove(claim);
        }

        await _db.SaveChangesAsync();
        return dueClaims.Count;
    }
}
