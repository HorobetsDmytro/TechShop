using TechShop.Models;

namespace TechShop.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Payment? GetByOrderId(int orderId);
    Task<Payment?> GetByOrderIdAsync(int orderId);
    Task<bool> UpdatePaymentStatusAsync(int orderId, PaymentStatus status, string? details = null);
}