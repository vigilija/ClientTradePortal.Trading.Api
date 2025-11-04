namespace ClientTradePortal.Trading.Api.Models.Responses;

public class StockQuoteResponse
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}