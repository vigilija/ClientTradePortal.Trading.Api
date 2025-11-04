using ClientTradePortal.Trading.Api.Entities;

namespace ClientTradePortal.Trading.Api.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
}