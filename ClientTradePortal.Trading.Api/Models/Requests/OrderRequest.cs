namespace ClientTradePortal.Trading.Api.Models.Requests;

public class OrderRequest
{
    public Guid AccountId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Guid IdempotencyKey { get; set; }
}