namespace ClientTradePortal.Trading.Api.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ITransactionRepository TransactionRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}