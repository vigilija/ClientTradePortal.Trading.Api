namespace ClientTradePortal.Trading.Api.Repositories.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Account?> GetWithPositionsAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task<bool> HasSufficientFundsAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default);
}