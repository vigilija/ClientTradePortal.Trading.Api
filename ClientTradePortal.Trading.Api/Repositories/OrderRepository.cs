namespace ClientTradePortal.Trading.Api.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly TradingDbContext _context;

    public OrderRepository(TradingDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
    }

    public async Task<Order?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<List<Order>> GetByAccountIdAsync(
        Guid accountId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.AccountId == accountId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }
}