using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ReactCore.Backend.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public ProductsController(IProductService productService, AppDbContext db, INotificationService notificationService)
    {
        _productService = productService;
        _db = db;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Lists products with optional filtering and pagination.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="minPrice">Optional minimum price filter.</param>
    /// <param name="maxPrice">Optional maximum price filter.</param>
    /// <param name="skip">Optional skip for pagination.</param>
    /// <param name="take">Optional take for pagination.</param>
    /// <param name="page">Optional 1-based page (used if skip not provided).</param>
    /// <param name="pageSize">Optional page size (used if take not provided).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? skip,
        [FromQuery] int? take,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedTake = take ?? pageSize ?? 20;
            var resolvedSkip = skip ?? (page.HasValue ? Math.Max(0, (page.Value - 1) * resolvedTake) : 0);

            var result = await _productService.GetProductsAsync(
                category,
                minPrice,
                maxPrice,
                resolvedSkip,
                resolvedTake,
                cancellationToken);
            return Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Invalid query parameters",
                errors = new Dictionary<string, string[]> { { ex.ParamName ?? "query", [ex.Message] } }
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Invalid query parameters",
                errors = new Dictionary<string, string[]> { { "query", [ex.Message] } }
            });
        }
    }

    /// <summary>
    /// Gets a product by id.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductById([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id, cancellationToken);
            if (product is null)
            {
                return NotFound(new { statusCode = 404, message = "Product not found" });
            }
            return Ok(product);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Invalid query parameters",
                errors = new Dictionary<string, string[]> { { ex.ParamName ?? "query", [ex.Message] } }
            });
        }
    }

    /// <summary>
    /// Gets related products for a product.
    /// </summary>
    /// <param name="id">Product id.</param>
    /// <param name="count">Maximum number of related products to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:int}/related")]
    public async Task<IActionResult> GetRelatedProducts(
        [FromRoute] int id,
        [FromQuery] int count = 4,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var related = await _productService.GetRelatedProductsAsync(id, count, cancellationToken);
            return Ok(related);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Invalid query parameters",
                errors = new Dictionary<string, string[]> { { ex.ParamName ?? "query", [ex.Message] } }
            });
        }
    }

    /// <summary>
    /// Gets inventory status for a product (rate limited).
    /// </summary>
    [HttpGet("{id:int}/inventory")]
    [EnableRateLimiting("inventory")]
    public async Task<IActionResult> GetInventoryStatus([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Invalid query parameters",
                errors = new Dictionary<string, string[]> { { "id", new[] { "id must be >= 1" } } }
            });
        }

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

        if (product is null)
        {
            return NotFound(new { statusCode = 404, message = "Product not found" });
        }

        var status = product.StockQuantity <= 0
            ? "out-of-stock"
            : product.StockQuantity <= product.ReorderLevel
                ? "low-stock"
                : "in-stock";

        return Ok(new InventoryStatusResponse(
            ProductId: product.Id,
            StockQuantity: product.StockQuantity,
            Status: status,
            ReorderLevel: product.ReorderLevel,
            LastUpdated: DateTime.UtcNow));
    }

    /// <summary>
    /// Subscribes the authenticated user to restock notifications for a product.
    /// </summary>
    [HttpPost("{id:int}/notify")]
    [Authorize]
    public async Task<IActionResult> SubscribeToRestock([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Invalid query parameters",
                errors = new Dictionary<string, string[]> { { "id", new[] { "id must be >= 1" } } }
            });
        }

        var userId = GetUserId();

        try
        {
            await _notificationService.SubscribeToRestockAsync(userId, id, cancellationToken);
            return StatusCode(201, new { success = true, productId = id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { statusCode = 404, message = "Product not found", productId = id });
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
