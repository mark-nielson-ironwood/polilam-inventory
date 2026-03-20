# Polilam Inventory Tracker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a web application for tracking Polilam sheet inventory — orders, receipts, pulls, and reports — replacing an Excel-based workflow.

**Architecture:** ASP.NET Core 8 MVC app with Razor Pages for server-rendered UI, SQLite via Entity Framework Core for persistence, and Bootstrap 5 dark theme for styling. Inventory is always computed from transaction history, never stored. Planned claims auto-convert to actual pulls when their scheduled date arrives.

**Tech Stack:** .NET 8, ASP.NET Core MVC, Entity Framework Core, SQLite, Bootstrap 5, QuestPDF, xUnit

---

## File Structure

```
PolilamInventory/
├── PolilamInventory.sln
├── src/
│   └── PolilamInventory.Web/
│       ├── Program.cs                          -- App startup, DI, middleware
│       ├── PolilamInventory.Web.csproj          -- Project file with NuGet refs
│       ├── appsettings.json                     -- Connection string, app config
│       ├── Data/
│       │   ├── AppDbContext.cs                  -- EF Core DbContext
│       │   └── Migrations/                     -- EF migrations (auto-generated)
│       ├── Models/
│       │   ├── Pattern.cs                       -- Pattern entity
│       │   ├── Size.cs                          -- Size entity with MaterialType
│       │   ├── Order.cs                         -- Order entity with derived props
│       │   ├── Receipt.cs                       -- Receipt entity
│       │   ├── InventoryAdjustment.cs           -- Adjustment entity
│       │   ├── PlannedClaim.cs                  -- Planned claim entity
│       │   └── ActualPull.cs                    -- Actual pull entity
│       ├── Services/
│       │   ├── InventoryService.cs              -- Inventory calculations, projections
│       │   ├── PlannedClaimConversionService.cs -- Auto-convert planned claims
│       │   ├── SizeService.cs                   -- Find-or-create Size records
│       │   └── ReportExportService.cs           -- CSV and PDF export
│       ├── Middleware/
│       │   └── PlannedClaimConversionMiddleware.cs -- Runs conversion on each request
│       ├── Controllers/
│       │   ├── DashboardController.cs           -- Dashboard page
│       │   ├── OrdersController.cs              -- Place Order page
│       │   ├── ReceiptsController.cs            -- Receive Shipment page
│       │   ├── PullsController.cs               -- Pull Sheets page
│       │   ├── ReportsController.cs             -- All three reports
│       │   ├── SettingsController.cs            -- Patterns & Sizes management
│       │   └── Api/
│       │       └── ContextApiController.cs      -- AJAX endpoints for form context panels
│       ├── ViewModels/
│       │   ├── DashboardViewModel.cs            -- Dashboard cards, alerts, claims
│       │   ├── PlaceOrderViewModel.cs           -- Order form + context data
│       │   ├── ReceiveShipmentViewModel.cs      -- Receipt form + order details
│       │   ├── PullSheetsViewModel.cs           -- Pull form + inventory preview
│       │   ├── InventoryReportViewModel.cs      -- Report data + filters
│       │   ├── RemovalReportViewModel.cs        -- Report data + filters
│       │   ├── TransactionReportViewModel.cs    -- Report data + filters
│       │   └── SettingsViewModel.cs             -- Patterns and sizes lists
│       ├── Views/
│       │   ├── Shared/
│       │   │   ├── _Layout.cshtml               -- Dark sidebar layout
│       │   │   └── _ValidationScriptsPartial.cshtml
│       │   ├── Dashboard/
│       │   │   └── Index.cshtml                 -- Dashboard page
│       │   ├── Orders/
│       │   │   └── Create.cshtml                -- Place Order form
│       │   ├── Receipts/
│       │   │   └── Create.cshtml                -- Receive Shipment form
│       │   ├── Pulls/
│       │   │   └── Create.cshtml                -- Pull Sheets form
│       │   ├── Reports/
│       │   │   ├── Inventory.cshtml             -- Inventory Report
│       │   │   ├── Removal.cshtml               -- Removal Report
│       │   │   └── Transactions.cshtml          -- Transaction Report
│       │   └── Settings/
│       │       └── Index.cshtml                 -- Settings page
│       └── wwwroot/
│           ├── css/
│           │   └── site.css                     -- Dark theme overrides, color system
│           └── js/
│               └── site.js                      -- Form context panel AJAX, toggle logic
└── tests/
    └── PolilamInventory.Tests/
        ├── PolilamInventory.Tests.csproj         -- Test project
        ├── Services/
        │   ├── InventoryServiceTests.cs          -- Inventory calculation tests
        │   ├── PlannedClaimConversionTests.cs    -- Auto-conversion tests
        │   └── SizeServiceTests.cs               -- Find-or-create tests
        ├── Controllers/
        │   ├── OrdersControllerTests.cs          -- Order creation tests
        │   ├── ReceiptsControllerTests.cs        -- Receipt + partial fill tests
        │   ├── PullsControllerTests.cs           -- Pull + planned claim tests
        │   └── ReportsControllerTests.cs         -- Report query tests
        └── Helpers/
            └── TestDbHelper.cs                   -- In-memory SQLite test fixture
```

---

## Task 1: Project Scaffolding and Database

Set up the solution, project, EF Core, and all entity models with an initial migration.

