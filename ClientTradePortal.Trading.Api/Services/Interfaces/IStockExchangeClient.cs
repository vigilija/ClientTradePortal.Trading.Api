namespace ClientTradePortal.Trading.Api.Services.Interfaces;

public interface IStockExchangeClient
{
    Task<decimal> GetStockPriceAsync(string symbol, CancellationToken cancellationToken = default);
    Task<string> ExecuteOrderAsync(string symbol, int quantity, CancellationToken cancellationToken = default);
}