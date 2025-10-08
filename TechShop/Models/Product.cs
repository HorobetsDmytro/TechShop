using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Назва товару обов'язкова")]
    public string Name { get; set; }

    public string Description { get; set; }

    [Required(ErrorMessage = "Ціна обов'язкова")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Ціна повинна бути більше 0")]
    public double Price { get; set; }

    [Required(ErrorMessage = "Кількість на складі обов'язкова")]
    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Категорія обов'язкова")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Час виготовлення обов'язковий")]
    [Range(1, 365, ErrorMessage = "Час виготовлення повинен бути від 1 до 365 днів")]
    [Display(Name = "Час виготовлення (днів)")]
    public int ProductionTimeDays { get; set; } = 3;
}
