namespace ClientTradePortal.Trading.Api.Models.Responses;

public class StockPositionResponse
{
    public string Symbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentPrice { get; set; }
}