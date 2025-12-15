using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Services.Admin;

namespace ReactCore.Backend.Controllers.Admin;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "admin")]
[EnableRateLimiting("admin")]
public class AdminReportsController : ControllerBase
{
    private readonly IAdminReportService _reports;
    private readonly IValidator<ReportDateRangeRequest> _validator;

    public AdminReportsController(IAdminReportService reports, IValidator<ReportDateRangeRequest> validator)
    {
        _reports = reports;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var request = new ReportDateRangeRequest { StartDate = startDate, EndDate = endDate };
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                error = "Invalid date range",
                message = validation.Errors.First().ErrorMessage,
                statusCode = 400
            });
        }

        var startUtc = NormalizeToUtc(startDate!.Value);
        var endUtc = NormalizeToUtc(endDate!.Value);

        var report = await _reports.GetSalesReportAsync(startUtc, endUtc, cancellationToken);
        return Ok(report);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var request = new ReportDateRangeRequest { StartDate = startDate, EndDate = endDate };
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                error = "Invalid date range",
                message = validation.Errors.First().ErrorMessage,
                statusCode = 400
            });
        }

        var startUtc = NormalizeToUtc(startDate!.Value);
        var endUtc = NormalizeToUtc(endDate!.Value);

        var (csvBytes, fileName) = await _reports.ExportSalesReportCsvAsync(startUtc, endUtc, cancellationToken);
        return File(csvBytes, "text/csv", fileName);
    }

    private static DateTime NormalizeToUtc(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        return dt.ToUniversalTime();
    }

    private Guid GetAdminUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user id claim");
        }
        return userId;
    }
}
