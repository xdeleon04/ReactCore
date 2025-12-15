using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReactCore.Backend.Services.Admin;

namespace ReactCore.Backend.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "admin")]
[EnableRateLimiting("admin")]
public class AdminAuditLogsController : ControllerBase
{
    private readonly IAdminAuditLogService _auditLogs;

    public AdminAuditLogsController(IAdminAuditLogService auditLogs)
    {
        _auditLogs = auditLogs;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value.ToUniversalTime() > endDate.Value.ToUniversalTime())
        {
            return BadRequest(new { error = "Validation failed", statusCode = 400, errors = new { dateRange = new[] { "startDate must be <= endDate" } } });
        }

        var (items, total) = await _auditLogs.ListAsync(startDate, endDate, skip, take, cancellationToken);
        return Ok(new { items, total, skip = Math.Max(0, skip), take = Math.Clamp(take, 1, 100) });
    }

    [HttpGet("{auditLogId:int}")]
    public async Task<IActionResult> GetDetail([FromRoute] int auditLogId, CancellationToken cancellationToken = default)
    {
        if (auditLogId < 1)
        {
            return BadRequest(new { error = "Validation failed", statusCode = 400, errors = new Dictionary<string, string[]> { ["id"] = new[] { "id must be >= 1" } } });
        }

        var item = await _auditLogs.GetDetailAsync(auditLogId, cancellationToken);
        if (item is null)
        {
            return NotFound(new { error = "Audit log not found", statusCode = 404 });
        }

        return Ok(item);
    }
}
