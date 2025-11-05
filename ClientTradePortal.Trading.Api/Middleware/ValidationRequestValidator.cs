namespace ClientTradePortal.Trading.Api.Validators;

public class ValidationRequestValidator : AbstractValidator<ValidationRequest>
{
    public ValidationRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Symbol)
            .NotEmpty()
            .WithMessage("Stock symbol is required")
            .MaximumLength(10)
            .WithMessage("Symbol cannot exceed 10 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.EstimatedPrice)
            .GreaterThan(0)
            .WithMessage("Estimated price must be greater than zero");
    }
}