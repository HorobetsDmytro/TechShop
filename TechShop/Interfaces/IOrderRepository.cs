using TechShop.Models;

namespace TechShop.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    IEnumerable<Order> GetOrdersByUser(string userId);
    Task<IEnumerable<Order>> GetAllAsync();
    Task<Order> GetByIdAsync(int id);
    Task UpdateAsync(Order order);
    Task DeleteAsync(int id);
    Task<Product> GetProductAsync(int id);
}
