using TechShop.Interfaces;
using TechShop.Models;

namespace TechShop.Repositories;

public class OrderItemRepository : GenericRepository<OrderItem>, IOrderItemRepository
{
    public OrderItemRepository(ApplicationDbContext context) : base(context) { }

    public IEnumerable<OrderItem> GetOrderItemsByOrder(int orderId)
    {
        return _dbSet.Where(oi => oi.OrderId == orderId).ToList();
    }
}
