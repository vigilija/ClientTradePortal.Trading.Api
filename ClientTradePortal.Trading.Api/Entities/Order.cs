namespace ClientTradePortal.Trading.Api.Entities;

public class Order
{
    public Guid OrderId { get; set; }
    public Guid AccountId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public int Quantity { get; set; }
    public decimal PricePerShare { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string? ExchangeOrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid IdempotencyKey { get; set; }

    // Navigation property
    public Account Account { get; set; } = null!;
}

public enum OrderType
{
    Buy,
    Sell
}

public enum OrderStatus
{
    Pending,
    Executed,
    Failed,
    Cancelled
}