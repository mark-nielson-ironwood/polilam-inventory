using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;
using PolilamInventory.Web.Middleware;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<PlannedClaimConversionService>();
builder.Services.AddScoped<SizeService>();
builder.Services.AddScoped<ReportExportService>();
builder.Services.AddScoped<PricingService>();

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
        var defaultPatterns = new (string Name, string Category)[]
        {
            ("Black 454", "Solid"), ("English Oak", "Woodgrain"), ("Espresso", "Woodgrain"),
            ("Nebraska", "Woodgrain"), ("Pigeon 461", "Solid"), ("White 457", "Solid")
        };
        foreach (var (name, category) in defaultPatterns)
            db.Patterns.Add(new Pattern { Name = name, ReorderTrigger = 5, Category = category });
        db.SaveChanges();
    }

    // Update existing pattern categories if they are still default "Solid"
    var categoryMap = new Dictionary<string, string>
    {
        ["English Oak"] = "Woodgrain",
        ["Espresso"] = "Woodgrain",
        ["Nebraska"] = "Woodgrain",
        ["Black 454"] = "Solid",
        ["Pigeon 461"] = "Solid",
        ["White 457"] = "Solid"
    };
    var patternsToUpdate = db.Patterns.Where(p => categoryMap.Keys.Contains(p.Name)).ToList();
    foreach (var p in patternsToUpdate)
    {
        if (categoryMap.TryGetValue(p.Name, out var cat))
            p.Category = cat;
    }
    db.SaveChanges();

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

    // Seed sheet pricing (idempotent)
    if (!db.SheetPricings.Any())
    {
        db.SheetPricings.AddRange(
            // Compact Laminate - Solid
            new SheetPricing { Category = "Solid", Thickness = 0.25m, Tier1Price = 5.10m, Tier2Price = 4.80m, Tier3Price = 4.73m },
            new SheetPricing { Category = "Solid", Thickness = 0.50m, Tier1Price = 8.40m, Tier2Price = 7.77m, Tier3Price = 7.70m },
            new SheetPricing { Category = "Solid", Thickness = 0.75m, Tier1Price = 11.50m, Tier2Price = 10.60m, Tier3Price = 10.50m },
            new SheetPricing { Category = "Solid", Thickness = 1.0m,  Tier1Price = 16.60m, Tier2Price = 15.35m, Tier3Price = 15.20m },
            // Compact Laminate - Woodgrain
            new SheetPricing { Category = "Woodgrain", Thickness = 0.25m, Tier1Price = 5.25m, Tier2Price = 4.90m, Tier3Price = 4.73m },
            new SheetPricing { Category = "Woodgrain", Thickness = 0.50m, Tier1Price = 8.55m, Tier2Price = 7.87m, Tier3Price = 7.70m },
            new SheetPricing { Category = "Woodgrain", Thickness = 0.75m, Tier1Price = 11.75m, Tier2Price = 10.75m, Tier3Price = 10.50m },
            new SheetPricing { Category = "Woodgrain", Thickness = 1.0m,  Tier1Price = 16.90m, Tier2Price = 15.50m, Tier3Price = 15.20m },
            // HPL / Plastic Laminate
            new SheetPricing { Category = "Solid",     Thickness = 0.039m, Tier1Price = 2.00m, Tier2Price = 1.75m, Tier3Price = 1.50m },
            new SheetPricing { Category = "Woodgrain", Thickness = 0.039m, Tier1Price = 2.10m, Tier2Price = 1.85m, Tier3Price = 1.60m }
        );
        db.SaveChanges();
    }

}

app.Run();
