using TechShop.Interfaces;
using TechShop.Models;
using Microsoft.EntityFrameworkCore;

namespace TechShop.Repositories;

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context) { }

    public Payment? GetByOrderId(int orderId)
    {
        return _dbSet.Include(p => p.Order).FirstOrDefault(p => p.OrderId == orderId);
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId)
    {
        return await _dbSet.Include(p => p.Order).FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<bool> UpdatePaymentStatusAsync(int orderId, PaymentStatus status, string? details = null)
    {
        var payment = await GetByOrderIdAsync(orderId);
        if (payment == null) return false;

        payment.Status = status;
        payment.ProcessedAt = DateTime.Now;
        if (!string.IsNullOrEmpty(details))
        {
            payment.TransactionDetails = details;
        }

        _context.Entry(payment).State = EntityState.Modified;
        return await _context.SaveChangesAsync() > 0;
    }
}