using ClientTradePortal.Trading.Api.Services.Interfaces;
using ClientTradePortal.Trading.Api.Repositories.Interfaces;
using ClientTradePortal.Trading.Api.Models.Requests;
using ClientTradePortal.Trading.Api.Models.Responses;

namespace ClientTradePortal.Trading.Api.Services;

public class ValidationService : IValidationService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IStockExchangeClient _stockExchangeClient;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(
        IAccountRepository accountRepository,
        IStockExchangeClient stockExchangeClient,
        ILogger<ValidationService> logger)
    {
        _accountRepository = accountRepository;
        _stockExchangeClient = stockExchangeClient;
        _logger = logger;
    }

    public async Task<ValidationResponse> ValidateOrderAsync(
        ValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating order: {Quantity} shares of {Symbol} for account {AccountId}",
            request.Quantity, request.Symbol, request.AccountId);

        var errors = new List<string>();

        // Validate quantity
        if (request.Quantity <= 0)
        {
            errors.Add("Quantity must be greater than zero");
        }

        if (request.Quantity > 10000)
        {
            errors.Add("Quantity cannot exceed 10,000 shares");
        }

        // Validate symbol
        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            errors.Add("Stock symbol is required");
        }

        // Get current price
        decimal currentPrice;
        try
        {
            currentPrice = await _stockExchangeClient.GetStockPriceAsync(request.Symbol, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stock price for validation");
            errors.Add("Unable to retrieve current stock price");

            return new ValidationResponse
            {
                IsValid = false,
                Errors = errors
            };
        }

        var totalAmount = currentPrice * request.Quantity;

        // Check sufficient funds
        try
        {
            var hasSufficientFunds = await _accountRepository.HasSufficientFundsAsync(
                request.AccountId,
                totalAmount,
                cancellationToken);

            if (!hasSufficientFunds)
            {
                var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
                errors.Add($"Insufficient funds. Required: €{totalAmount:N2}, Available: €{account?.CashBalance:N2}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check account funds");
            errors.Add("Unable to verify account balance");
        }

        return new ValidationResponse
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            CurrentPrice = currentPrice,
            TotalAmount = totalAmount
        };
    }
}