**Files:**
- Create: `PolilamInventory.sln`
- Create: `src/PolilamInventory.Web/PolilamInventory.Web.csproj`
- Create: `src/PolilamInventory.Web/Program.cs`
- Create: `src/PolilamInventory.Web/appsettings.json`
- Create: `src/PolilamInventory.Web/Data/AppDbContext.cs`
- Create: `src/PolilamInventory.Web/Models/Pattern.cs`
- Create: `src/PolilamInventory.Web/Models/Size.cs`
- Create: `src/PolilamInventory.Web/Models/Order.cs`
- Create: `src/PolilamInventory.Web/Models/Receipt.cs`
- Create: `src/PolilamInventory.Web/Models/InventoryAdjustment.cs`
- Create: `src/PolilamInventory.Web/Models/PlannedClaim.cs`
- Create: `src/PolilamInventory.Web/Models/ActualPull.cs`
- Create: `tests/PolilamInventory.Tests/PolilamInventory.Tests.csproj`
- Create: `tests/PolilamInventory.Tests/Helpers/TestDbHelper.cs`

- [ ] **Step 1: Create solution and web project**

```bash
dotnet new sln -n PolilamInventory
mkdir -p src/PolilamInventory.Web
dotnet new mvc -n PolilamInventory.Web -o src/PolilamInventory.Web --no-https
dotnet sln add src/PolilamInventory.Web/PolilamInventory.Web.csproj
```

- [ ] **Step 2: Add NuGet packages**

```bash
cd src/PolilamInventory.Web
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package QuestPDF
```

- [ ] **Step 3: Create all entity model files**

Create `Models/Pattern.cs`:
```csharp
namespace PolilamInventory.Web.Models;

public class Pattern
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ReorderTrigger { get; set; } = 5;
}
```

Create `Models/Size.cs`:
```csharp
namespace PolilamInventory.Web.Models;

public class Size
{
    public int Id { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public decimal Thickness { get; set; }

    public string MaterialType => Thickness == 0.039m ? "Plastic Laminate" : "Compact Laminate";
    public string DisplayName => $"{Width}×{Length}×{Thickness}";
}
```

Create `Models/Order.cs`:
```csharp
namespace PolilamInventory.Web.Models;

public class Order
{
    public int Id { get; set; }
    public int PatternId { get; set; }
    public Pattern Pattern { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;
    public int QuantityOrdered { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime EtaDate { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public string? Note { get; set; }

    public List<Receipt> Receipts { get; set; } = new();

    public int QuantityReceived => Receipts.Sum(r => r.QuantityReceived);
    public int QuantityOutstanding => QuantityOrdered - QuantityReceived;
    public bool IsFilled => QuantityOutstanding <= 0;
}
```

Create `Models/Receipt.cs`:
```csharp
namespace PolilamInventory.Web.Models;

public class Receipt
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int QuantityReceived { get; set; }
    public DateTime DateReceived { get; set; }
    public string? Note { get; set; }
}
```

Create `Models/InventoryAdjustment.cs`:
```csharp
namespace PolilamInventory.Web.Models;

public class InventoryAdjustment
{
    public int Id { get; set; }
    public int PatternId { get; set; }
    public Pattern Pattern { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTime DateAdded { get; set; }
    public string? Note { get; set; }
}
```

Create `Models/PlannedClaim.cs`:
```csharp
namespace PolilamInventory.Web.Models;

public class PlannedClaim
{
    public int Id { get; set; }
    public int PatternId { get; set; }
    public Pattern Pattern { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string SoNumber { get; set; } = string.Empty;
    public string? Note { get; set; }
}
```

Create `Models/ActualPull.cs`:
```csharp
namespace PolilamInventory.Web.Models;

public class ActualPull
{
    public int Id { get; set; }
    public int PatternId { get; set; }
    public Pattern Pattern { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTime PullDate { get; set; }
    public string SoNumber { get; set; } = string.Empty;
    public string? Note { get; set; }
}
```

- [ ] **Step 4: Create AppDbContext**

Create `Data/AppDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Models;

namespace PolilamInventory.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Pattern> Patterns => Set<Pattern>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();
    public DbSet<PlannedClaim> PlannedClaims => Set<PlannedClaim>();
    public DbSet<ActualPull> ActualPulls => Set<ActualPull>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Size>()
            .HasIndex(s => new { s.Width, s.Length, s.Thickness })
            .IsUnique();

        modelBuilder.Entity<Pattern>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Pattern).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Size).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Receipt>()
            .HasOne(r => r.Order).WithMany(o => o.Receipts).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InventoryAdjustment>()
            .HasOne(a => a.Pattern).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryAdjustment>()
            .HasOne(a => a.Size).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PlannedClaim>()
            .HasOne(c => c.Pattern).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PlannedClaim>()
            .HasOne(c => c.Size).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActualPull>()
            .HasOne(p => p.Pattern).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActualPull>()
            .HasOne(p => p.Size).WithMany().OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 5: Configure Program.cs**

Update `Program.cs` to register EF Core with SQLite, configure the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=polilam.db"
  }
}
```

```csharp
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Convert any planned claims that are due on startup
    var conversionService = new PlannedClaimConversionService(db);
    await conversionService.ConvertDueClaims();
}

app.Run();
```

- [ ] **Step 6: Create initial migration**

```bash
cd src/PolilamInventory.Web
dotnet ef migrations add InitialCreate
```

