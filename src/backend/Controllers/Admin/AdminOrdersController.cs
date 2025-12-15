using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReactCore.Backend.Services.Admin;

namespace ReactCore.Backend.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "admin")]
[EnableRateLimiting("admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _orders;
    private readonly IValidator<AdminOrderStatusChangeRequest> _statusValidator;

    public AdminOrdersController(IAdminOrderService orders, IValidator<AdminOrderStatusChangeRequest> statusValidator)
    {
        _orders = orders;
        _statusValidator = statusValidator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? orderNumber,
        [FromQuery] string? email,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _orders.ListOrdersAsync(orderNumber, email, status, startDate, endDate, skip, take, cancellationToken);
        return Ok(new { items, total, skip = Math.Max(0, skip), take = Math.Clamp(take, 1, 100) });
    }

    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetDetail([FromRoute] int orderId, CancellationToken cancellationToken = default)
    {
        if (orderId < 1)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Validation failed",
                errors = new Dictionary<string, string[]> { ["id"] = new[] { "id must be >= 1" } }
            });
        }

        var order = await _orders.GetOrderDetailAsync(orderId, cancellationToken);
        if (order is null)
        {
            return NotFound(new { error = "Order not found", statusCode = 404 });
        }

        return Ok(order);
    }

    [HttpPatch("{orderId:int}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] int orderId, [FromBody] AdminOrderStatusChangeRequest request, CancellationToken cancellationToken = default)
    {
        if (orderId < 1)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Validation failed",
                errors = new Dictionary<string, string[]> { ["id"] = new[] { "id must be >= 1" } }
            });
        }

        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                error = "Validation failed",
                errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
                statusCode = 400
            });
        }

        var adminUserId = GetAdminUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (success, body, errorBody) = await _orders.UpdateStatusAsync(adminUserId, orderId, request.Status, request.Reason, ip, cancellationToken);
        if (!success)
        {
            if (errorBody is not null)
            {
                if (errorBody.GetType().GetProperty("statusCode")?.GetValue(errorBody) is int statusCode)
                {
                    if (statusCode == 404)
                    {
                        return NotFound(errorBody);
                    }
                }

                return BadRequest(errorBody);
            }

            return BadRequest(new { error = "Unable to update order status", statusCode = 400 });
        }

        return Ok(body);
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

public class AdminOrderStatusChangeRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
