using System.ComponentModel.DataAnnotations;

namespace TechShop.Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        public User User { get; set; }
        public List<ShoppingCartItem> Items { get; set; } = [];
    }
}
