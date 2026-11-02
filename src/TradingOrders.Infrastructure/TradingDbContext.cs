using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TradingOrders.Domain;

namespace TradingOrders.Infrastructure;

public sealed class TradingDbContext(DbContextOptions<TradingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Order>();
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.ClientOrderId).IsUnique();
        entity.Property(x => x.ClientOrderId).HasMaxLength(64).IsRequired();
        entity.Property(x => x.Symbol).HasMaxLength(16).IsRequired();
        entity.Property(x => x.Side).HasConversion<string>().HasMaxLength(8);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        // SQLite has no native fixed-precision decimal type. Store decimals as
        // invariant-culture TEXT to preserve their exact base-10 value. This
        // demo never filters or sorts by quantity/price in SQL.
        entity.Property(x => x.Quantity).HasConversion(
            value => value.ToString(CultureInfo.InvariantCulture),
            value => decimal.Parse(value, CultureInfo.InvariantCulture));
        entity.Property(x => x.LimitPrice).HasConversion(
            value => value.ToString(CultureInfo.InvariantCulture),
            value => decimal.Parse(value, CultureInfo.InvariantCulture));
    }
}