- [ ] **Step 7: Create test project and test helper**

```bash
mkdir -p tests/PolilamInventory.Tests
dotnet new xunit -n PolilamInventory.Tests -o tests/PolilamInventory.Tests
dotnet sln add tests/PolilamInventory.Tests/PolilamInventory.Tests.csproj
dotnet add tests/PolilamInventory.Tests/PolilamInventory.Tests.csproj reference src/PolilamInventory.Web/PolilamInventory.Web.csproj
cd tests/PolilamInventory.Tests
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

Create `tests/PolilamInventory.Tests/Helpers/TestDbHelper.cs`:
```csharp
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
```

- [ ] **Step 8: Verify build and tests pass**

```bash
dotnet build
dotnet test
```

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat: project scaffolding with EF Core models and SQLite"
```

---

## Task 2: Inventory Service and Core Business Logic

Implement the inventory calculation engine and planned claim auto-conversion — the heart of the application.

**Files:**
- Create: `src/PolilamInventory.Web/Services/InventoryService.cs`
- Create: `src/PolilamInventory.Web/Services/PlannedClaimConversionService.cs`
- Create: `src/PolilamInventory.Web/Services/SizeService.cs`
- Create: `src/PolilamInventory.Web/Middleware/PlannedClaimConversionMiddleware.cs`
- Create: `tests/PolilamInventory.Tests/Services/InventoryServiceTests.cs`
- Create: `tests/PolilamInventory.Tests/Services/PlannedClaimConversionTests.cs`
- Create: `tests/PolilamInventory.Tests/Services/SizeServiceTests.cs`

- [ ] **Step 1: Write InventoryService tests**

Create `tests/PolilamInventory.Tests/Services/InventoryServiceTests.cs`:
```csharp
using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;

namespace PolilamInventory.Tests.Services;

public class InventoryServiceTests
{
    [Fact]
    public async Task GetCurrentInventory_WithAdjustmentsReceiptsAndPulls_CalculatesCorrectly()
    {
        using var db = TestDbHelper.CreateContext();
        var pattern = TestDbHelper.CreatePattern(db);
        var size = TestDbHelper.CreateSize(db);
        var service = new InventoryService(db);

        // Add 10 via adjustment
        db.InventoryAdjustments.Add(new InventoryAdjustment
            { PatternId = pattern.Id, SizeId = size.Id, Quantity = 10, DateAdded = DateTime.Today });

        // Receive 4 via order+receipt
        var order = new Order
        {
            PatternId = pattern.Id, SizeId = size.Id, QuantityOrdered = 7,
            OrderDate = DateTime.Today.AddDays(-30), EtaDate = DateTime.Today, PoNumber = "PO1"
        };
        db.Orders.Add(order);
        db.SaveChanges();
        db.Receipts.Add(new Receipt { OrderId = order.Id, QuantityReceived = 4, DateReceived = DateTime.Today });

        // Pull 6
        db.ActualPulls.Add(new ActualPull
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 6,
            PullDate = DateTime.Today, SoNumber = "SO1"
        });
        db.SaveChanges();

        var inventory = await service.GetCurrentInventory(pattern.Id, size.Id);

        Assert.Equal(8, inventory); // 10 + 4 - 6
    }

    [Fact]
    public async Task GetCurrentInventory_WithNegativeAdjustment_SubtractsCorrectly()
    {
        using var db = TestDbHelper.CreateContext();
        var pattern = TestDbHelper.CreatePattern(db);
        var size = TestDbHelper.CreateSize(db);
        var service = new InventoryService(db);

        db.InventoryAdjustments.Add(new InventoryAdjustment
            { PatternId = pattern.Id, SizeId = size.Id, Quantity = 10, DateAdded = DateTime.Today });
        db.InventoryAdjustments.Add(new InventoryAdjustment
            { PatternId = pattern.Id, SizeId = size.Id, Quantity = -3, DateAdded = DateTime.Today, Note = "Damaged" });
        db.SaveChanges();

        var inventory = await service.GetCurrentInventory(pattern.Id, size.Id);

        Assert.Equal(7, inventory);
    }

    [Fact]
    public async Task GetProjectedInventory_CalculatesCommittedBeforeArrival()
    {
        using var db = TestDbHelper.CreateContext();
        var pattern = TestDbHelper.CreatePattern(db);
        var size = TestDbHelper.CreateSize(db);
        var service = new InventoryService(db);

        // 10 in stock
        db.InventoryAdjustments.Add(new InventoryAdjustment
            { PatternId = pattern.Id, SizeId = size.Id, Quantity = 10, DateAdded = DateTime.Today });

        // 9 on order, ETA Jul 1
        var order = new Order
        {
            PatternId = pattern.Id, SizeId = size.Id, QuantityOrdered = 9,
            OrderDate = DateTime.Today, EtaDate = new DateTime(2026, 7, 1), PoNumber = "PO1"
        };
        db.Orders.Add(order);

        // 7 claimed before ETA, 5 claimed after ETA
        db.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 7,
            ScheduledDate = new DateTime(2026, 6, 15), SoNumber = "SO1"
        });
        db.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 5,
            ScheduledDate = new DateTime(2026, 6, 19), SoNumber = "SO2"
        });
        db.SaveChanges();

        var projection = await service.GetProjectedInventory(pattern.Id, size.Id);

        Assert.Equal(12, projection.CommittedBeforeArrival);  // 7 + 5 both before Jul 1
        Assert.Equal(-2, projection.ProjectedAtArrival);       // 10 - 12
        Assert.Equal(12, projection.TotalCommitted);           // 7 + 5
        Assert.Equal(7, projection.ProjectedBalance);          // 10 + 9 - 12
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test
```
Expected: FAIL — `InventoryService` does not exist yet.

