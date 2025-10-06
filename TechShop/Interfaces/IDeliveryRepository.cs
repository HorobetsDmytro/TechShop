using TechShop.Models;

namespace TechShop.Interfaces;

public interface IDeliveryRepository : IRepository<Delivery>
{
    Delivery? GetByOrderId(int orderId);
    Task<Delivery?> GetByOrderIdAsync(int orderId);
    Task<bool> UpdateDeliveryStatusAsync(int orderId, DeliveryStatus status);
    IEnumerable<Delivery> GetPendingDeliveries();
}