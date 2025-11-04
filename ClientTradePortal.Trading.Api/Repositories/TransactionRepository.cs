using ClientTradePortal.Trading.Api.Data;
using ClientTradePortal.Trading.Api.Entities;
using ClientTradePortal.Trading.Api.Repositories.Interfaces;

namespace ClientTradePortal.Trading.Api.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TradingDbContext _context;

    public TransactionRepository(TradingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }
}