using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public double TotalAmount { get; set; }
        public int StatusId { get; set; }

        [NotMapped]
        public OrderStatus Status
        {
            get { return (OrderStatus)StatusId; }
            set { StatusId = (int)value; }
        }

        public string UserId { get; set; }
        public User User { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        
        public Payment? Payment { get; set; }
        public Delivery? Delivery { get; set; }
        public decimal TotalWithDelivery => (decimal)TotalAmount + (Delivery?.Cost ?? 0);
    }

    public enum OrderStatus
    {
        [Display(Name = "Нове")]
        New = 0,
        
        [Display(Name = "В обробці")]
        Processing = 1,
        
        [Display(Name = "Завершене")]
        Completed = 2
    }
}