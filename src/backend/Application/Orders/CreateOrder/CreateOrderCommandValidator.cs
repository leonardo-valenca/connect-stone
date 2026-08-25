using FluentValidation;

namespace Application.Orders.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AmountInCents).GreaterThan(0);
    }
}
