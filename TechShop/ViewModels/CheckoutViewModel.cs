using System.ComponentModel.DataAnnotations;
using TechShop.Models;

namespace TechShop.ViewModels;

public class CheckoutViewModel
{
    public ShoppingCart? Cart { get; set; }
    public User? User { get; set; }
    public string? UserId { get; set; }

    [Required(ErrorMessage = "Оберіть спосіб оплати")]
    public PaymentMethod PaymentMethod { get; set; }

    [Required(ErrorMessage = "Оберіть спосіб доставки")]
    public DeliveryMethod DeliveryMethod { get; set; }

    [Required(ErrorMessage = "Вкажіть ім'я отримувача")]
    [StringLength(100, ErrorMessage = "Ім'я не може бути довше 100 символів")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть телефон отримувача")]
    [Phone(ErrorMessage = "Невірний формат телефону")]
    public string RecipientPhone { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? NovaPoshtaBranch { get; set; }

    public string? CarrierName { get; set; }

    public string? DeliveryNotes { get; set; }

    public double DeliveryCost => CalculateDeliveryCost();
    
    [Display(Name = "Бажана дата доставки")]
    public DateTime? PreferredDeliveryDate { get; set; }
    
    public double TotalWithDelivery => Cart?.Items.Sum(i => i.Quantity * i.Product.Price) + DeliveryCost ?? 0;

    private double CalculateDeliveryCost()
    {
        return DeliveryMethod switch
        {
            DeliveryMethod.SelfPickup => 0,
            DeliveryMethod.NovaPoshta => string.IsNullOrEmpty(City) ? 80 : 
                City.Contains("київ", StringComparison.CurrentCultureIgnoreCase) ? 60 : 80,
            DeliveryMethod.Courier => string.IsNullOrEmpty(City) ? 150 : 
                City.Contains("київ", StringComparison.CurrentCultureIgnoreCase) ? 100 : 150,
            _ => 0
        };
    }
}