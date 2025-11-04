# ClientTradePortal.Trading.Api Tests

This directory contains comprehensive test coverage for the Trading API.

## Test Projects

### 1. ClientTradePortal.Trading.Api.Tests (Unit Tests)
Unit tests for services and repositories using mocking and in-memory databases.

**Test Coverage:**
- **TradingService** - Order placement, idempotency, stock prices, order retrieval
- **AccountService** - Account retrieval, balance checks, position management
- **ValidationService** - Order validation, fund verification
- **AccountRepository** - Account queries, fund checks
- **OrderRepository** - Order queries, pagination, idempotency

**Key Features:**
- Uses Moq for mocking dependencies
- In-memory database for repository tests
- FluentAssertions for readable assertions
- xUnit test framework

### 2. ClientTradePortal.Trading.Api.IntegrationTests
End-to-end integration tests for API endpoints.

**Test Coverage:**
- Health check endpoint
- Account management endpoints (GET account, balance, positions)
- Trading endpoints (place order, get orders, stock quotes)
- Validation endpoints
- Error handling and validation

**Key Features:**
- WebApplicationFactory for in-memory API testing
- In-memory database with test data seeding
- Full HTTP request/response testing
- Tests all status codes (200, 201, 400, 404)

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run unit tests only
```bash
dotnet test Tests/ClientTradePortal.Trading.Api.Tests/ClientTradePortal.Trading.Api.Tests.csproj
```

### Run integration tests only
```bash
dotnet test Tests/ClientTradePortal.Trading.Api.IntegrationTests/ClientTradePortal.Trading.Api.IntegrationTests.csproj
```

### Run with coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Structure

### Unit Tests
```
Tests/ClientTradePortal.Trading.Api.Tests/
├── Services/
│   ├── TradingServiceTests.cs
│   ├── AccountServiceTests.cs
│   └── ValidationServiceTests.cs
├── Repositories/
│   ├── AccountRepositoryTests.cs
│   └── OrderRepositoryTests.cs
└── GlobalUsings.cs
```

### Integration Tests
```
Tests/ClientTradePortal.Trading.Api.IntegrationTests/
├── TradingApiFactory.cs (WebApplicationFactory)
├── TradingApiIntegrationTests.cs
└── GlobalUsings.cs
```

## Test Scenarios Covered

### TradingService
- ✅ Get stock price
- ✅ Place order successfully
- ✅ Idempotency handling (duplicate prevention)
- ✅ Insufficient funds validation
- ✅ Account not found handling
- ✅ Exchange execution failure
- ✅ Create new position
- ✅ Update existing position
- ✅ Get order by ID
- ✅ Get orders with pagination

### AccountService
- ✅ Get account with positions
- ✅ Account not found
- ✅ Fallback to average price on error
- ✅ Get balance
- ✅ Get positions
- ✅ Mixed price fetch results

### ValidationService
- ✅ Valid order validation
- ✅ Invalid quantity (zero, negative, exceeds limit)
- ✅ Empty symbol validation
- ✅ Price fetch failure
- ✅ Insufficient funds
- ✅ Multiple validation errors
- ✅ Boundary testing

### Repository Tests
- ✅ CRUD operations
- ✅ Query methods
- ✅ Pagination
- ✅ Fund sufficiency checks
- ✅ Entity relationships
- ✅ Idempotency key lookup

### Integration Tests
- ✅ All API endpoints
- ✅ Request validation
- ✅ HTTP status codes
- ✅ Error responses
- ✅ Idempotent order placement
- ✅ End-to-end workflows

## Dependencies

- **xUnit** - Test framework
- **Moq** - Mocking library
- **FluentAssertions** - Fluent assertion library
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database for testing
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing support

## Notes

- All tests use in-memory databases for isolation
- Integration tests automatically seed test data
- Tests are independent and can run in parallel
- Test data uses fixed GUIDs for predictability
- Exception scenarios are thoroughly tested
