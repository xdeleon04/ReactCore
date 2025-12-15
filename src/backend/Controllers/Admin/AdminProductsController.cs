using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Services.Admin;

namespace ReactCore.Backend.Controllers.Admin;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "admin")]
[EnableRateLimiting("admin")]
public class AdminProductsController : ControllerBase
{
    private readonly IAdminProductService _adminProducts;
    private readonly IValidator<AdminProductUpsertRequest> _upsertValidator;

    public AdminProductsController(IAdminProductService adminProducts, IValidator<AdminProductUpsertRequest> upsertValidator)
    {
        _adminProducts = adminProducts;
        _upsertValidator = upsertValidator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? isDeleted,
        [FromQuery] bool? lowStockOnly,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var includeArchived = isDeleted == true;

        var (items, total) = await _adminProducts.ListProductsAsync(search, category, includeArchived, lowStockOnly, skip, take, cancellationToken);
        return Ok(new { items, total, skip = Math.Max(0, skip), take = Math.Clamp(take, 1, 100) });
    }

    [HttpGet("{productId:int}")]
    public async Task<IActionResult> GetDetail([FromRoute] int productId, CancellationToken cancellationToken = default)
    {
        if (productId < 1)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Validation failed",
                errors = new Dictionary<string, string[]> { ["id"] = new[] { "id must be >= 1" } }
            });
        }

        var product = await _adminProducts.GetProductDetailAsync(productId, cancellationToken);
        if (product is null)
        {
            return NotFound(new { error = "Product not found", statusCode = 404 });
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminProductUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _upsertValidator.ValidateAsync(request, cancellationToken);
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

        var (success, _, body, errorBody) = await _adminProducts.CreateAsync(adminUserId, request, ip, cancellationToken);
        if (!success)
        {
            return BadRequest(errorBody ?? new { error = "Unable to create product", statusCode = 400 });
        }

        return StatusCode(201, body);
    }

    [HttpPut("{productId:int}")]
    public async Task<IActionResult> Update([FromRoute] int productId, [FromBody] AdminProductUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (productId < 1)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Validation failed",
                errors = new Dictionary<string, string[]> { ["id"] = new[] { "id must be >= 1" } }
            });
        }

        var validation = await _upsertValidator.ValidateAsync(request, cancellationToken);
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

        var (success, body, errorBody) = await _adminProducts.UpdateAsync(adminUserId, productId, request, ip, cancellationToken);
        if (!success)
        {
            if (errorBody is not null)
            {
                return NotFound(errorBody);
            }

            return BadRequest(new { error = "Unable to update product", statusCode = 400 });
        }

        return Ok(body);
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Delete([FromRoute] int productId, CancellationToken cancellationToken = default)
    {
        if (productId < 1)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Validation failed",
                errors = new Dictionary<string, string[]> { ["id"] = new[] { "id must be >= 1" } }
            });
        }

        var adminUserId = GetAdminUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (success, errorBody) = await _adminProducts.SoftDeleteAsync(adminUserId, productId, ip, cancellationToken);
        if (!success)
        {
            return NotFound(errorBody ?? new { error = "Product not found", statusCode = 404 });
        }

        return NoContent();
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
