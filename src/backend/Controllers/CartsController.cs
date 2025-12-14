using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Services;
using System.Security.Claims;

namespace ReactCore.Backend.Controllers;

[ApiController]
[Route("api/carts")]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Gets the authenticated user's current cart.
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var cart = await _cartService.GetCartAsync(userId, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Adds an item to the authenticated user's cart.
    /// </summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        try
        {
            var response = await _cartService.AddItemAsync(userId, request, cancellationToken);
            return StatusCode(201, response);
        }
        catch (ProductNotFoundException ex)
        {
            return NotFound(new { statusCode = 404, message = ex.Message, productId = ex.ProductId });
        }
        catch (InsufficientInventoryException ex)
        {
            return Conflict(new
            {
                statusCode = 409,
                message = ex.Message,
                productId = ex.ProductId,
                requestedQuantity = ex.RequestedQuantity,
                availableQuantity = ex.AvailableQuantity
            });
        }
    }

    /// <summary>
    /// Updates the quantity for a cart item in the authenticated user's cart.
    /// </summary>
    [HttpPut("items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(
        [FromRoute] int itemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        try
        {
            var response = await _cartService.UpdateItemAsync(userId, itemId, request, cancellationToken);
            return Ok(response);
        }
        catch (CartItemNotFoundException ex)
        {
            return NotFound(new { statusCode = 404, message = ex.Message, itemId = ex.ItemId });
        }
        catch (ProductNotFoundException ex)
        {
            return NotFound(new { statusCode = 404, message = ex.Message, productId = ex.ProductId });
        }
        catch (InsufficientInventoryException ex)
        {
            return Conflict(new
            {
                statusCode = 409,
                message = ex.Message,
                productId = ex.ProductId,
                requestedQuantity = ex.RequestedQuantity,
                availableQuantity = ex.AvailableQuantity
            });
        }
    }

    /// <summary>
    /// Removes an item from the authenticated user's cart.
    /// </summary>
    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem([FromRoute] int itemId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        try
        {
            await _cartService.RemoveItemAsync(userId, itemId, cancellationToken);
            return NoContent();
        }
        catch (CartItemNotFoundException ex)
        {
            return NotFound(new { statusCode = 404, message = ex.Message, itemId = ex.ItemId });
        }
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
