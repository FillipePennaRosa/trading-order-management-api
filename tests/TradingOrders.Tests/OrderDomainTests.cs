using TradingOrders.Domain;
using Xunit;

namespace TradingOrders.Tests;

public sealed class OrderDomainTests
{
    [Fact]
    public void Create_normalizes_symbol_and_sets_pending()
    {
        var order = Order.Create(" key-123 ", " msft ", OrderSide.Buy, 2.5m, 350.50m, DateTime.UtcNow);
        Assert.Equal("key-123", order.ClientOrderId);
        Assert.Equal("MSFT", order.Symbol);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Create_rejects_nonpositive_values()
    {
        var now = DateTime.UtcNow;
        Assert.Throws<OrderValidationException>(() =>
            Order.Create("key-123", "MSFT", OrderSide.Buy, 0m, 10m, now));
        Assert.Throws<OrderValidationException>(() =>
            Order.Create("key-123", "MSFT", OrderSide.Buy, -1m, 10m, now));
        Assert.Throws<OrderValidationException>(() =>
            Order.Create("key-123", "MSFT", OrderSide.Buy, 1m, 0m, now));
        Assert.Throws<OrderValidationException>(() =>
            Order.Create("key-123", "MSFT", OrderSide.Buy, 1m, -10m, now));
    }

    [Fact]
    public void Repeated_cancellation_does_not_change_cancellation_timestamp()
    {
        var order = Order.Create("key-123", "MSFT", OrderSide.Sell, 1, 350.50m, DateTime.UtcNow);
        var first = DateTime.UtcNow;
        var second = first.AddMinutes(5);

        Assert.True(order.Cancel(first));
        Assert.False(order.Cancel(second));
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(first, order.CancelledAtUtc);
    }

    [Fact]
    public void Client_order_id_does_not_define_economic_equivalence()
    {
        var timestamp = DateTime.UtcNow;
        var left = Order.Create("id-1", "aapl", OrderSide.Buy, 3.25m, 175.20m, timestamp);
        var same = Order.Create("id-1", "AAPL", OrderSide.Buy, 3.25m, 175.20m, timestamp);
        var different = Order.Create("id-1", "AAPL", OrderSide.Buy, 4.25m, 175.20m, timestamp);
        Assert.True(left.MatchesIntent(same));
        Assert.False(left.MatchesIntent(different));
    }
}
