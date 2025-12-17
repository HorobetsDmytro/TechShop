using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Ціна повинна бути більше 0")]
    public double Price { get; set; }

    [Required]
    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    [Required]
    public int CategoryId { get; set; }
}
