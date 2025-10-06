using TechShop.Models;

namespace TechShop.Interfaces;

public interface IOrderItemRepository : IRepository<OrderItem>
{
    IEnumerable<OrderItem> GetOrderItemsByOrder(int orderId);
}
