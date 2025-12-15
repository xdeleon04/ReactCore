using FluentValidation;
using ReactCore.Backend.Controllers.Admin;

namespace ReactCore.Backend.Validators;

public class AdminOrderStatusChangeRequestValidator : AbstractValidator<AdminOrderStatusChangeRequest>
{
    public AdminOrderStatusChangeRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(OrderStatusTransitions.IsKnownStatus)
            .WithMessage("Status must be one of: Pending, Processing, Shipped, Completed");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .Must(r => r is null || !string.IsNullOrWhiteSpace(r))
            .WithMessage("Reason must not be empty when provided");
    }
}

public static class OrderStatusTransitions
{
    private static readonly Dictionary<string, string[]> AllowedNext = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pending"] = new[] { "Processing" },
        ["Processing"] = new[] { "Shipped" },
        ["Shipped"] = new[] { "Completed" },
        ["Completed"] = Array.Empty<string>(),
    };

    public static bool IsKnownStatus(string? status)
        => !string.IsNullOrWhiteSpace(status) && AllowedNext.ContainsKey(status.Trim());

    public static string Normalize(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        var s = status.Trim();
        foreach (var key in AllowedNext.Keys)
        {
            if (string.Equals(key, s, StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return s;
    }

    public static IReadOnlyList<string> GetAllowedNext(string currentStatus)
    {
        var normalized = Normalize(currentStatus);
        return AllowedNext.TryGetValue(normalized, out var next) ? next : Array.Empty<string>();
    }

    public static bool CanTransition(string currentStatus, string nextStatus)
    {
        var from = Normalize(currentStatus);
        var to = Normalize(nextStatus);

        if (!AllowedNext.TryGetValue(from, out var allowed))
        {
            return false;
        }

        return allowed.Any(s => string.Equals(s, to, StringComparison.OrdinalIgnoreCase));
    }
}
