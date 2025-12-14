using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Services;
using System.Security.Claims;

namespace ReactCore.Backend.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Creates an order from the authenticated user's cart.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        try
        {
            var order = await _orderService.CreateOrderAsync(userId, request, cancellationToken);
            return StatusCode(201, order);
        }
        catch (InvalidCartException)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Validation failed",
                errors = new Dictionary<string, string[]> { ["cartId"] = new[] { "Cart does not belong to authenticated user" } }
            });
        }
        catch (EmptyCartException)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Validation failed",
                errors = new Dictionary<string, string[]> { ["cartId"] = new[] { "Cart must contain at least one item" } }
            });
        }
        catch (InventoryConflictException ex)
        {
            return Conflict(new
            {
                statusCode = 409,
                message = ex.Message,
                conflicts = ex.Conflicts
            });
        }
    }

    /// <summary>
    /// Gets an order by order number (scoped to the authenticated user).
    /// </summary>
    [HttpGet("{orderNumber}")]
    public async Task<IActionResult> GetByOrderNumber([FromRoute] string orderNumber, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var order = await _orderService.GetOrderAsync(userId, orderNumber, cancellationToken);

        if (order is null)
        {
            return NotFound(new { statusCode = 404, message = "Order not found", orderNumber });
        }

        return Ok(order);
    }

    /// <summary>
    /// Lists orders for the authenticated user with optional status filter and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);

        var orders = await _orderService.GetUserOrdersAsync(userId, status, skip, take, cancellationToken);
        return Ok(orders);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user id claim");
        }
        return userId;
    }
}
