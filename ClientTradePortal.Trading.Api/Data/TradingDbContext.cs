using Microsoft.EntityFrameworkCore;
using ClientTradePortal.Trading.Api.Entities;

namespace ClientTradePortal.Trading.Api.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<StockPosition> StockPositions => Set<StockPosition>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);

        // Seed data
        modelBuilder.Entity<Account>().HasData(
            new Account
            {
                AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ClientId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                CashBalance = 50000.00m,
                Currency = "EUR",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        modelBuilder.Entity<StockPosition>().HasData(
            new StockPosition
            {
                PositionId = Guid.NewGuid(),
                AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Symbol = "AAPL",
                Quantity = 10,
                AveragePrice = 150.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
    }
}