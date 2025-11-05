namespace ClientTradePortal.Trading.Api.Services;

public class StockExchangeClient : IStockExchangeClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StockExchangeClient> _logger;

    public StockExchangeClient(HttpClient httpClient, ILogger<StockExchangeClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<decimal> GetStockPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting stock price for {Symbol} (Mock)", symbol);

        // TODO: Replace with actual external API call
        // For now, return mock prices
        var mockPrice = symbol.ToUpper() switch
        {
            "AAPL" => 175.50m,
            "MSFT" => 380.25m,
            "GOOGL" => 140.75m,
            "AMZN" => 145.30m,
            "TSLA" => 245.60m,
            _ => 100.00m
        };

        return Task.FromResult(mockPrice);
    }

    public Task<string> ExecuteOrderAsync(string symbol, int quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing order: {Quantity} shares of {Symbol} (Mock)", quantity, symbol);

        // TODO: Replace with actual external API call
        // For now, return mock exchange order ID
        var exchangeOrderId = $"EXC-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        return Task.FromResult(exchangeOrderId);
    }
}