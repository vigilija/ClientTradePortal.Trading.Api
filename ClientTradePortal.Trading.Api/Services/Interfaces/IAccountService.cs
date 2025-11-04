using ClientTradePortal.Trading.Api.Models.Responses;

namespace ClientTradePortal.Trading.Api.Services.Interfaces;

public interface IAccountService
{
    Task<AccountResponse?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<AccountBalanceResponse?> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<List<StockPositionResponse>> GetPositionsAsync(Guid accountId, CancellationToken cancellationToken = default);
}