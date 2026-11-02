using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TradingOrders.Application;
using TradingOrders.Domain;

namespace TradingOrders.Infrastructure;

public sealed class EfOrderRepository(TradingDbContext db) : IOrderRepository
{
    public Task<Order?> FindByClientOrderIdAsync(string clientOrderId, CancellationToken ct) =>
        db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.ClientOrderId == clientOrderId, ct);

    public Task<Order?> FindByIdAsync(Guid id, CancellationToken ct) =>
        db.Orders.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Order>> ListAsync(int limit, CancellationToken ct) =>
        await db.Orders.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(ct);

    public void Add(Order order) => db.Orders.Add(order);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqliteException sqlite &&
            sqlite.SqliteExtendedErrorCode == 2067 &&
            sqlite.Message.Contains("Orders.ClientOrderId", StringComparison.OrdinalIgnoreCase))
        {
            throw new DuplicateClientOrderIdException();
        }
    }
}
