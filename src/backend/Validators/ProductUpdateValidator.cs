using FluentValidation;
using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Validators;

public class AdminProductUpsertRequestValidator : AbstractValidator<AdminProductUpsertRequest>
{
    public AdminProductUpsertRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ReorderLevel.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => x.Description is not null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .When(x => x.ImageUrl is not null);
    }
}
