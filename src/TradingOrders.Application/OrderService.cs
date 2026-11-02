using TradingOrders.Domain;

namespace TradingOrders.Application;

public sealed record CreateOrderCommand(
    string ClientOrderId, string Symbol, OrderSide Side, decimal Quantity, decimal LimitPrice);

public sealed record OrderDto(
    Guid Id, string ClientOrderId, string Symbol, string Side,
    decimal Quantity, decimal LimitPrice, string Status,
    DateTime CreatedAtUtc, DateTime? CancelledAtUtc)
{
    public static OrderDto From(Order order) => new(
        order.Id, order.ClientOrderId, order.Symbol, order.Side.ToString(),
        order.Quantity, order.LimitPrice, order.Status.ToString(),
        order.CreatedAtUtc, order.CancelledAtUtc);
}

public sealed record CreateOrderResult(OrderDto Order, bool Replayed);

public interface IOrderRepository
{
    Task<Order?> FindByClientOrderIdAsync(string clientOrderId, CancellationToken ct);
    Task<Order?> FindByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Order>> ListAsync(int limit, CancellationToken ct);
    void Add(Order order);
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class OrderConflictException(string message) : Exception(message);
public sealed class OrderNotFoundException(Guid id) : Exception($"Order '{id}' was not found.");
/// <summary>Thrown when a database unique-key race occurs on clientOrderId.</summary>
public sealed class DuplicateClientOrderIdException() : Exception("ClientOrderId already exists.");

public sealed class OrderService(IOrderRepository repository)
{
    public async Task<CreateOrderResult> CreateAsync(CreateOrderCommand command, CancellationToken ct)
    {
        // Validate before checking idempotency, including the request's economic intent.
        var candidate = Order.Create(
            command.ClientOrderId, command.Symbol, command.Side,
            command.Quantity, command.LimitPrice, DateTime.UtcNow);

        var existing = await repository.FindByClientOrderIdAsync(candidate.ClientOrderId, ct);
        if (existing is not null) return ReplayOrConflict(existing, candidate);

        repository.Add(candidate);
        try
        {
            await repository.SaveChangesAsync(ct);
            return new CreateOrderResult(OrderDto.From(candidate), Replayed: false);
        }
        catch (DuplicateClientOrderIdException)
        {
            // Another request may have committed the same key in parallel. The
            // unique database index is authoritative; only identical intents replay.
            existing = await repository.FindByClientOrderIdAsync(candidate.ClientOrderId, ct);
            if (existing is null) throw;
            return ReplayOrConflict(existing, candidate);
        }
    }

    public async Task<OrderDto> GetAsync(Guid id, CancellationToken ct)
    {
        var order = await repository.FindByIdAsync(id, ct) ?? throw new OrderNotFoundException(id);
        return OrderDto.From(order);
    }

    public async Task<IReadOnlyList<OrderDto>> ListAsync(int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 100)
            throw new OrderValidationException("Limit must be between 1 and 100.");
        return (await repository.ListAsync(limit, ct)).Select(OrderDto.From).ToArray();
    }

    public async Task<OrderDto> CancelAsync(Guid id, CancellationToken ct)
    {
        var order = await repository.FindByIdAsync(id, ct) ?? throw new OrderNotFoundException(id);
        if (order.Cancel(DateTime.UtcNow)) await repository.SaveChangesAsync(ct);
        return OrderDto.From(order);
    }

    private static CreateOrderResult ReplayOrConflict(Order existing, Order candidate)
    {
        if (!existing.MatchesIntent(candidate))
            throw new OrderConflictException(
                "ClientOrderId already exists with a different order payload.");
        return new CreateOrderResult(OrderDto.From(existing), Replayed: true);
    }
}
