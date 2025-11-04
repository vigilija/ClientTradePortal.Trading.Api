namespace ClientTradePortal.Trading.Api.Entities;

public class Account
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public decimal CashBalance { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<StockPosition> Positions { get; set; } = new List<StockPosition>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}