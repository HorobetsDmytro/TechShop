using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop.Models
{
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

        [Required(ErrorMessage = "Колір обов'язковий")]
        public string Color { get; set; }

        [Required(ErrorMessage = "Ширина обов'язкова")]
        [Range(1, 1000, ErrorMessage = "Ширина повинна бути від 1 до 1000 см")]
        public int Width { get; set; }

        [Required(ErrorMessage = "Висота обов'язкова")]
        [Range(1, 1000, ErrorMessage = "Висота повинна бути від 1 до 1000 см")]
        public int Height { get; set; }

        [Required(ErrorMessage = "Кількість штук в упаковці обов'язкова")]
        [Range(1, 1000, ErrorMessage = "Кількість штук повинна бути від 1 до 1000")]
        public int PiecesPerPackage { get; set; }

        public int? Volume { get; set; }

        [NotMapped]
        public bool IsGarbageBag { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [Required]
        public int Density { get; set; }

        [Required(ErrorMessage = "Час виготовлення обов'язковий")]
        [Range(1, 365, ErrorMessage = "Час виготовлення повинен бути від 1 до 365 днів")]
        [Display(Name = "Час виготовлення (днів)")]
        public int ProductionTimeDays { get; set; } = 3;
    }

    public static class ProductConstants
    {
        public const string GarbageBagsCategoryName = "Пакети для сміття";
    }
}