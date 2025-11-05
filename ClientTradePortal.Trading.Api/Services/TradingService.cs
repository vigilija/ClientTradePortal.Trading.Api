namespace ClientTradePortal.Trading.Api.Services;

public class TradingService : ITradingService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IStockExchangeClient _stockExchangeClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TradingService> _logger;

    public TradingService(
        IAccountRepository accountRepository,
        IOrderRepository orderRepository,
        IStockExchangeClient stockExchangeClient,
        IUnitOfWork unitOfWork,
        ILogger<TradingService> logger)
    {
        _accountRepository = accountRepository;
        _orderRepository = orderRepository;
        _stockExchangeClient = stockExchangeClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<decimal> GetStockPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting stock price for {Symbol}", symbol);
        return await _stockExchangeClient.GetStockPriceAsync(symbol, cancellationToken);
    }

    public async Task<OrderResponse> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Placing order: {Quantity} shares of {Symbol} for account {AccountId}",
            request.Quantity, request.Symbol, request.AccountId);

        // Check for idempotency
        var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);

        if (existingOrder != null)
        {
            _logger.LogInformation("Order with idempotency key {IdempotencyKey} already exists", request.IdempotencyKey);
            return MapToOrderResponse(existingOrder);
        }

        // Get current stock price
        var stockPrice = await _stockExchangeClient.GetStockPriceAsync(request.Symbol, cancellationToken);
        var totalAmount = stockPrice * request.Quantity;

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Get account with positions
            var account = await _accountRepository.GetWithPositionsAsync(request.AccountId, cancellationToken);

            if (account == null)
            {
                throw new InvalidOperationException($"Account {request.AccountId} not found");
            }

            // Validate sufficient funds
            if (account.CashBalance < totalAmount)
            {
                throw new InvalidOperationException(
                    $"Insufficient funds. Required: €{totalAmount:N2}, Available: €{account.CashBalance:N2}");
            }

            // Create order entity
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                AccountId = request.AccountId,
                Symbol = request.Symbol,
                OrderType = OrderType.Buy,
                Quantity = request.Quantity,
                PricePerShare = stockPrice,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                IdempotencyKey = request.IdempotencyKey
            };

            await _orderRepository.AddAsync(order, cancellationToken);

            // Execute order on exchange
            try
            {
                var exchangeOrderId = await _stockExchangeClient.ExecuteOrderAsync(
                    request.Symbol,
                    request.Quantity,
                    cancellationToken);

                order.ExchangeOrderId = exchangeOrderId;
                order.Status = OrderStatus.Executed;
                order.ExecutedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute order on exchange");
                order.Status = OrderStatus.Failed;
                order.ErrorMessage = ex.Message;

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                throw;
            }

            // Update account balance
            account.CashBalance -= totalAmount;
            account.UpdatedAt = DateTime.UtcNow;

            // Update or create stock position
            var position = account.Positions.FirstOrDefault(p => p.Symbol == request.Symbol);

            if (position != null)
            {
                // Update existing position
                var totalShares = position.Quantity + request.Quantity;
                var totalCost = (position.Quantity * position.AveragePrice) + (request.Quantity * stockPrice);

                position.AveragePrice = totalCost / totalShares;
                position.Quantity = totalShares;
                position.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new position
                var newPosition = new StockPosition
                {
                    PositionId = Guid.NewGuid(),
                    AccountId = account.AccountId,
                    Symbol = request.Symbol,
                    Quantity = request.Quantity,
                    AveragePrice = stockPrice,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                account.Positions.Add(newPosition);
            }

            // Create transaction record
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                AccountId = account.AccountId,
                OrderId = order.OrderId,
                TransactionType = TransactionType.Debit,
                Amount = totalAmount,
                BalanceBefore = account.CashBalance + totalAmount,
                BalanceAfter = account.CashBalance,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.TransactionRepository.AddAsync(transaction, cancellationToken);

            // Save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} executed successfully", order.OrderId);

            return MapToOrderResponse(order);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to place order for account {AccountId}", request.AccountId);
            throw;
        }
    }

    public async Task<OrderResponse?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting order {OrderId}", orderId);

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        return order != null ? MapToOrderResponse(order) : null;
    }

    public async Task<List<OrderResponse>> GetOrdersAsync(
        Guid accountId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting orders for account {AccountId}, page {PageNumber}, size {PageSize}",
            accountId, pageNumber, pageSize);

        var orders = await _orderRepository.GetByAccountIdAsync(accountId, pageNumber, pageSize, cancellationToken);

        return orders.Select(MapToOrderResponse).ToList();
    }

    private static OrderResponse MapToOrderResponse(Order order)
    {
        return new OrderResponse
        {
            OrderId = order.OrderId,
            AccountId = order.AccountId,
            Symbol = order.Symbol,
            OrderType = order.OrderType.ToString(),
            Quantity = order.Quantity,
            PricePerShare = order.PricePerShare,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            ExecutedAt = order.ExecutedAt,
            ErrorMessage = order.ErrorMessage
        };
    }
}