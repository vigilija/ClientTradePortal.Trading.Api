namespace ClientTradePortal.Trading.Api.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly TradingDbContext _context;

    public AccountRepository(TradingDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);
    }

    public async Task<Account?> GetWithPositionsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Positions)
            .FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasSufficientFundsAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default)
    {
        var balance = await _context.Accounts
            .Where(a => a.AccountId == accountId)
            .Select(a => a.CashBalance)
            .FirstOrDefaultAsync(cancellationToken);

        return balance >= amount;
    }
}