using ClientTradePortal.Trading.Api.Data;
using ClientTradePortal.Trading.Api.Entities;
using ClientTradePortal.Trading.Api.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClientTradePortal.Trading.Api.Tests.Repositories;

public class OrderRepositoryTests : IDisposable
{
    private readonly TradingDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TradingDbContext(options);
        _repository = new OrderRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            OrderId = orderId,
            AccountId = Guid.NewGuid(),
            Symbol = "AAPL",
            OrderType = OrderType.Buy,
            Quantity = 10,
            PricePerShare = 150.00m,
            TotalAmount = 1500.00m,
            Status = OrderStatus.Executed,
            CreatedAt = DateTime.UtcNow,
            IdempotencyKey = Guid.NewGuid()
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.Symbol.Should().Be("AAPL");
        result.Quantity.Should().Be(10);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Symbol = "AAPL",
            OrderType = OrderType.Buy,
            Quantity = 10,
            PricePerShare = 150.00m,
            TotalAmount = 1500.00m,
            Status = OrderStatus.Executed,
            CreatedAt = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdempotencyKeyAsync(idempotencyKey);

        // Assert
        result.Should().NotBeNull();
        result!.IdempotencyKey.Should().Be(idempotencyKey);
        result.Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdempotencyKeyAsync(idempotencyKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByAccountIdAsync_ShouldReturnOrders_OrderedByCreatedAtDescending()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var orders = new List<Order>
        {
            new Order
            {
                OrderId = Guid.NewGuid(),
                AccountId = accountId,
                Symbol = "AAPL",
                OrderType = OrderType.Buy,
                Quantity = 10,
                PricePerShare = 150.00m,
                TotalAmount = 1500.00m,
                Status = OrderStatus.Executed,
                CreatedAt = now.AddMinutes(-10),
                IdempotencyKey = Guid.NewGuid()
            },
            new Order
            {
                OrderId = Guid.NewGuid(),
                AccountId = accountId,
                Symbol = "MSFT",
                OrderType = OrderType.Buy,
                Quantity = 5,
                PricePerShare = 380.00m,
                TotalAmount = 1900.00m,
                Status = OrderStatus.Executed,
                CreatedAt = now.AddMinutes(-5),
                IdempotencyKey = Guid.NewGuid()
            },
            new Order
            {
                OrderId = Guid.NewGuid(),
                AccountId = accountId,
                Symbol = "GOOGL",
                OrderType = OrderType.Buy,
                Quantity = 20,
                PricePerShare = 140.00m,
                TotalAmount = 2800.00m,
                Status = OrderStatus.Executed,
                CreatedAt = now,
                IdempotencyKey = Guid.NewGuid()
            }
        };

        await _context.Orders.AddRangeAsync(orders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByAccountIdAsync(accountId, 1, 10);

        // Assert
        result.Should().HaveCount(3);
        result[0].Symbol.Should().Be("GOOGL"); // Most recent
        result[1].Symbol.Should().Be("MSFT");
        result[2].Symbol.Should().Be("AAPL"); // Oldest
    }

    [Fact]
    public async Task GetByAccountIdAsync_ShouldRespectPagination()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var orders = new List<Order>();
        for (int i = 0; i < 25; i++)
        {
            orders.Add(new Order
            {
                OrderId = Guid.NewGuid(),
                AccountId = accountId,
                Symbol = $"SYM{i}",
                OrderType = OrderType.Buy,
                Quantity = 10,
                PricePerShare = 100.00m,
                TotalAmount = 1000.00m,
                Status = OrderStatus.Executed,
                CreatedAt = now.AddMinutes(-i),
                IdempotencyKey = Guid.NewGuid()
            });
        }

        await _context.Orders.AddRangeAsync(orders);
        await _context.SaveChangesAsync();

        // Act - Get page 1 (first 10)
        var page1 = await _repository.GetByAccountIdAsync(accountId, 1, 10);
        var page2 = await _repository.GetByAccountIdAsync(accountId, 2, 10);
        var page3 = await _repository.GetByAccountIdAsync(accountId, 3, 10);

        // Assert
        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
        page3.Should().HaveCount(5);

        // Ensure no overlap
        var allSymbols = page1.Select(o => o.Symbol)
            .Concat(page2.Select(o => o.Symbol))
            .Concat(page3.Select(o => o.Symbol))
            .ToList();

        allSymbols.Should().HaveCount(25);
        allSymbols.Distinct().Should().HaveCount(25);
    }

    [Fact]
    public async Task GetByAccountIdAsync_ShouldReturnEmptyList_WhenAccountHasNoOrders()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByAccountIdAsync(accountId, 1, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByAccountIdAsync_ShouldOnlyReturnOrdersForSpecificAccount()
    {
        // Arrange
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();

        var orders = new List<Order>
        {
            new Order
            {
                OrderId = Guid.NewGuid(),
                AccountId = accountId1,
                Symbol = "AAPL",
                OrderType = OrderType.Buy,
                Quantity = 10,
                PricePerShare = 150.00m,
                TotalAmount = 1500.00m,
                Status = OrderStatus.Executed,
                CreatedAt = DateTime.UtcNow,
                IdempotencyKey = Guid.NewGuid()
            },
            new Order
            {
                OrderId = Guid.NewGuid(),
                AccountId = accountId2,
                Symbol = "MSFT",
                OrderType = OrderType.Buy,
                Quantity = 5,
                PricePerShare = 380.00m,
                TotalAmount = 1900.00m,
                Status = OrderStatus.Executed,
                CreatedAt = DateTime.UtcNow,
                IdempotencyKey = Guid.NewGuid()
            }
        };

        await _context.Orders.AddRangeAsync(orders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByAccountIdAsync(accountId1, 1, 10);

        // Assert
        result.Should().HaveCount(1);
        result[0].AccountId.Should().Be(accountId1);
        result[0].Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task AddAsync_ShouldAddOrder()
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Symbol = "AAPL",
            OrderType = OrderType.Buy,
            Quantity = 10,
            PricePerShare = 150.00m,
            TotalAmount = 1500.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            IdempotencyKey = Guid.NewGuid()
        };

        // Act
        await _repository.AddAsync(order);
        await _context.SaveChangesAsync();

        // Assert
        var savedOrder = await _context.Orders.FindAsync(order.OrderId);
        savedOrder.Should().NotBeNull();
        savedOrder!.Symbol.Should().Be("AAPL");
        savedOrder.Status.Should().Be(OrderStatus.Pending);
    }
}