- [ ] **Step 3: Implement InventoryService**

Create `src/PolilamInventory.Web/Services/InventoryService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using PolilamInventory.Web.Data;

namespace PolilamInventory.Web.Services;

public class InventoryProjection
{
    public int CurrentInventory { get; set; }
    public int OnOrder { get; set; }
    public DateTime? EarliestEta { get; set; }
    public DateTime? EarliestOrderDate { get; set; }
    public int CommittedBeforeArrival { get; set; }
    public int ProjectedAtArrival { get; set; }
    public int TotalCommitted { get; set; }
    public int ProjectedBalance { get; set; }
}

public class InventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetCurrentInventory(int patternId, int sizeId)
    {
        var adjustments = await _db.InventoryAdjustments
            .Where(a => a.PatternId == patternId && a.SizeId == sizeId)
            .SumAsync(a => a.Quantity);

        var receipts = await _db.Receipts
            .Where(r => r.Order.PatternId == patternId && r.Order.SizeId == sizeId)
            .SumAsync(r => r.QuantityReceived);

        var pulls = await _db.ActualPulls
            .Where(p => p.PatternId == patternId && p.SizeId == sizeId)
            .SumAsync(p => p.Quantity);

        return adjustments + receipts - pulls;
    }

    public async Task<InventoryProjection> GetProjectedInventory(int patternId, int sizeId)
    {
        var currentInventory = await GetCurrentInventory(patternId, sizeId);

        var openOrders = await _db.Orders
            .Include(o => o.Receipts)
            .Where(o => o.PatternId == patternId && o.SizeId == sizeId)
            .ToListAsync();

        var unfilledOrders = openOrders.Where(o => !o.IsFilled).ToList();
        var onOrder = unfilledOrders.Sum(o => o.QuantityOutstanding);
        var earliestEta = unfilledOrders.Any() ? unfilledOrders.Min(o => o.EtaDate) : (DateTime?)null;
        var earliestOrderDate = unfilledOrders.Any() ? unfilledOrders.Min(o => o.OrderDate) : (DateTime?)null;

        var allClaims = await _db.PlannedClaims
            .Where(c => c.PatternId == patternId && c.SizeId == sizeId)
            .ToListAsync();

        var totalCommitted = allClaims.Sum(c => c.Quantity);
        var committedBeforeArrival = earliestEta.HasValue
            ? allClaims.Where(c => c.ScheduledDate < earliestEta.Value).Sum(c => c.Quantity)
            : 0;

        return new InventoryProjection
        {
            CurrentInventory = currentInventory,
            OnOrder = onOrder,
            EarliestEta = earliestEta,
            EarliestOrderDate = earliestOrderDate,
            CommittedBeforeArrival = committedBeforeArrival,
            ProjectedAtArrival = currentInventory - committedBeforeArrival,
            TotalCommitted = totalCommitted,
            ProjectedBalance = currentInventory + onOrder - totalCommitted
        };
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test
```
Expected: All 3 tests PASS.

- [ ] **Step 5: Write PlannedClaimConversion tests**

Create `tests/PolilamInventory.Tests/Services/PlannedClaimConversionTests.cs`:
```csharp
using PolilamInventory.Tests.Helpers;
using PolilamInventory.Web.Models;
using PolilamInventory.Web.Services;

namespace PolilamInventory.Tests.Services;

public class PlannedClaimConversionTests
{
    [Fact]
    public async Task ConvertDueClaims_ConvertsClaimsDueToday()
    {
        using var db = TestDbHelper.CreateContext();
        var pattern = TestDbHelper.CreatePattern(db);
        var size = TestDbHelper.CreateSize(db);

        db.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 5,
            ScheduledDate = DateTime.Today, SoNumber = "SO1", Note = "Test"
        });
        db.SaveChanges();

        var service = new PlannedClaimConversionService(db);
        var converted = await service.ConvertDueClaims();

        Assert.Equal(1, converted);
        Assert.Empty(db.PlannedClaims.ToList());
        var pull = Assert.Single(db.ActualPulls.ToList());
        Assert.Equal(5, pull.Quantity);
        Assert.Equal("SO1", pull.SoNumber);
        Assert.Equal("Test", pull.Note);
    }

    [Fact]
    public async Task ConvertDueClaims_DoesNotConvertFutureClaims()
    {
        using var db = TestDbHelper.CreateContext();
        var pattern = TestDbHelper.CreatePattern(db);
        var size = TestDbHelper.CreateSize(db);

        db.PlannedClaims.Add(new PlannedClaim
        {
            PatternId = pattern.Id, SizeId = size.Id, Quantity = 5,
            ScheduledDate = DateTime.Today.AddDays(30), SoNumber = "SO1"
        });
        db.SaveChanges();

        var service = new PlannedClaimConversionService(db);
        var converted = await service.ConvertDueClaims();

        Assert.Equal(0, converted);
        Assert.Single(db.PlannedClaims.ToList());
        Assert.Empty(db.ActualPulls.ToList());
    }
}
```

- [ ] **Step 6: Run tests to verify they fail**

