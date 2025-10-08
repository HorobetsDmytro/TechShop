using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TechShop.Models;
using Microsoft.EntityFrameworkCore;

namespace TechShop.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly ApplicationDbContext _context;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            ApplicationDbContext context)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _context = context;
        }

        public async Task SendOrderStatusUpdateEmailAsync(Order order)
        {
            try
            {
                var orderWithDetails = await GetOrderWithDetailsAsync(order.Id);
                if (orderWithDetails?.User == null)
                {
                    _logger.LogError("Cannot find order details or user for order {OrderId}", order.Id);
                    return;
                }

                var emailBody = CreateOrderStatusEmailBody(orderWithDetails);
                await SendEmailAsync(orderWithDetails.User.Email, "Оновлення статусу замовлення", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending status update email: {ExMessage}", ex.Message);
                throw;
            }
        }

        private async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Delivery)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        private static string CreateOrderStatusEmailBody(Order order)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2>Оновлення статусу замовлення</h2>
                    <p>Шановний {order.User.FirstName} {order.User.LastName},</p>
                    <p>Статус вашого замовлення №{order.Id} було оновлено до {order.Status}.</p>
                    <div style='margin: 20px 0; padding: 15px; background-color: #f8f9fa;'>
                        <h3 style='margin-top: 0;'>Деталі замовлення:</h3>
                        <p>Дата замовлення: {order.CreatedAt:dd.MM.yyyy HH:mm}</p>
                        <p>Загальна сума: {order.TotalAmount:C}</p>
                        <p>Поточний статус: <strong>{order.Status}</strong></p>
                    </div>
                    <p>З повагою,<br>Команда магазину</p>
                </div>
            </body>
            </html>";
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrEmpty(toEmail))
            {
                _logger.LogError("Cannot send email: recipient email is null or empty");
                throw new ArgumentException("Recipient email is required");
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName ?? "Shop", _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress(toEmail, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send email to {ToEmail}: {ExMessage}", toEmail, ex.Message);
                throw;
            }
        }
        
        public async Task SendDeliveryStatusUpdateEmailAsync(Order order)
        {
            try
            {
                var orderWithDetails = await GetOrderWithDetailsAsync(order.Id);
                if (orderWithDetails?.User == null || orderWithDetails.Delivery == null)
                {
                    _logger.LogError("Cannot find order details, user, or delivery info for order {OrderId}", order.Id);
                    return;
                }

                var emailBody = CreateDeliveryStatusEmailBody(orderWithDetails);
                await SendEmailAsync(orderWithDetails.User.Email, "Оновлення статусу доставки", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending delivery status update email: {ex.Message}");
                throw;
            }
        }

        private static string CreateDeliveryStatusEmailBody(Order order)
        {
            var deliveryStatusName = GetDeliveryStatusName(order.Delivery.Status);
            var deliveryMethodName = GetDeliveryMethodName(order.Delivery.Method);

            return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2>Оновлення статусу доставки</h2>
                    <p>Шановний {order.User.FirstName} {order.User.LastName},</p>
                    <p>Статус доставки вашого замовлення №{order.Id} було оновлено.</p>
                    
                    <div style='margin: 20px 0; padding: 15px; background-color: #f8f9fa; border-radius: 5px;'>
                        <h3 style='margin-top: 0; color: #495057;'>Інформація про доставку:</h3>
                        <p><strong>Спосіб доставки:</strong> {deliveryMethodName}</p>
                        <p><strong>Поточний статус:</strong> <span style='color: #28a745; font-weight: bold;'>{deliveryStatusName}</span></p>
                        {(order.Delivery.Status == DeliveryStatus.Delivered && order.Delivery.DeliveredAt.HasValue ? 
                            $"<p><strong>Дата доставки:</strong> {order.Delivery.DeliveredAt:dd.MM.yyyy HH:mm}</p>" : "")}
                        {(!string.IsNullOrEmpty(order.Delivery.CarrierTrackingNumber) ? 
                            $"<p><strong>Номер для відстеження:</strong> {order.Delivery.CarrierTrackingNumber}</p>" : "")}
                        {(!string.IsNullOrEmpty(order.Delivery.NovaPoshtaTrackingNumber) ? 
                            $"<p><strong>ТТН Нова Пошта:</strong> {order.Delivery.NovaPoshtaTrackingNumber}</p>" : "")}
                    </div>

                    <div style='margin: 20px 0; padding: 15px; background-color: #e9ecef; border-radius: 5px;'>
                        <h3 style='margin-top: 0; color: #495057;'>Деталі замовлення:</h3>
                        <p><strong>Номер замовлення:</strong> #{order.Id}</p>
                        <p><strong>Дата замовлення:</strong> {order.CreatedAt:dd.MM.yyyy HH:mm}</p>
                        <p><strong>Загальна сума:</strong> {order.TotalAmount:C}</p>
                    </div>

                    {(order.Delivery.Status == DeliveryStatus.Delivered ? 
                        "<p style='color: #28a745; font-weight: bold;'>🎉 Ваше замовлення успішно доставлено! Дякуємо за покупку!</p>" :
                        "<p>Ми повідомимо вас про подальші зміни статусу доставки.</p>")}
                    
                    <p>З повагою,<br>Команда магазину</p>
                </div>
            </body>
            </html>";
        }

        private static string GetDeliveryStatusName(DeliveryStatus status)
        {
            return status switch
            {
                DeliveryStatus.Pending => "Очікує відправки",
                DeliveryStatus.Processing => "Обробляється",
                DeliveryStatus.Shipped => "Відправлено",
                DeliveryStatus.Delivered => "Доставлено",
                DeliveryStatus.Cancelled => "Скасовано",
                _ => "Невідомий статус"
            };
        }

        private static string GetDeliveryMethodName(DeliveryMethod method)
        {
            return method switch
            {
                DeliveryMethod.SelfPickup => "Самовивіз",
                DeliveryMethod.NovaPoshta => "Нова Пошта",
                DeliveryMethod.Courier => "Кур'єрська доставка",
                _ => "Невідомо"
            };
        }
    }

    public class EmailSettings
    {
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}