using ClientTradePortal.Trading.Api.Data;
using ClientTradePortal.Trading.Api.Entities;
using ClientTradePortal.Trading.Api.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClientTradePortal.Trading.Api.Tests.Repositories;

public class AccountRepositoryTests : IDisposable
{
    private readonly TradingDbContext _context;
    private readonly AccountRepository _repository;

    public AccountRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TradingDbContext(options);
        _repository = new AccountRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAccount_WhenAccountExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result!.AccountId.Should().Be(accountId);
        result.CashBalance.Should().Be(50000.00m);
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(accountId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithPositionsAsync_ShouldReturnAccountWithPositions_WhenAccountExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var positions = new List<StockPosition>
        {
            new StockPosition
            {
                PositionId = Guid.NewGuid(),
                AccountId = accountId,
                Symbol = "AAPL",
                Quantity = 10,
                AveragePrice = 150.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new StockPosition
            {
                PositionId = Guid.NewGuid(),
                AccountId = accountId,
                Symbol = "MSFT",
                Quantity = 5,
                AveragePrice = 380.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Accounts.AddAsync(account);
        await _context.StockPositions.AddRangeAsync(positions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithPositionsAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result!.AccountId.Should().Be(accountId);
        result.Positions.Should().HaveCount(2);
        result.Positions.Should().Contain(p => p.Symbol == "AAPL");
        result.Positions.Should().Contain(p => p.Symbol == "MSFT");
    }

    [Fact]
    public async Task GetWithPositionsAsync_ShouldReturnNull_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var result = await _repository.GetWithPositionsAsync(accountId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HasSufficientFundsAsync_ShouldReturnTrue_WhenBalanceIsSufficient()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasSufficientFundsAsync(accountId, 30000.00m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSufficientFundsAsync_ShouldReturnFalse_WhenBalanceIsInsufficient()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 10000.00m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasSufficientFundsAsync(accountId, 30000.00m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasSufficientFundsAsync_ShouldReturnFalse_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var result = await _repository.HasSufficientFundsAsync(accountId, 1000.00m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAccount_WhenAccountExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Detach the entity to simulate getting it from another context
        _context.Entry(account).State = EntityState.Detached;

        // Modify the account
        account.CashBalance = 45000.00m;
        account.UpdatedAt = DateTime.UtcNow;

        // Act
        await _repository.UpdateAsync(account);

        // Assert
        var updatedAccount = await _context.Accounts.FindAsync(accountId);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.CashBalance.Should().Be(45000.00m);
    }

    [Fact]
    public async Task HasSufficientFundsAsync_ShouldReturnTrue_WhenAmountEqualsBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasSufficientFundsAsync(accountId, 50000.00m);

        // Assert
        result.Should().BeTrue();
    }
}