```bash
dotnet test
```

- [ ] **Step 7: Implement PlannedClaimConversionService**

Create `src/PolilamInventory.Web/Services/PlannedClaimConversionService.cs`:
```csharp
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
```

- [ ] **Step 8: Implement SizeService**

Create `src/PolilamInventory.Web/Services/SizeService.cs`:
```csharp
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
```

- [ ] **Step 9: Create the middleware**

Create `src/PolilamInventory.Web/Middleware/PlannedClaimConversionMiddleware.cs`:
```csharp
using PolilamInventory.Web.Services;

namespace PolilamInventory.Web.Middleware;

public class PlannedClaimConversionMiddleware
{
    private readonly RequestDelegate _next;

    public PlannedClaimConversionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, PlannedClaimConversionService conversionService)
    {
        await conversionService.ConvertDueClaims();
        await _next(context);
    }
}
```

- [ ] **Step 10: Register services in Program.cs**

Add to `Program.cs` before `var app = builder.Build();`:
```csharp
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<PlannedClaimConversionService>();
builder.Services.AddScoped<SizeService>();
```

Add after `app.UseRouting();`:
```csharp
app.UseMiddleware<PlannedClaimConversionMiddleware>();
```

- [ ] **Step 11: Run all tests**

```bash
dotnet test
```
Expected: All 5 tests PASS.

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "feat: inventory service, planned claim conversion, and size service"
```

---

## Task 3: Layout and Dark Theme

Build the shared layout with dark sidebar navigation and Bootstrap 5 dark theme.

**Files:**
- Create: `src/PolilamInventory.Web/wwwroot/css/site.css`
- Modify: `src/PolilamInventory.Web/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Create site.css with dark theme and color system**

Create `wwwroot/css/site.css` with:
- CSS custom properties for the color system (#28a745, #ffc107, #dc3545, #4fc3f7, #0d1520, #1a2332, #2a3a4a)
- Dark sidebar styles (fixed position, dark background, nav links)
- Main content area offset for sidebar width
- Card styles matching mockup (dark background, colored left border)
- Table styles (alternating row backgrounds, colored text classes)
- Alert banner styles
- Form styles (dark inputs, labels)
- Status badge styles (.badge-healthy, .badge-low-stock, .badge-deficit)
- Transaction type badge styles (.badge-initial, .badge-order, .badge-receipt, .badge-pull)
- Utility classes: .text-healthy, .text-warning, .text-deficit, .text-info-blue
- Negative value styling (red, bold)
- Responsive adjustments

- [ ] **Step 2: Build _Layout.cshtml with sidebar navigation**

Replace the default `_Layout.cshtml` with a dark sidebar layout containing:
- Sidebar with app title "Polilam Inventory"
- Nav sections: Dashboard, Transactions (Place Order, Receive Shipment, Pull Sheets), Reports (Inventory, Removal, Transaction), Settings
- Active page highlighting
- Main content area with `@RenderBody()`
- Bootstrap 5 CDN links
- Reference to site.css and site.js

- [ ] **Step 3: Verify the layout renders**

```bash
cd src/PolilamInventory.Web
dotnet run
```

Open browser to `http://localhost:5000` — verify sidebar appears with dark theme. Stop the server.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: dark sidebar layout with Bootstrap 5 theme"
```

---

## Task 4: Settings Page (Patterns & Sizes Management)

Build the Settings page for managing patterns, widths, lengths, thicknesses, and reorder triggers. This is needed before other pages because forms depend on these values.

**Files:**
- Create: `src/PolilamInventory.Web/Controllers/SettingsController.cs`
- Create: `src/PolilamInventory.Web/ViewModels/SettingsViewModel.cs`
- Create: `src/PolilamInventory.Web/Views/Settings/Index.cshtml`

- [ ] **Step 1: Create SettingsViewModel**

```csharp
namespace PolilamInventory.Web.ViewModels;

public class SettingsViewModel
{
    public List<PatternRow> Patterns { get; set; } = new();
    public List<decimal> Widths { get; set; } = new();
    public List<decimal> Lengths { get; set; } = new();
    public List<ThicknessRow> Thicknesses { get; set; } = new();
    public string AppVersion { get; set; } = "1.0.0";
}

public class PatternRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ReorderTrigger { get; set; }
    public bool HasTransactions { get; set; }
}

