using ClientTradePortal.Trading.Api.Entities;
using ClientTradePortal.Trading.Api.Models.Requests;
using ClientTradePortal.Trading.Api.Repositories.Interfaces;
using ClientTradePortal.Trading.Api.Services;
using ClientTradePortal.Trading.Api.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClientTradePortal.Trading.Api.Tests.Services;

public class TradingServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IStockExchangeClient> _stockExchangeClientMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<TradingService>> _loggerMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly TradingService _tradingService;

    public TradingServiceTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _stockExchangeClientMock = new Mock<IStockExchangeClient>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<TradingService>>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();

        // Setup UnitOfWork to return transaction repository
        _unitOfWorkMock.Setup(u => u.TransactionRepository).Returns(_transactionRepositoryMock.Object);

        _tradingService = new TradingService(
            _accountRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _stockExchangeClientMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetStockPriceAsync_ShouldReturnPrice_WhenSymbolIsValid()
    {
        // Arrange
        var symbol = "AAPL";
        var expectedPrice = 150.50m;
        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPrice);

        // Act
        var result = await _tradingService.GetStockPriceAsync(symbol);

        // Assert
        result.Should().Be(expectedPrice);
        _stockExchangeClientMock.Verify(x => x.GetStockPriceAsync(symbol, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldReturnExistingOrder_WhenIdempotencyKeyExists()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var existingOrder = new Order
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

        var request = new OrderRequest
        {
            AccountId = existingOrder.AccountId,
            Symbol = "AAPL",
            Quantity = 10,
            IdempotencyKey = idempotencyKey
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _tradingService.PlaceOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(existingOrder.OrderId);
        result.Symbol.Should().Be(existingOrder.Symbol);
        result.Status.Should().Be(OrderStatus.Executed.ToString());

        // Verify no new order was created
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldThrowException_WhenAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new OrderRequest
        {
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = 10,
            IdempotencyKey = Guid.NewGuid()
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150.00m);

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act
        var act = async () => await _tradingService.PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Account {accountId} not found");

        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldThrowException_WhenInsufficientFunds()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 1000.00m,
            Currency = "EUR",
            Positions = new List<StockPosition>()
        };

        var request = new OrderRequest
        {
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = 100, // 100 shares * 150 = 15000 (more than balance of 1000)
            IdempotencyKey = Guid.NewGuid()
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150.00m);

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var act = async () => await _tradingService.PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient funds*");

        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldCreateNewPosition_WhenPositionDoesNotExist()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            Positions = new List<StockPosition>()
        };

        var request = new OrderRequest
        {
            AccountId = accountId,
            Symbol = "MSFT",
            Quantity = 10,
            IdempotencyKey = Guid.NewGuid()
        };

        var stockPrice = 380.25m;
        var exchangeOrderId = "EXG-12345";

        _orderRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stockPrice);

        _stockExchangeClientMock
            .Setup(x => x.ExecuteOrderAsync(request.Symbol, request.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeOrderId);

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _tradingService.PlaceOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Symbol.Should().Be("MSFT");
        result.Quantity.Should().Be(10);
        result.PricePerShare.Should().Be(stockPrice);
        result.TotalAmount.Should().Be(stockPrice * 10);
        result.Status.Should().Be(OrderStatus.Executed.ToString());

        // Verify new position was added
        account.Positions.Should().HaveCount(1);
        account.Positions[0].Symbol.Should().Be("MSFT");
        account.Positions[0].Quantity.Should().Be(10);
        account.Positions[0].AveragePrice.Should().Be(stockPrice);

        // Verify balance was updated
        account.CashBalance.Should().Be(50000.00m - (stockPrice * 10));

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldUpdateExistingPosition_WhenPositionExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var existingPosition = new StockPosition
        {
            PositionId = Guid.NewGuid(),
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = 10,
            AveragePrice = 150.00m,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            Positions = new List<StockPosition> { existingPosition }
        };

        var request = new OrderRequest
        {
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = 5,
            IdempotencyKey = Guid.NewGuid()
        };

        var stockPrice = 160.00m;
        var exchangeOrderId = "EXG-67890";

        _orderRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stockPrice);

        _stockExchangeClientMock
            .Setup(x => x.ExecuteOrderAsync(request.Symbol, request.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeOrderId);

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _tradingService.PlaceOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Executed.ToString());

        // Verify position was updated with weighted average
        // Old: 10 shares @ 150 = 1500
        // New: 5 shares @ 160 = 800
        // Total: 15 shares @ 153.33 (2300 / 15)
        account.Positions.Should().HaveCount(1);
        account.Positions[0].Quantity.Should().Be(15);
        account.Positions[0].AveragePrice.Should().BeApproximately(153.33m, 0.01m);

        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldRollbackTransaction_WhenExchangeExecutionFails()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            Positions = new List<StockPosition>()
        };

        var request = new OrderRequest
        {
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = 10,
            IdempotencyKey = Guid.NewGuid()
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150.00m);

        _stockExchangeClientMock
            .Setup(x => x.ExecuteOrderAsync(request.Symbol, request.Quantity, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Exchange service unavailable"));

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var act = async () => await _tradingService.PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Exchange service unavailable");

        // Verify transaction was committed to save the failed order status
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        // Verify order was marked as failed
        _orderRepositoryMock.Verify(x => x.AddAsync(It.Is<Order>(o => o.Status == OrderStatus.Pending), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrderAsync_ShouldReturnOrder_WhenOrderExists()
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

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _tradingService.GetOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.Symbol.Should().Be("AAPL");
        result.Quantity.Should().Be(10);
    }

    [Fact]
    public async Task GetOrderAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _tradingService.GetOrderAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldReturnOrders_WhenAccountHasOrders()
    {
        // Arrange
        var accountId = Guid.NewGuid();
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
                CreatedAt = DateTime.UtcNow,
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
                CreatedAt = DateTime.UtcNow,
                IdempotencyKey = Guid.NewGuid()
            }
        };

        _orderRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(accountId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _tradingService.GetOrdersAsync(accountId, 1, 10);

        // Assert
        result.Should().HaveCount(2);
        result[0].Symbol.Should().Be("AAPL");
        result[1].Symbol.Should().Be("MSFT");
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldReturnEmptyList_WhenAccountHasNoOrders()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _orderRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(accountId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        // Act
        var result = await _tradingService.GetOrdersAsync(accountId, 1, 10);

        // Assert
        result.Should().BeEmpty();
    }
}
