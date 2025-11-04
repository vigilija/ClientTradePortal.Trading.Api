namespace ClientTradePortal.Trading.Api.Models.Responses;

public class AccountBalanceResponse
{
    public Guid AccountId { get; set; }
    public decimal CashBalance { get; set; }
    public string Currency { get; set; } = "EUR";
}