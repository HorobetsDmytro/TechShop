using TechShop.Models;

namespace TechShop.Services
{
    public interface IEmailService
    {
        Task SendOrderStatusUpdateEmailAsync(Order order);
        Task SendDeliveryStatusUpdateEmailAsync(Order order);
    }
}
