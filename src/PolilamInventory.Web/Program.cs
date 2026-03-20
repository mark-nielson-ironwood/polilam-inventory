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

    // Development-only rich seed data (idempotent)
    if (app.Environment.IsDevelopment())
    {
        var sizeService = scope.ServiceProvider.GetRequiredService<SizeService>();

        // Seed patterns from Excel example (idempotent)
        var seedPatternNames = new[]
        {
            ("Espresso", 3), ("Arctic", 3), ("Slate", 3),
            ("Linen", 3), ("Onyx", 3), ("Ivory", 3)
        };
        foreach (var (name, reorderTrigger) in seedPatternNames)
        {
            if (!db.Patterns.Any(p => p.Name == name))
            {
                db.Patterns.Add(new Pattern { Name = name, ReorderTrigger = reorderTrigger });
            }
        }
        db.SaveChanges();

        // Seed dimension values (idempotent)
        var seedWidths = new[] { 48m, 60m };
        var seedLengths = new[] { 96m, 120m, 144m };
        var seedThicknesses = new[] { 0.039m, 0.5m, 0.75m, 1.0m, 1.25m, 1.5m };

        foreach (var w in seedWidths)
            if (!db.DimensionValues.Any(d => d.Type == "Width" && d.Value == w))
                db.DimensionValues.Add(new DimensionValue { Type = "Width", Value = w });
        foreach (var l in seedLengths)
            if (!db.DimensionValues.Any(d => d.Type == "Length" && d.Value == l))
                db.DimensionValues.Add(new DimensionValue { Type = "Length", Value = l });
        foreach (var t in seedThicknesses)
            if (!db.DimensionValues.Any(d => d.Type == "Thickness" && d.Value == t))
                db.DimensionValues.Add(new DimensionValue { Type = "Thickness", Value = t });
        db.SaveChanges();

        // Seed sample transactions only if no inventory adjustments exist
        if (!db.InventoryAdjustments.Any())
        {
            var today = DateTime.Today;

            var espresso = db.Patterns.First(p => p.Name == "Espresso");
            var arctic   = db.Patterns.First(p => p.Name == "Arctic");
            var slate    = db.Patterns.First(p => p.Name == "Slate");

            var size60x144x075 = await sizeService.FindOrCreate(60m, 144m, 0.75m);

            // Inventory adjustments: 10 sheets each
            db.InventoryAdjustments.AddRange(
                new InventoryAdjustment { PatternId = espresso.Id, SizeId = size60x144x075.Id, Quantity = 10, DateAdded = today },
                new InventoryAdjustment { PatternId = arctic.Id,   SizeId = size60x144x075.Id, Quantity = 10, DateAdded = today },
                new InventoryAdjustment { PatternId = slate.Id,    SizeId = size60x144x075.Id, Quantity = 10, DateAdded = today }
            );
            db.SaveChanges();

            // Open order: PO-001, Espresso 60×144×0.75, qty 5
            var order = new Order
            {
                PatternId = espresso.Id,
                SizeId = size60x144x075.Id,
                QuantityOrdered = 5,
                OrderDate = today.AddDays(-30),
                EtaDate = today.AddDays(7),
                PoNumber = "PO-001"
            };
            db.Orders.Add(order);
            db.SaveChanges();

            // Partial receipt: 2 sheets against PO-001
            db.Receipts.Add(new Receipt
            {
                OrderId = order.Id,
                QuantityReceived = 2,
                DateReceived = today.AddDays(-5)
            });
            db.SaveChanges();

            // Actual pull: 2 sheets, Espresso 60×144×0.75, SO-001
            db.ActualPulls.Add(new ActualPull
            {
                PatternId = espresso.Id,
                SizeId = size60x144x075.Id,
                Quantity = 2,
                PullDate = today.AddDays(-15),
                SoNumber = "SO-001"
            });
            db.SaveChanges();

            // Planned claim: 3 sheets, Arctic 60×144×0.75, SO-002
            db.PlannedClaims.Add(new PlannedClaim
            {
                PatternId = arctic.Id,
                SizeId = size60x144x075.Id,
                Quantity = 3,
                ScheduledDate = today.AddDays(14),
                SoNumber = "SO-002"
            });
            db.SaveChanges();
        }
    }
}

app.Run();
