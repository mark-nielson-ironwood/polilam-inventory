using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Tests.Helpers;

public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext Context { get; }

    private TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public static TestDb Create() => new();

    public Pattern CreatePattern(string name = "Espresso", int reorderTrigger = 5)
    {
        var pattern = new Pattern { Name = name, ReorderTrigger = reorderTrigger };
        Context.Patterns.Add(pattern);
        Context.SaveChanges();
        return pattern;
    }

    public Size CreateSize(decimal width = 60, decimal length = 144, decimal thickness = 0.75m)
    {
        var size = new Size { Width = width, Length = length, Thickness = thickness };
        Context.Sizes.Add(size);
        Context.SaveChanges();
        return size;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
