using System.ComponentModel.DataAnnotations;

namespace ReactCore.Backend.Models;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public Product? Product { get; set; }
}
