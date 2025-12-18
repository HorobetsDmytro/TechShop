using Microsoft.EntityFrameworkCore;
using TechShop.Interfaces;
using TechShop.Models;

namespace TechShop.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public IEnumerable<Order> GetOrdersByUser(string userId)
    {
        return _dbSet.Where(o => o.UserId == userId).ToList();
    }

    public override void Add(Order order)
    {
        _context.Orders.Add(order);
        _context.SaveChanges();
    }

    public override IEnumerable<Order> GetAll()
    {
        return _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .ToList();
    }

    public override Order GetById(int id)
    {
        return _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .FirstOrDefault(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .ToListAsync();

        foreach (var order in orders)
        {
            if (order.OrderItems == null)
            {
                order.OrderItems = new List<OrderItem>();
            }
        }

        return orders;
    }

    public async Task<Order> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Product> GetProductAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}