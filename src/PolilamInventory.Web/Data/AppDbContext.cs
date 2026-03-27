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
    public DbSet<DimensionValue> DimensionValues => Set<DimensionValue>();
    public DbSet<SheetPricing> SheetPricings => Set<SheetPricing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DimensionValue>()
            .HasIndex(d => new { d.Type, d.Value })
            .IsUnique();

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
