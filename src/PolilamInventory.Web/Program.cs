using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Middleware;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<PlannedClaimConversionService>();
builder.Services.AddScoped<SizeService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<PlannedClaimConversionMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var conversionService = new PlannedClaimConversionService(db);
    await conversionService.ConvertDueClaims();

    // Seed default data if empty
    if (!db.Patterns.Any())
    {
        var defaultPatterns = new[]
        {
            "Espresso", "Alpine White", "Phantom Grey", "Natural Rift Oak", "Calcatta Marble", "Absolute Black"
        };
        foreach (var name in defaultPatterns)
            db.Patterns.Add(new Pattern { Name = name, ReorderTrigger = 5 });
        db.SaveChanges();
    }

    if (!db.DimensionValues.Any())
    {
        var defaults = new[]
        {
            new DimensionValue { Type = "Width", Value = 30 },
            new DimensionValue { Type = "Width", Value = 60 },
            new DimensionValue { Type = "Length", Value = 72 },
            new DimensionValue { Type = "Length", Value = 144 },
            new DimensionValue { Type = "Thickness", Value = 0.039m },
            new DimensionValue { Type = "Thickness", Value = 0.25m },
            new DimensionValue { Type = "Thickness", Value = 0.5m },
            new DimensionValue { Type = "Thickness", Value = 0.75m },
            new DimensionValue { Type = "Thickness", Value = 1m },
        };
        db.DimensionValues.AddRange(defaults);
        db.SaveChanges();
    }
}

app.Run();
