using FluentValidation;
using ClientTradePortal.Trading.Api.Models.Requests;

namespace ClientTradePortal.Trading.Api.Validators;

public class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Symbol)
            .NotEmpty()
            .WithMessage("Stock symbol is required")
            .MaximumLength(10)
            .WithMessage("Symbol cannot exceed 10 characters")
            .Matches("^[A-Z]+$")
            .WithMessage("Symbol must contain only uppercase letters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero")
            .LessThanOrEqualTo(10000)
            .WithMessage("Quantity cannot exceed 10,000 shares");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required");
    }
}