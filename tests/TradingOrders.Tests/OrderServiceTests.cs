using TradingOrders.Application;
using TradingOrders.Domain;
using Xunit;

namespace TradingOrders.Tests;

public sealed class OrderServiceTests
{
    private readonly FakeRepository _repo = new();
    private readonly OrderService _service;

    public OrderServiceTests() => _service = new OrderService(_repo);

    private static CreateOrderCommand Request(decimal quantity = 2.5m) =>
        new("order-001", "msft", OrderSide.Buy, quantity, 350.50m);

    [Fact]
    public async Task Same_key_and_payload_replays_without_duplicate_insert()
    {
        var initial = await _service.CreateAsync(Request(), CancellationToken.None);
        var retry = await _service.CreateAsync(Request(), CancellationToken.None);

        Assert.False(initial.Replayed);
        Assert.True(retry.Replayed);
        Assert.Equal(initial.Order.Id, retry.Order.Id);
        Assert.Single(_repo.Orders);
    }

    [Fact]
    public async Task Same_key_and_different_payload_conflicts()
    {
        var ct = CancellationToken.None;
        await _service.CreateAsync(Request(), ct);
        await Assert.ThrowsAsync<OrderConflictException>(() =>
            _service.CreateAsync(Request(quantity: 3.5m), ct));
    }

    [Fact]
    public async Task Cancellation_is_idempotent()
    {
        var ct = CancellationToken.None;
        var created = await _service.CreateAsync(Request(), ct);
        var first = await _service.CancelAsync(created.Order.Id, ct);
        var second = await _service.CancelAsync(created.Order.Id, ct);

        Assert.Equal("Cancelled", second.Status);
        Assert.Equal(first.CancelledAtUtc, second.CancelledAtUtc);
    }

    [Fact]
    public async Task Invalid_list_limit_returns_domain_validation_error()
    {
        await Assert.ThrowsAsync<OrderValidationException>(() =>
            _service.ListAsync(0, CancellationToken.None));
    }

    private sealed class FakeRepository : IOrderRepository
    {
        public List<Order> Orders { get; } = [];
        public void Add(Order order) => Orders.Add(order);
        public Task<Order?> FindByClientOrderIdAsync(string id, CancellationToken ct) =>
            Task.FromResult(Orders.SingleOrDefault(x => x.ClientOrderId == id));
        public Task<Order?> FindByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Orders.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Order>> ListAsync(int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Order>>(Orders.Take(limit).ToArray());
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
