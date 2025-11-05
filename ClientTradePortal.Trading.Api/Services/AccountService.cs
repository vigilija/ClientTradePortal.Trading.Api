namespace ClientTradePortal.Trading.Api.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IStockExchangeClient _stockExchangeClient;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accountRepository,
        IStockExchangeClient stockExchangeClient,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _stockExchangeClient = stockExchangeClient;
        _logger = logger;
    }

    public async Task<AccountResponse?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting account {AccountId}", accountId);

        var account = await _accountRepository.GetWithPositionsAsync(accountId, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning("Account {AccountId} not found", accountId);
            return null;
        }

        // Get current prices for positions
        var positionsWithPrices = new List<StockPositionResponse>();

        foreach (var position in account.Positions)
        {
            try
            {
                var currentPrice = await _stockExchangeClient.GetStockPriceAsync(position.Symbol, cancellationToken);

                positionsWithPrices.Add(new StockPositionResponse
                {
                    Symbol = position.Symbol,
                    Quantity = position.Quantity,
                    AveragePrice = position.AveragePrice,
                    CurrentPrice = currentPrice
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get price for {Symbol}", position.Symbol);

                // Use average price as fallback
                positionsWithPrices.Add(new StockPositionResponse
                {
                    Symbol = position.Symbol,
                    Quantity = position.Quantity,
                    AveragePrice = position.AveragePrice,
                    CurrentPrice = position.AveragePrice
                });
            }
        }

        return new AccountResponse
        {
            AccountId = account.AccountId,
            ClientId = account.ClientId,
            CashBalance = account.CashBalance,
            Currency = account.Currency,
            Positions = positionsWithPrices
        };
    }

    public async Task<AccountBalanceResponse?> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting balance for account {AccountId}", accountId);

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning("Account {AccountId} not found", accountId);
            return null;
        }

        return new AccountBalanceResponse
        {
            AccountId = account.AccountId,
            CashBalance = account.CashBalance,
            Currency = account.Currency
        };
    }

    public async Task<List<StockPositionResponse>> GetPositionsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting positions for account {AccountId}", accountId);

        var account = await _accountRepository.GetWithPositionsAsync(accountId, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning("Account {AccountId} not found", accountId);
            return new List<StockPositionResponse>();
        }

        var positions = new List<StockPositionResponse>();

        foreach (var position in account.Positions)
        {
            try
            {
                var currentPrice = await _stockExchangeClient.GetStockPriceAsync(position.Symbol, cancellationToken);

                positions.Add(new StockPositionResponse
                {
                    Symbol = position.Symbol,
                    Quantity = position.Quantity,
                    AveragePrice = position.AveragePrice,
                    CurrentPrice = currentPrice
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get price for {Symbol}", position.Symbol);

                positions.Add(new StockPositionResponse
                {
                    Symbol = position.Symbol,
                    Quantity = position.Quantity,
                    AveragePrice = position.AveragePrice,
                    CurrentPrice = position.AveragePrice
                });
            }
        }

        return positions;
    }
}