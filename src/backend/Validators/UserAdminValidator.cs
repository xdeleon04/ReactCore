using FluentValidation;
using ReactCore.Backend.Controllers.Admin;

namespace ReactCore.Backend.Validators;

public class AdminDeactivateRequestValidator : AbstractValidator<AdminDeactivateRequest>
{
    public AdminDeactivateRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .Must(r => r is null || !string.IsNullOrWhiteSpace(r))
            .WithMessage("Reason must not be empty when provided");
    }
}
