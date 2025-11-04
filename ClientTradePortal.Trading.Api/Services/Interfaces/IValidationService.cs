using ClientTradePortal.Trading.Api.Models.Requests;
using ClientTradePortal.Trading.Api.Models.Responses;

namespace ClientTradePortal.Trading.Api.Services.Interfaces;

public interface IValidationService
{
    Task<ValidationResponse> ValidateOrderAsync(ValidationRequest request, CancellationToken cancellationToken = default);
}