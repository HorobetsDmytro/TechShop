using System.ComponentModel.DataAnnotations;

namespace TechShop.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Текст коментаря обов'язковий")]
        public string Text { get; set; }

        public DateTime CreatedAt { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }
    }
}
