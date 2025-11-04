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

public class ValidationServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IStockExchangeClient> _stockExchangeClientMock;
    private readonly Mock<ILogger<ValidationService>> _loggerMock;
    private readonly ValidationService _validationService;

    public ValidationServiceTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _stockExchangeClientMock = new Mock<IStockExchangeClient>();
        _loggerMock = new Mock<ILogger<ValidationService>>();

        _validationService = new ValidationService(
            _accountRepositoryMock.Object,
            _stockExchangeClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ValidateOrderAsync_ShouldReturnValid_WhenAllValidationsPass()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new ValidationRequest
        {
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = 10
        };

        var stockPrice = 150.00m;

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stockPrice);

        _accountRepositoryMock
            .Setup(x => x.HasSufficientFundsAsync(accountId, stockPrice * request.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _validationService.ValidateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.CurrentPrice.Should().Be(stockPrice);
        result.TotalAmount.Should().Be(1500.00m);
    }

    [Fact]
    public async Task ValidateOrderAsync_ShouldReturnError_WhenQuantityIsZero()
    {
        // Arrange
        var request = new ValidationRequest
        {
            AccountId = Guid.NewGuid(),
            Symbol = "AAPL",
            Quantity = 0
        };

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150.00m);

        _accountRepositoryMock
            .Setup(x => x.HasSufficientFundsAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _validationService.ValidateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Quantity must be greater than zero");
    }

    [Fact]
    public async Task ValidateOrderAsync_ShouldReturnError_WhenQuantityExceedsLimit()
    {
        // Arrange
        var request = new ValidationRequest
        {
            AccountId = Guid.NewGuid(),
            Symbol = "AAPL",
            Quantity = 15000
        };

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150.00m);

        _accountRepositoryMock
            .Setup(x => x.HasSufficientFundsAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _validationService.ValidateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Quantity cannot exceed 10,000 shares");
    }

    [Fact]
    public async Task ValidateOrderAsync_ShouldReturnError_WhenSymbolIsEmpty()
    {
        // Arrange
        var request = new ValidationRequest
        {
            AccountId = Guid.NewGuid(),
            Symbol = "",
            Quantity = 10
        };

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(150.00m);

        _accountRepositoryMock
            .Setup(x => x.HasSufficientFundsAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _validationService.ValidateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Stock symbol is required");
    }

    [Fact]
    public async Task ValidateOrderAsync_ShouldReturnError_WhenPriceFetchFails()
    {
        // Arrange
        var request = new ValidationRequest
        {
            AccountId = Guid.NewGuid(),
            Symbol = "INVALID",
            Quantity = 10
        };

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Stock not found"));

        // Act
        var result = await _validationService.ValidateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Unable to retrieve current stock price");
    }

    [Fact]
    public async Task ValidateOrderAsync_ShouldReturnError_WhenInsufficientFunds()
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

        var request = new ValidationRequest
        {
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = 100 // 100 * 150 = 15000
        };

        var stockPrice = 150.00m;

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stockPrice);

        _accountRepositoryMock
            .Setup(x => x.HasSufficientFundsAsync(accountId, stockPrice * request.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _validationService.ValidateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Insufficient funds"));
        result.CurrentPrice.Should().Be(stockPrice);
        result.TotalAmount.Should().Be(15000.00m);
    }

    [Fact]
    public async Task ValidateOrderAsync_ShouldReturnError_WhenAccountBalanceCheckFails()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new ValidationRequest
        {
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = 10
        };

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150.00m);

        _accountRepositoryMock
            .Setup(x => x.HasSufficientFundsAsync(accountId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _validationService.ValidateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Unable to verify account balance");
    }

    [Fact]
    public async Task ValidateOrderAsync_ShouldReturnMultipleErrors_WhenMultipleValidationsFail()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new ValidationRequest
        {
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = -5 // Invalid: negative quantity
        };

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150.00m);

        _accountRepositoryMock
            .Setup(x => x.HasSufficientFundsAsync(accountId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { CashBalance = 100.00m });

        // Act
        var result = await _validationService.ValidateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Errors.Should().Contain("Quantity must be greater than zero");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(1000)]
    [InlineData(10000)]
    public async Task ValidateOrderAsync_ShouldAcceptValidQuantities(int quantity)
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new ValidationRequest
        {
            AccountId = accountId,
            Symbol = "AAPL",
            Quantity = quantity
        };

        _stockExchangeClientMock
            .Setup(x => x.GetStockPriceAsync(request.Symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150.00m);

        _accountRepositoryMock
            .Setup(x => x.HasSufficientFundsAsync(accountId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _validationService.ValidateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
