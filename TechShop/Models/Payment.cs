using System.ComponentModel.DataAnnotations;

namespace TechShop.Models;

public class Payment
{
    public int Id { get; set; }
        
    [Required]
    public int OrderId { get; set; }
    public Order Order { get; set; }
        
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
        
    [Required]
    public PaymentStatus Status { get; set; }
        
    public double Amount { get; set; }
        
    public string? LiqPayOrderId { get; set; }
    public string? LiqPayPaymentId { get; set; }
    public string? LiqPayStatus { get; set; }
        
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
        
    public string? TransactionDetails { get; set; }
}

public enum PaymentMethod
{
    Cash = 0,
    Card = 1
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Success = 2,
    Failed = 3, 
    Cancelled = 4
}