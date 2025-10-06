using System.ComponentModel.DataAnnotations;

namespace TechShop.Models;

public class Category
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
    public string Name { get; set; }
}