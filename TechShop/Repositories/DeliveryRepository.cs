using Microsoft.EntityFrameworkCore;
using TechShop.Interfaces;
using TechShop.Models;

namespace TechShop.Repositories;

public class DeliveryRepository : GenericRepository<Delivery>, IDeliveryRepository
{
    public DeliveryRepository(ApplicationDbContext context) : base(context) { }

    public Delivery? GetByOrderId(int orderId)
    {
        return _dbSet.Include(d => d.Order).FirstOrDefault(d => d.OrderId == orderId);
    }

    public async Task<Delivery?> GetByOrderIdAsync(int orderId)
    {
        return await _dbSet.Include(d => d.Order).FirstOrDefaultAsync(d => d.OrderId == orderId);
    }

    public async Task<bool> UpdateDeliveryStatusAsync(int orderId, DeliveryStatus status)
    {
        var delivery = await GetByOrderIdAsync(orderId);
        if (delivery == null) return false;

        delivery.Status = status;
        if (status == DeliveryStatus.Delivered)
        {
            delivery.DeliveredAt = DateTime.Now;
        }

        _context.Entry(delivery).State = EntityState.Modified;
        return await _context.SaveChangesAsync() > 0;
    }

    public IEnumerable<Delivery> GetPendingDeliveries()
    {
        return _dbSet.Include(d => d.Order)
            .Where(d => d.Status == DeliveryStatus.Pending || d.Status == DeliveryStatus.Processing)
            .ToList();
    }
}