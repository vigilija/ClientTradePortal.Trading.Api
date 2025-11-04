using ClientTradePortal.Trading.Api.Entities;
using ClientTradePortal.Trading.Api.Repositories.Interfaces;
using ClientTradePortal.Trading.Api.Services;
using ClientTradePortal.Trading.Api.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClientTradePortal.Trading.Api.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IStockExchangeClient> _stockExchangeClientMock;
    private readonly Mock<ILogger<AccountService>> _loggerMock;
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _stockExchangeClientMock = new Mock<IStockExchangeClient>();
        _loggerMock = new Mock<ILogger<AccountService>>();

        _accountService = new AccountService(
            _accountRepositoryMock.Object,
            _stockExchangeClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAccountAsync_ShouldReturnAccountWithCurrentPrices_WhenAccountExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            Positions = new List<StockPosition>
            {
                new StockPosition
                {
                    PositionId = Guid.NewGuid(),
                    AccountId = accountId,
                    Symbol = "AAPL",
                    Quantity = 10,
                    AveragePrice = 150.00m
                },
                new StockPosition
                {
                    PositionId = Guid.NewGuid(),
                    AccountId = accountId,
                    Symbol = "MSFT",
                    Quantity = 5,
                    AveragePrice = 380.00m
                }
            }
        };

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(175.50m);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync("MSFT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(390.25m);

        // Act
        var result = await _accountService.GetAccountAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result!.AccountId.Should().Be(accountId);
        result.CashBalance.Should().Be(50000.00m);
        result.Currency.Should().Be("EUR");
        result.Positions.Should().HaveCount(2);

        var applePosition = result.Positions.First(p => p.Symbol == "AAPL");
        applePosition.Quantity.Should().Be(10);
        applePosition.AveragePrice.Should().Be(150.00m);
        applePosition.CurrentPrice.Should().Be(175.50m);

        var msftPosition = result.Positions.First(p => p.Symbol == "MSFT");
        msftPosition.Quantity.Should().Be(5);
        msftPosition.AveragePrice.Should().Be(380.00m);
        msftPosition.CurrentPrice.Should().Be(390.25m);
    }

    [Fact]
    public async Task GetAccountAsync_ShouldReturnNull_WhenAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _accountService.GetAccountAsync(accountId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountAsync_ShouldUseFallbackPrice_WhenPriceFetchFails()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            Positions = new List<StockPosition>
            {
                new StockPosition
                {
                    PositionId = Guid.NewGuid(),
                    AccountId = accountId,
                    Symbol = "AAPL",
                    Quantity = 10,
                    AveragePrice = 150.00m
                }
            }
        };

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync("AAPL", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Stock exchange service unavailable"));

        // Act
        var result = await _accountService.GetAccountAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result!.Positions.Should().HaveCount(1);
        result.Positions[0].Symbol.Should().Be("AAPL");
        result.Positions[0].CurrentPrice.Should().Be(150.00m); // Fallback to average price
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldReturnBalance_WhenAccountExists()
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

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _accountService.GetBalanceAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result!.AccountId.Should().Be(accountId);
        result.CashBalance.Should().Be(50000.00m);
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task GetBalanceAsync_ShouldReturnNull_WhenAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _accountService.GetBalanceAsync(accountId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPositionsAsync_ShouldReturnPositions_WhenAccountExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            Positions = new List<StockPosition>
            {
                new StockPosition
                {
                    PositionId = Guid.NewGuid(),
                    AccountId = accountId,
                    Symbol = "GOOGL",
                    Quantity = 20,
                    AveragePrice = 140.00m
                }
            }
        };

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync("GOOGL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(145.75m);

        // Act
        var result = await _accountService.GetPositionsAsync(accountId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Symbol.Should().Be("GOOGL");
        result[0].Quantity.Should().Be(20);
        result[0].AveragePrice.Should().Be(140.00m);
        result[0].CurrentPrice.Should().Be(145.75m);
    }

    [Fact]
    public async Task GetPositionsAsync_ShouldReturnEmptyList_WhenAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _accountService.GetPositionsAsync(accountId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPositionsAsync_ShouldUseFallbackPrice_WhenPriceFetchFails()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            Positions = new List<StockPosition>
            {
                new StockPosition
                {
                    PositionId = Guid.NewGuid(),
                    AccountId = accountId,
                    Symbol = "TSLA",
                    Quantity = 15,
                    AveragePrice = 245.00m
                }
            }
        };

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync("TSLA", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network timeout"));

        // Act
        var result = await _accountService.GetPositionsAsync(accountId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Symbol.Should().Be("TSLA");
        result[0].CurrentPrice.Should().Be(245.00m); // Fallback to average price
    }

    [Fact]
    public async Task GetAccountAsync_ShouldHandleMixedPriceFetchResults()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            AccountId = accountId,
            ClientId = Guid.NewGuid(),
            CashBalance = 50000.00m,
            Currency = "EUR",
            Positions = new List<StockPosition>
            {
                new StockPosition
                {
                    PositionId = Guid.NewGuid(),
                    AccountId = accountId,
                    Symbol = "AAPL",
                    Quantity = 10,
                    AveragePrice = 150.00m
                },
                new StockPosition
                {
                    PositionId = Guid.NewGuid(),
                    AccountId = accountId,
                    Symbol = "MSFT",
                    Quantity = 5,
                    AveragePrice = 380.00m
                }
            }
        };

        _accountRepositoryMock
            .Setup(x => x.GetWithPositionsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // AAPL succeeds
        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(175.50m);

        // MSFT fails
        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync("MSFT", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Price not available"));

        // Act
        var result = await _accountService.GetAccountAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result!.Positions.Should().HaveCount(2);

        var applePosition = result.Positions.First(p => p.Symbol == "AAPL");
        applePosition.CurrentPrice.Should().Be(175.50m); // Real price

        var msftPosition = result.Positions.First(p => p.Symbol == "MSFT");
        msftPosition.CurrentPrice.Should().Be(380.00m); // Fallback to average
    }
}
