namespace TradingOrders.Domain;

public enum OrderSide { Buy, Sell }
public enum OrderStatus { Pending, Cancelled }

/// <summary>
/// An illustrative limit order. This demo does not match orders, connect to a
/// broker, hold funds, or execute trades.
/// </summary>
public sealed class Order
{
    // Required by EF Core. The application creates orders through Create.
    private Order() { }

    public Guid Id { get; private set; }
    public string ClientOrderId { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public OrderSide Side { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal LimitPrice { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public static Order Create(
        string clientOrderId,
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal limitPrice,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(clientOrderId) || clientOrderId.Trim().Length > 64)
            throw new OrderValidationException("ClientOrderId is required and must be at most 64 characters.");
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Trim().Length > 16)
            throw new OrderValidationException("Symbol is required and must be at most 16 characters.");
        if (!Enum.IsDefined(side))
            throw new OrderValidationException("Side must be Buy or Sell.");
        if (quantity <= 0 || quantity > 1_000_000)
            throw new OrderValidationException("Quantity must be greater than zero and at most 1,000,000.");
        if (limitPrice <= 0 || limitPrice > 1_000_000_000)
            throw new OrderValidationException("LimitPrice must be greater than zero and at most 1,000,000,000.");
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be UTC.", nameof(nowUtc));

        return new Order
        {
            Id = Guid.NewGuid(),
            ClientOrderId = clientOrderId.Trim(),
            Symbol = symbol.Trim().ToUpperInvariant(),
            Side = side,
            Quantity = quantity,
            LimitPrice = limitPrice,
            Status = OrderStatus.Pending,
            CreatedAtUtc = nowUtc
        };
    }

    public bool MatchesIntent(Order other) =>
        Symbol == other.Symbol && Side == other.Side &&
        Quantity == other.Quantity && LimitPrice == other.LimitPrice;

    /// <summary>Returns false when an order was already cancelled (idempotent).</summary>
    public bool Cancel(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be UTC.", nameof(nowUtc));
        if (Status == OrderStatus.Cancelled) return false;
        Status = OrderStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        return true;
    }
}

public sealed class OrderValidationException(string message) : Exception(message);
