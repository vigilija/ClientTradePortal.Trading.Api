namespace ClientTradePortal.Trading.Api.Models.Requests;

public class ValidationRequest
{
    public Guid AccountId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal EstimatedPrice { get; set; }
}