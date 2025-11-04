//using System.Text.Json.Serialization;

//var builder = WebApplication.CreateSlimBuilder(args);

//builder.Services.ConfigureHttpJsonOptions(options =>
//{
//    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
//});

//var app = builder.Build();

//var sampleTodos = new Todo[] {
//    new(1, "Walk the dog"),
//    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
//    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
//    new(4, "Clean the bathroom"),
//    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
//};

//var todosApi = app.MapGroup("/todos");
//todosApi.MapGet("/", () => sampleTodos);
//todosApi.MapGet("/{id}", (int id) =>
//    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
//        ? Results.Ok(todo)
//        : Results.NotFound());

//app.Run();

//public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

//[JsonSerializable(typeof(Todo[]))]
//internal partial class AppJsonSerializerContext : JsonSerializerContext
//{

//}
// Contains:
// - Service registration (DI)
// - Middleware pipeline
// - API endpoints (routes)
// - App configuration
using Microsoft.EntityFrameworkCore;
using ClientTradePortal.Trading.Api.Data;
using ClientTradePortal.Trading.Api.Services;
using ClientTradePortal.Trading.Api.Services.Interfaces;
using ClientTradePortal.Trading.Api.Repositories;
using ClientTradePortal.Trading.Api.Repositories.Interfaces;
using ClientTradePortal.Trading.Api.Middleware;
using ClientTradePortal.Trading.Api.Models.Requests;
using ClientTradePortal.Trading.Api.Models.Responses;
using FluentValidation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/api-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Trading API",
        Version = "v1",
        Description = "API for stock trading operations"
    });
});

// Database
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:7190", "http://localhost:5028")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITradingService, TradingService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IStockExchangeClient, StockExchangeClient>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Memory Cache
builder.Services.AddMemoryCache();

// HTTP Client for external stock exchange API
builder.Services.AddHttpClient<IStockExchangeClient, StockExchangeClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalApis:StockExchange"] ?? "https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowBlazorClient");

// ==================== API ENDPOINTS ====================

// Health Check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

// ==================== ACCOUNT ENDPOINTS ====================

app.MapGet("/api/accounts/{accountId:guid}", async (
    Guid accountId,
    IAccountService accountService) =>
{
    var account = await accountService.GetAccountAsync(accountId);

    if (account == null)
        return Results.NotFound(ApiResponse<AccountResponse>.ErrorResponse("Account not found"));

    return Results.Ok(ApiResponse<AccountResponse>.SuccessResponse(account));
})
.WithName("GetAccount")
.WithTags("Accounts")
.Produces<ApiResponse<AccountResponse>>(StatusCodes.Status200OK)
.Produces<ApiResponse<AccountResponse>>(StatusCodes.Status404NotFound);

app.MapGet("/api/accounts/{accountId:guid}/balance", async (
    Guid accountId,
    IAccountService accountService) =>
{
    var balance = await accountService.GetBalanceAsync(accountId);

    if (balance == null)
        return Results.NotFound(ApiResponse<AccountBalanceResponse>.ErrorResponse("Account not found"));

    return Results.Ok(ApiResponse<AccountBalanceResponse>.SuccessResponse(balance));
})
.WithName("GetAccountBalance")
.WithTags("Accounts")
.Produces<ApiResponse<AccountBalanceResponse>>(StatusCodes.Status200OK);

app.MapGet("/api/accounts/{accountId:guid}/positions", async (
    Guid accountId,
    IAccountService accountService) =>
{
    var positions = await accountService.GetPositionsAsync(accountId);
    return Results.Ok(ApiResponse<List<StockPositionResponse>>.SuccessResponse(positions));
})
.WithName("GetAccountPositions")
.WithTags("Accounts")
.Produces<ApiResponse<List<StockPositionResponse>>>(StatusCodes.Status200OK);

// ==================== TRADING ENDPOINTS ====================

app.MapGet("/api/trading/quote", async (
    string symbol,
    ITradingService tradingService) =>
{
    var price = await tradingService.GetStockPriceAsync(symbol);

    var quote = new StockQuoteResponse
    {
        Symbol = symbol,
        Price = price,
        Timestamp = DateTime.UtcNow
    };

    return Results.Ok(ApiResponse<StockQuoteResponse>.SuccessResponse(quote));
})
.WithName("GetStockQuote")
.WithTags("Trading")
.Produces<ApiResponse<StockQuoteResponse>>(StatusCodes.Status200OK);

app.MapPost("/api/trading/orders", async (
    OrderRequest request,
    ITradingService tradingService,
    IValidator<OrderRequest> validator) =>
{
    // Validate request
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(ApiResponse<OrderResponse>.ErrorResponse(
            "Validation failed",
            validationResult.Errors.Select(e => e.ErrorMessage).ToArray()));
    }

    var order = await tradingService.PlaceOrderAsync(request);

    return Results.Created($"/api/trading/orders/{order.OrderId}",
        ApiResponse<OrderResponse>.SuccessResponse(order));
})
.WithName("PlaceOrder")
.WithTags("Trading")
.Produces<ApiResponse<OrderResponse>>(StatusCodes.Status201Created)
.Produces<ApiResponse<OrderResponse>>(StatusCodes.Status400BadRequest);

app.MapGet("/api/trading/orders/{orderId:guid}", async (
    Guid orderId,
    ITradingService tradingService) =>
{
    var order = await tradingService.GetOrderAsync(orderId);

    if (order == null)
        return Results.NotFound(ApiResponse<OrderResponse>.ErrorResponse("Order not found"));

    return Results.Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
})
.WithName("GetOrder")
.WithTags("Trading")
.Produces<ApiResponse<OrderResponse>>(StatusCodes.Status200OK)
.Produces<ApiResponse<OrderResponse>>(StatusCodes.Status404NotFound);

app.MapGet("/api/trading/orders", async (
    Guid accountId,
    int pageNumber,
    int pageSize,
    ITradingService tradingService) =>
{
    var orders = await tradingService.GetOrdersAsync(accountId, pageNumber, pageSize);
    return Results.Ok(ApiResponse<List<OrderResponse>>.SuccessResponse(orders));
})
.WithName("GetOrders")
.WithTags("Trading")
.Produces<ApiResponse<List<OrderResponse>>>(StatusCodes.Status200OK);

// ==================== VALIDATION ENDPOINTS ====================

app.MapPost("/api/validation/order", async (
    ValidationRequest request,
    IValidationService validationService) =>
{
    var result = await validationService.ValidateOrderAsync(request);
    return Results.Ok(ApiResponse<ValidationResponse>.SuccessResponse(result));
})
.WithName("ValidateOrder")
.WithTags("Validation")
.Produces<ApiResponse<ValidationResponse>>(StatusCodes.Status200OK);

// ==================== RUN APPLICATION ====================

// Apply migrations on startup (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

    try
    {
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error applying database migrations");
    }
}

Log.Information("Starting Van Lanschot Trading API");

app.Run();