using System.ComponentModel.DataAnnotations;

namespace TechShop.Models;

public class Delivery
{
    public int Id { get; set; }
        
    [Required]
    public int OrderId { get; set; }
    public Order Order { get; set; }
        
    [Required]
    public DeliveryMethod Method { get; set; }
        
    [Required]
    public DeliveryStatus Status { get; set; }
        
    public decimal Cost { get; set; }
        
    [StringLength(100)]
    public string? RecipientName { get; set; }
        
    [StringLength(15)]
    public string? RecipientPhone { get; set; }
        
    [StringLength(200)]
    public string? Address { get; set; }
        
    [StringLength(50)]
    public string? City { get; set; }
    
    [Display(Name = "Бажана дата доставки")]
    public DateTime? PreferredDeliveryDate { get; set; }
        
    [StringLength(100)]
    public string? NovaPoshtaBranch { get; set; }
    public string? NovaPoshtaTrackingNumber { get; set; }
        
    [StringLength(100)]
    public string? CarrierName { get; set; }
    public string? CarrierTrackingNumber { get; set; }
        
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
        
    public string? Notes { get; set; }
}

public enum DeliveryMethod
{
    SelfPickup = 0,
    NovaPoshta = 1,
    Courier = 2
}

public enum DeliveryStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}