public class ThicknessRow
{
    public decimal Value { get; set; }
    public string MaterialType { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create SettingsController**

Implement with these actions:
- `Index` — loads all patterns, distinct widths/lengths/thicknesses from Sizes table
- `AddPattern(string name, int reorderTrigger)` — creates new pattern
- `EditPattern(int id, string name, int reorderTrigger)` — updates pattern
- `DeletePattern(int id)` — deletes if no transactions reference it
- `AddWidth(decimal value)`, `AddLength(decimal value)`, `AddThickness(decimal value)` — adds dimension values by creating Size records for all existing combinations
- `DeleteWidth(decimal value)`, etc. — removes if no transactions reference it

Note: Read Steps 2 and 3 together before starting. We need a DimensionValue entity (Step 3) to track available dimension values independently of the Sizes table. The actual Size record (W+L+T combination) is created on-the-fly when used in a form (via SizeService.FindOrCreate). The Settings page manages DimensionValues, not Sizes directly.

- [ ] **Step 3: Add DimensionValue model**

Create `Models/DimensionValue.cs`:
```csharp
namespace PolilamInventory.Web.Models;

public class DimensionValue
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // "Width", "Length", "Thickness"
    public decimal Value { get; set; }
}
```

Add `DbSet<DimensionValue> DimensionValues` to `AppDbContext`. Add unique index on (Type, Value). Create migration.

- [ ] **Step 4: Create Settings/Index.cshtml**

Build the view with three sections:
- Patterns table with inline add/edit/delete
- Dimension values in three columns (Widths, Lengths, Thicknesses) with add/delete
- App version display at bottom
- Material type label next to each thickness value

- [ ] **Step 5: Seed default data**

Add seed data in `Program.cs` after migration — the 6 initial patterns with reorder trigger of 5, and the dimension values (widths: 30, 60; lengths: 72, 144; thicknesses: 0.039, 0.25, 0.5, 0.75, 1).

- [ ] **Step 6: Verify Settings page works**

```bash
dotnet run
```
Open browser, navigate to Settings. Verify patterns display, add/edit/delete work, dimension values display.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: settings page for patterns and dimension management"
```

---

## Task 5: Place Order Page

**Files:**
- Create: `src/PolilamInventory.Web/Controllers/OrdersController.cs`
- Create: `src/PolilamInventory.Web/ViewModels/PlaceOrderViewModel.cs`
- Create: `src/PolilamInventory.Web/Views/Orders/Create.cshtml`
- Create: `src/PolilamInventory.Web/Controllers/Api/ContextApiController.cs`
- Create: `tests/PolilamInventory.Tests/Controllers/OrdersControllerTests.cs`

- [ ] **Step 1: Write order creation tests**

Test that:
- Creating an order with valid data succeeds and persists
- SizeService.FindOrCreate is called to get/create the Size record
- Validation rejects missing required fields (pattern, quantity, PO number, dates)

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Create PlaceOrderViewModel**

Include: list of patterns, list of widths/lengths/thicknesses for dropdowns, form input fields.

- [ ] **Step 4: Create OrdersController**

- `Create (GET)` — loads dropdown data
- `Create (POST)` — validates, calls SizeService.FindOrCreate, saves Order, redirects to Dashboard

- [ ] **Step 5: Create ContextApiController**

JSON API endpoint: `GET /api/context/pattern/{patternId}` returns:
- Current inventory per size (sorted by ascending thickness)
- Open orders per size with PO, ordered, received, outstanding, ETA

This powers the right-side context panel via AJAX when pattern dropdown changes.

- [ ] **Step 6: Create Orders/Create.cshtml**

Two-column layout per mockup:
- Left: form with pattern dropdown, W/L/T dropdowns, quantity, PO, dates, note, green submit button
- Right: context panel (empty until pattern selected, populated via AJAX from ContextApiController)

- [ ] **Step 7: Add JavaScript for context panel**

In `wwwroot/js/site.js`, add handler for pattern dropdown change that fetches `/api/context/pattern/{id}` and populates the context tables.

- [ ] **Step 8: Run tests**

```bash
dotnet test
```

- [ ] **Step 9: Manual verification**

```bash
dotnet run
```
Navigate to Place Order, select a pattern, verify context panel updates, submit an order, verify it persists.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "feat: place order page with context panel"
```

---

## Task 6: Receive Shipment Page

**Files:**
- Create: `src/PolilamInventory.Web/Controllers/ReceiptsController.cs`
- Create: `src/PolilamInventory.Web/ViewModels/ReceiveShipmentViewModel.cs`
- Create: `src/PolilamInventory.Web/Views/Receipts/Create.cshtml`
- Create: `tests/PolilamInventory.Tests/Controllers/ReceiptsControllerTests.cs`

- [ ] **Step 1: Write receipt tests**

Test that:
- Recording a receipt against an open order succeeds
- Receipt quantity cannot exceed outstanding balance
- Multiple receipts against same order accumulate correctly
- Order is marked as filled when fully received

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Create ReceiveShipmentViewModel**

Include: list of open orders for dropdown, selected order details, receipt form fields, receipt history.

- [ ] **Step 4: Create ReceiptsController**

- `Create (GET)` — loads open orders
- `GetOrderDetails(int orderId)` — AJAX endpoint returning order detail JSON for auto-population
- `Create (POST)` — validates quantity ≤ outstanding, saves Receipt, redirects

- [ ] **Step 5: Create Receipts/Create.cshtml**

Per mockup: open order dropdown, auto-populated details panel, quantity/date/note fields, preview message, receipt history table.

- [ ] **Step 6: Add JavaScript for order selection**

When open order dropdown changes, fetch order details and populate the detail panel and receipt history.

- [ ] **Step 7: Run tests and manual verification**

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: receive shipment page with partial fill tracking"
```

---

## Task 7: Pull Sheets Page

**Files:**
- Create: `src/PolilamInventory.Web/Controllers/PullsController.cs`
- Create: `src/PolilamInventory.Web/ViewModels/PullSheetsViewModel.cs`
- Create: `src/PolilamInventory.Web/Views/Pulls/Create.cshtml`
- Create: `tests/PolilamInventory.Tests/Controllers/PullsControllerTests.cs`

- [ ] **Step 1: Write pull tests**

Test that:
- "Pull Now" creates ActualPull and is blocked if quantity > current inventory
- "Schedule Future Pull" creates PlannedClaim and is allowed even if quantity > current inventory
- Deficit warning is generated when planned claim would push projected balance negative

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Create PullSheetsViewModel**

Include: pull mode (PullNow/ScheduleFuture), pattern/size dropdowns, quantity, SO number, date, inventory impact preview data, deficit warning message.

- [ ] **Step 4: Create PullsController**

- `Create (GET)` — loads dropdown data
- `Create (POST)` — branches on mode: PullNow creates ActualPull (validates quantity ≤ inventory), ScheduleFuture creates PlannedClaim (generates deficit warning if applicable)
- `GetInventoryImpact(int patternId, decimal width, decimal length, decimal thickness, int quantity)` — AJAX endpoint returning current inventory and after-pull projection

- [ ] **Step 5: Create Pulls/Create.cshtml**

Per mockup: mode toggle (Pull Now / Schedule Future Pull), pattern/W/L/T dropdowns, quantity, SO number, date, note, inventory impact preview, deficit warning area, colored submit button.

- [ ] **Step 6: Add JavaScript for mode toggle and impact preview**

Toggle changes button color/label and date label. Pattern/size/quantity changes trigger AJAX call to update inventory impact preview.

- [ ] **Step 7: Run tests and manual verification**

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: pull sheets page with pull now and schedule future modes"
```

---

## Task 8: Dashboard

**Files:**
- Create: `src/PolilamInventory.Web/Controllers/DashboardController.cs`
- Create: `src/PolilamInventory.Web/ViewModels/DashboardViewModel.cs`
- Create: `src/PolilamInventory.Web/Views/Dashboard/Index.cshtml`

- [ ] **Step 1: Create DashboardViewModel**

```csharp
namespace PolilamInventory.Web.ViewModels;

public class DashboardViewModel
{
    public List<AlertItem> Alerts { get; set; } = new();
    public List<PatternCard> PatternCards { get; set; } = new();
    public List<UpcomingClaim> UpcomingClaims { get; set; } = new();
}

public class AlertItem
{
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // "danger" or "warning"
}

public class PatternCard
{
    public string PatternName { get; set; } = string.Empty;
    public int TotalSheets { get; set; }
    public string Status { get; set; } = string.Empty; // "healthy", "low-stock", "deficit"
    public List<SizeBreakdown> Sizes { get; set; } = new();
}

public class SizeBreakdown
{
    public string SizeDisplay { get; set; } = string.Empty;
    public int InStock { get; set; }
    public int OnOrder { get; set; }
    public string StockStatus { get; set; } = string.Empty;
}

public class UpcomingClaim
{
    public string PatternName { get; set; } = string.Empty;
    public string SizeDisplay { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string SoNumber { get; set; } = string.Empty;
    public bool IsDeficit { get; set; }
}
```

- [ ] **Step 2: Create DashboardController**

`Index` action:
1. Load all patterns
2. For each pattern, find all distinct sizes that have any transactions (also include patterns with zero transactions — they should show with 0 stock)
3. For each pattern+size, call InventoryService.GetProjectedInventory
4. Build PatternCards with per-size breakdown, sorted by ascending thickness
5. Determine card status (worst across all sizes)
6. Build Alerts from deficit/reorder situations
7. Load all PlannedClaims for UpcomingClaims table
8. Return DashboardViewModel

- [ ] **Step 3: Create Dashboard/Index.cshtml**

Per mockup:
- Alert banner at top (red for deficits, amber for reorder warnings)
- 3-column grid of pattern cards with status badges, total count, per-size tables
- Upcoming planned claims table at bottom
- Color coding matching the approved design

- [ ] **Step 4: Manual verification**

Seed some test data (use the example data from the spreadsheet), run the app, verify dashboard matches the mockup.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: dashboard with pattern cards, alerts, and upcoming claims"
```

---

## Task 9: Reports — Inventory, Removal, Transaction

**Files:**
- Create: `src/PolilamInventory.Web/Controllers/ReportsController.cs`
- Create: `src/PolilamInventory.Web/ViewModels/InventoryReportViewModel.cs`
- Create: `src/PolilamInventory.Web/ViewModels/RemovalReportViewModel.cs`
- Create: `src/PolilamInventory.Web/ViewModels/TransactionReportViewModel.cs`
- Create: `src/PolilamInventory.Web/Views/Reports/Inventory.cshtml`
- Create: `src/PolilamInventory.Web/Views/Reports/Removal.cshtml`
- Create: `src/PolilamInventory.Web/Views/Reports/Transactions.cshtml`
- Create: `tests/PolilamInventory.Tests/Controllers/ReportsControllerTests.cs`

- [ ] **Step 1: Write report query tests**

Test:
- Inventory report calculates all columns correctly for a known data set
- Inventory report pattern filter works
- Removal report counts pulls only within date range
- Removal report "Include Inactive" toggle works (off excludes zeroes, on includes them)
- Transaction report combines all transaction types in correct chronological order
- Transaction report pattern and date range filters work

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Create report ViewModels**

InventoryReportViewModel: filter values, list of InventoryReportRow (pattern, WxLxT, in stock, last adj date, on order, order date, ETA, committed before arrival, projected at arrival, total committed, projected balance, reorder flag).

RemovalReportViewModel: start date, end date, include inactive toggle, list of RemovalReportRow (pattern, WxLxT, sheets removed, last removal date).

TransactionReportViewModel: pattern filter, date range, list of TransactionReportRow (date, type, pattern, WxLxT, quantity with sign, PO/SO, note).

- [ ] **Step 4: Create ReportsController**

Three actions:
- `Inventory(string? patternFilter)` — queries all pattern+size combinations, calls InventoryService for each, builds report rows sorted by pattern name then ascending thickness
- `Removal(DateTime? startDate, DateTime? endDate, bool includeInactive = false)` — queries ActualPulls in date range, groups by pattern+size, optionally includes zero-removal entries
- `Transactions(string? patternFilter, DateTime? startDate, DateTime? endDate)` — unions all transaction types into a chronological list, applies filters

- [ ] **Step 5: Create report views**

Each view per mockup:
- Filter bar at top with dropdowns/date pickers
- CSV and PDF export buttons
- Data table with color-coded values
- Inventory report: red for negative projected values, amber for reorder warnings
- Removal report: red for removal counts, gray for zeroes
- Transaction report: colored type badges, signed quantities

- [ ] **Step 6: Run tests and manual verification**

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: inventory, removal, and transaction reports"
```

---

## Task 10: CSV and PDF Export

**Files:**
- Create: `src/PolilamInventory.Web/Services/ReportExportService.cs`
- Modify: `src/PolilamInventory.Web/Controllers/ReportsController.cs` (add export actions)

- [ ] **Step 1: Implement CSV export**

Create `ReportExportService` with methods:
- `GenerateInventoryCsv(List<InventoryReportRow> data)` → byte[]
- `GenerateRemovalCsv(List<RemovalReportRow> data)` → byte[]
- `GenerateTransactionCsv(List<TransactionReportRow> data)` → byte[]

Each generates a proper CSV file with headers matching the report columns.

- [ ] **Step 2: Implement PDF export**

Using QuestPDF, add methods:
- `GenerateInventoryPdf(...)` → byte[]
- `GenerateRemovalPdf(...)` → byte[]
- `GenerateTransactionPdf(...)` → byte[]

Each generates a formatted PDF with the company name, report title, date range, and data table.

Note: QuestPDF requires `QuestPDF.Settings.License = LicenseType.Community;` in Program.cs.

- [ ] **Step 3: Add export actions to ReportsController**

Add actions for each report type:
- `InventoryCsv(string? patternFilter)`, `InventoryPdf(string? patternFilter)`
- `RemovalCsv(...)`, `RemovalPdf(...)`
- `TransactionCsv(...)`, `TransactionPdf(...)`

Each returns `File(bytes, contentType, filename)`.

- [ ] **Step 4: Wire up export buttons in views**

Update the CSV and PDF buttons in each report view to link to the export actions with current filter values.

- [ ] **Step 5: Manual verification**

Test CSV and PDF downloads for each report. Verify CSV opens in Excel correctly. Verify PDF renders readable tables.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: CSV and PDF export for all reports"
```

---

## Task 11: Polish and Integration Testing

Final pass for visual polish, edge cases, and end-to-end testing.

**Files:**
- Modify: various CSS and views for polish
- Modify: `src/PolilamInventory.Web/Program.cs` (seed data for demo)

- [ ] **Step 1: Add seed data matching the Excel example**

Create a data seeder that populates the database with the example data from the spreadsheet:
- 6 patterns with initial stock (10 each of various sizes)
- Sample orders (PO Aaa through Ddd)
- Sample receipts (partial fill for PO Aaa)
- Sample pulls and planned claims
This provides a realistic demo environment.

- [ ] **Step 2: End-to-end walkthrough**

Run the application and verify each workflow:
1. Dashboard shows correct cards, alerts, and claims
2. Place an order → appears on dashboard and inventory report
3. Receive partial shipment → order shows partial fill, inventory updates
4. Receive remaining → order marked as filled
5. Pull sheets (now) → inventory decreases, appears in removal report
6. Schedule future pull → appears as planned claim, impacts projections
7. All three reports render correctly with filters and exports
8. Settings: add/edit/delete patterns and dimension values

- [ ] **Step 3: Visual polish pass**

- Verify all color coding matches spec (green/amber/red counts, badges, borders)
- Verify ascending thickness sort everywhere
- Verify "—" for empty/zero values
- Verify negative values in red bold
- Verify responsive behavior on smaller screens
- Verify sidebar active state highlighting

- [ ] **Step 4: Edge case review**

- Pattern with no transactions: should show on dashboard with 0 stock
- All orders filled: "No open orders" text, no order columns on inventory report
- No planned claims: upcoming claims table shows "No upcoming claims" message
- Removal report with no pulls in range: respects Include Inactive toggle
- Very long pattern names: don't break card layout

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: seed data, polish, and integration verification"
```

---

## Task 12: Deployment Preparation

**Files:**
- Modify: `src/PolilamInventory.Web/Program.cs`
- Create: `README.md` (only if not existing — brief deployment instructions)

- [ ] **Step 1: Configure for production**

- Set `ASPNETCORE_ENVIRONMENT=Production` handling
- Ensure SQLite database path is configurable via appsettings
- Add error handling page
- Remove seed data from production startup (make it conditional on Development environment)

- [ ] **Step 2: Publish**

```bash
dotnet publish src/PolilamInventory.Web -c Release -o ./publish
```

- [ ] **Step 3: Test published build**

```bash
cd publish
dotnet PolilamInventory.Web.dll
```

Verify application runs and is accessible.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: production configuration and publish setup"
```
