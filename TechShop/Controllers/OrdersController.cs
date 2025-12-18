using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechShop.Interfaces;
using TechShop.Models;
using TechShop.Services;

namespace TechShop.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILiqPayService _liqPayService;

        public OrdersController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _orderRepository.GetByIdAsync(id);

            if (order.UserId != userId)
            {
                return NotFound();
            }

            return View(order);
        }
        
        [HttpPost]
        public async Task<IActionResult> VerifyPayment(int orderId)
        {
            var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
            if (payment == null) return RedirectToAction("Index");

            if (payment.Status == PaymentStatus.Success)
            {
                return RedirectToAction("Index");
            }

            var liqPayData = await _liqPayService.GetStatusAsync(orderId.ToString());

            if (liqPayData.TryGetValue("status", out var statusObj))
            {
                var status = statusObj.ToString();

                if (status is "success" or "sandbox" or "wait_accept" or "processing")
                {
                    await _paymentRepository.UpdatePaymentStatusAsync(orderId, PaymentStatus.Success, 
                        System.Text.Json.JsonSerializer.Serialize(liqPayData));
                    
                    TempData["Message"] = "Статус оплати успішно оновлено!";
                    TempData["MessageType"] = "success";
                }
                else
                {
                    TempData["Message"] = $"Оплату не знайдено або помилка. Статус LiqPay: {status}";
                    TempData["MessageType"] = "error";
                }
            }

            return RedirectToAction("Index");
        }
    }
}