using System.Net;
using System.Net.Http.Json;
using ClientTradePortal.Trading.Api.Models.Requests;
using ClientTradePortal.Trading.Api.Models.Responses;
using FluentAssertions;
using Xunit;

namespace ClientTradePortal.Trading.Api.IntegrationTests;

public class TradingApiIntegrationTests : IClassFixture<TradingApiFactory>
{
    private readonly HttpClient _client;
    private readonly Guid _testAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public TradingApiIntegrationTests(TradingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetAccount_ShouldReturnAccount_WhenAccountExists()
    {
        // Act
        var response = await _client.GetAsync($"/api/accounts/{_testAccountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccountId.Should().Be(_testAccountId);
        result.Data.CashBalance.Should().Be(50000.00m);
        result.Data.Currency.Should().Be("EUR");
        result.Data.Positions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAccount_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        // Arrange
        var nonExistentAccountId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/accounts/{nonExistentAccountId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBalance_ShouldReturnBalance_WhenAccountExists()
    {
        // Act
        var response = await _client.GetAsync($"/api/accounts/{_testAccountId}/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AccountBalanceResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccountId.Should().Be(_testAccountId);
        result.Data.CashBalance.Should().Be(50000.00m);
    }

    [Fact]
    public async Task GetPositions_ShouldReturnPositions_WhenAccountExists()
    {
        // Act
        var response = await _client.GetAsync($"/api/accounts/{_testAccountId}/positions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<StockPositionResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data![0].Symbol.Should().Be("AAPL");
        result.Data[0].Quantity.Should().Be(10);
    }

    [Fact]
    public async Task GetStockQuote_ShouldReturnPrice_WhenSymbolIsValid()
    {
        // Act
        var response = await _client.GetAsync("/api/trading/quote?symbol=AAPL");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StockQuoteResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Symbol.Should().Be("AAPL");
        result.Data.Price.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PlaceOrder_ShouldCreateOrder_WhenRequestIsValid()
    {
        // Arrange
        var request = new OrderRequest
        {
            AccountId = _testAccountId,
            Symbol = "AAPL",
            Quantity = 5,
            IdempotencyKey = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/trading/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Symbol.Should().Be("AAPL");
        result.Data.Quantity.Should().Be(5);
        result.Data.Status.Should().Be("Executed");
        result.Data.OrderId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnBadRequest_WhenAccountIdIsEmpty()
    {
        // Arrange
        var request = new OrderRequest
        {
            AccountId = Guid.Empty,
            Symbol = "AAPL",
            Quantity = 5,
            IdempotencyKey = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/trading/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnBadRequest_WhenSymbolIsInvalid()
    {
        // Arrange
        var request = new OrderRequest
        {
            AccountId = _testAccountId,
            Symbol = "invalid123",
            Quantity = 5,
            IdempotencyKey = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/trading/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnBadRequest_WhenQuantityIsZero()
    {
        // Arrange
        var request = new OrderRequest
        {
            AccountId = _testAccountId,
            Symbol = "AAPL",
            Quantity = 0,
            IdempotencyKey = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/trading/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnBadRequest_WhenQuantityExceedsLimit()
    {
        // Arrange
        var request = new OrderRequest
        {
            AccountId = _testAccountId,
            Symbol = "AAPL",
            Quantity = 15000,
            IdempotencyKey = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/trading/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnSameOrder_WhenIdempotencyKeyIsReused()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var request = new OrderRequest
        {
            AccountId = _testAccountId,
            Symbol = "MSFT",
            Quantity = 2,
            IdempotencyKey = idempotencyKey
        };

        // Act - Place order first time
        var response1 = await _client.PostAsJsonAsync("/api/trading/orders", request);
        var result1 = await response1.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>();

        // Act - Place same order again with same idempotency key
        var response2 = await _client.PostAsJsonAsync("/api/trading/orders", request);
        var result2 = await response2.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        result1!.Data!.OrderId.Should().Be(result2!.Data!.OrderId);
        result1.Data.Symbol.Should().Be(result2.Data.Symbol);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange - First create an order
        var request = new OrderRequest
        {
            AccountId = _testAccountId,
            Symbol = "GOOGL",
            Quantity = 3,
            IdempotencyKey = Guid.NewGuid()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/trading/orders", request);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>();
        var orderId = createResult!.Data!.OrderId;

        // Act
        var response = await _client.GetAsync($"/api/trading/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.OrderId.Should().Be(orderId);
        result.Data.Symbol.Should().Be("GOOGL");
    }

    [Fact]
    public async Task GetOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var nonExistentOrderId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/trading/orders/{nonExistentOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrders_ShouldReturnOrders_WhenAccountHasOrders()
    {
        // Arrange - Create a couple of orders
        for (int i = 0; i < 3; i++)
        {
            var request = new OrderRequest
            {
                AccountId = _testAccountId,
                Symbol = "TSLA",
                Quantity = 1,
                IdempotencyKey = Guid.NewGuid()
            };
            await _client.PostAsJsonAsync("/api/trading/orders", request);
        }

        // Act
        var response = await _client.GetAsync($"/api/trading/orders?accountId={_testAccountId}&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<OrderResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateOrder_ShouldReturnValid_WhenOrderIsValid()
    {
        // Arrange
        var request = new ValidationRequest
        {
            AccountId = _testAccountId,
            Symbol = "AAPL",
            Quantity = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/validation/order", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ValidationResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsValid.Should().BeTrue();
        result.Data.Errors.Should().BeEmpty();
        result.Data.CurrentPrice.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateOrder_ShouldReturnInvalid_WhenInsufficientFunds()
    {
        // Arrange
        var request = new ValidationRequest
        {
            AccountId = _testAccountId,
            Symbol = "AAPL",
            Quantity = 10000 // Too many shares
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/validation/order", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ValidationResponse>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.IsValid.Should().BeFalse();
        result.Data.Errors.Should().Contain(e => e.Contains("Insufficient funds"));
    }
}
