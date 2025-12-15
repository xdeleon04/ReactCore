using FluentValidation;
using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Validators;

public class ReportDateRangeRequestValidator : AbstractValidator<ReportDateRangeRequest>
{
    private const int MaxRangeDays = 365;

    public ReportDateRangeRequestValidator()
    {
        RuleFor(x => x.StartDate)
            .NotNull()
            .WithMessage("startDate is required");

        RuleFor(x => x.EndDate)
            .NotNull()
            .WithMessage("endDate is required");

        RuleFor(x => x)
            .Must(x => x.StartDate.HasValue && x.EndDate.HasValue && x.EndDate.Value > x.StartDate.Value)
            .WithMessage("End date must be after start date")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x)
            .Must(x =>
            {
                if (!x.StartDate.HasValue || !x.EndDate.HasValue)
                {
                    return true;
                }

                var range = x.EndDate.Value - x.StartDate.Value;
                return range <= TimeSpan.FromDays(MaxRangeDays);
            })
            .WithMessage($"Date range cannot exceed {MaxRangeDays} days")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
