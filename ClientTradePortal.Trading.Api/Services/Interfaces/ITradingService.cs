using ClientTradePortal.Trading.Api.Models.Requests;
using ClientTradePortal.Trading.Api.Models.Responses;

namespace ClientTradePortal.Trading.Api.Services.Interfaces;

public interface ITradingService
{
    Task<decimal> GetStockPriceAsync(string symbol, CancellationToken cancellationToken = default);
    Task<OrderResponse> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<List<OrderResponse>> GetOrdersAsync(Guid accountId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}