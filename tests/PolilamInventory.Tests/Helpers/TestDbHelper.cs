using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Tests.Helpers;

public static class TestDbHelper
{
    public static AppDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static Pattern CreatePattern(AppDbContext db, string name = "Espresso", int reorderTrigger = 5)
    {
        var pattern = new Pattern { Name = name, ReorderTrigger = reorderTrigger };
        db.Patterns.Add(pattern);
        db.SaveChanges();
        return pattern;
    }

    public static Size CreateSize(AppDbContext db, decimal width = 60, decimal length = 144, decimal thickness = 0.75m)
    {
        var size = new Size { Width = width, Length = length, Thickness = thickness };
        db.Sizes.Add(size);
        db.SaveChanges();
        return size;
    }
}
