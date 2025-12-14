using FluentValidation;
using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.CartId).GreaterThan(0);
    }
}
