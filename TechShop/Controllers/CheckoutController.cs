using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TechShop.Interfaces;
using TechShop.Models;
using TechShop.Services;
using System.Security.Claims;
using TechShop.ViewModels;

namespace TechShop.Controllers;

[Authorize]
public class CheckoutController(
    IShoppingCartRepository cartRepository,
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IDeliveryRepository deliveryRepository,
    IProductRepository productRepository,
    ILiqPayService liqPayService,
    ILogger<CheckoutController> logger)
    : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cart = cartRepository.GetCartByUserId(userId);

        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "ShoppingCart");
        }

        var model = new CheckoutViewModel
        {
            Cart = cart,
            UserId = userId,
            PaymentMethod = PaymentMethod.Card,
            DeliveryMethod = DeliveryMethod.SelfPickup
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ProcessOrder(CheckoutViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        model.UserId = userId;
        model.Cart = cartRepository.GetCartByUserId(userId);
        
        if (model.Cart == null || !model.Cart.Items.Any())
        {
            return RedirectToAction("Index", "ShoppingCart");
        }

        var stockIssues = new List<string>();
        foreach (var item in model.Cart.Items)
        {
            var product = productRepository.GetById(item.ProductId);

            if (product.StockQuantity >= item.Quantity) continue;
            stockIssues.Add(product.StockQuantity == 0
                ? $"Товар '{product.Name}' закінчився на складі"
                : $"Товар '{product.Name}': запитано {item.Quantity} шт., але в наявності лише {product.StockQuantity} шт.");
        }

        if (stockIssues.Count != 0)
        {
            var errorMessage = "Виявлено проблеми з наявністю товарів: " + string.Join("; ", stockIssues);
            ModelState.AddModelError("", errorMessage);
            return View("Index", model);
        }

        if (model.DeliveryMethod != DeliveryMethod.SelfPickup)
        {
            if (string.IsNullOrWhiteSpace(model.City))
            {
                ModelState.AddModelError(nameof(model.City), "Місто є обов'язковим для доставки");
            }

            switch (model.DeliveryMethod)
            {
                case DeliveryMethod.NovaPoshta when string.IsNullOrWhiteSpace(model.NovaPoshtaBranch):
                    ModelState.AddModelError(nameof(model.NovaPoshtaBranch), "Вкажіть номер відділення Нової Пошти");
                    break;
                case DeliveryMethod.Courier:
                {
                    if (string.IsNullOrWhiteSpace(model.Address))
                    {
                        ModelState.AddModelError(nameof(model.Address), "Адреса є обов'язковою для кур'єрської доставки");
                    }

                    if (!model.PreferredDeliveryDate.HasValue)
                    {
                        ModelState.AddModelError(nameof(model.PreferredDeliveryDate), "Вкажіть бажану дату доставки");
                    }
                    else if (model.PreferredDeliveryDate.Value.Date <= DateTime.Now.AddDays(1).Date)
                    {
                        ModelState.AddModelError(nameof(model.PreferredDeliveryDate), "Доставка можлива не раніше ніж через 2 дні від замовлення");
                    }

                    break;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            var order = await CreateOrderAsync(model);
            
            if (model.PaymentMethod == PaymentMethod.Cash)
            {
                return RedirectToAction("Success", new { orderId = order.Id });
            }
            
            return RedirectToAction("PayWithCard", new { orderId = order.Id });
        }
        catch (Exception ex)
        {
            logger.LogError("Error processing order: {ExMessage}", ex.Message);
            ModelState.AddModelError("", "Виникла помилка при обробці замовлення. Спробуйте пізніше.");
            
            return View("Index", model);
        }
    }
    
    [HttpGet]
    public IActionResult PayWithCard(int orderId)
    {
        try
        {
            var order = orderRepository.GetById(orderId);

            logger.LogInformation("Processing payment for order {OrderId}, amount: {OrderTotalWithDelivery}", orderId, order.TotalWithDelivery);

            var returnUrl = Url.Action("PaymentResult", "Checkout", new { orderId = orderId }, Request.Scheme);
            var callbackUrl = Url.Action("PaymentCallback", "Checkout", null, Request.Scheme);

            logger.LogInformation("Return URL: {ReturnUrl}", returnUrl);
            logger.LogInformation("Callback URL: {CallbackUrl}", callbackUrl);

            var paymentForm = liqPayService.GeneratePaymentForm(order, returnUrl, callbackUrl);
        
            ViewBag.PaymentForm = paymentForm;
            ViewBag.Order = order;
        
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing payment for order {OrderId}", orderId);
            return RedirectToAction("Failed", new { orderId });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback()
    {
        try
        {
            var data = Request.Form["data"];
            var signature = Request.Form["signature"];

            if (!liqPayService.VerifyCallback(data, signature))
            {
                logger.LogWarning("Invalid LiqPay callback signature");
                return BadRequest("Invalid signature");
            }

            var callbackData = liqPayService.ParseCallbackData(data);
            
            if (callbackData.TryGetValue("order_id", out var orderIdObj) && 
                int.TryParse(orderIdObj.ToString(), out var orderId))
            {
                var status = callbackData.GetValueOrDefault("status")?.ToString();
                var paymentStatus = status switch
                {
                    "success" => PaymentStatus.Success,
                    "failure" => PaymentStatus.Failed,
                    "processing" => PaymentStatus.Processing,
                    _ => PaymentStatus.Failed
                };

                await paymentRepository.UpdatePaymentStatusAsync(orderId, paymentStatus, 
                    System.Text.Json.JsonSerializer.Serialize(callbackData));
            }

            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError($"Error processing payment callback: {ex.Message}");
            return StatusCode(500);
        }
    }

    [HttpGet]
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentResult(int orderId)
    {
        if (orderId == 0 && Request.Query.ContainsKey("orderId"))
        {
            int.TryParse(Request.Query["orderId"], out orderId);
        }

        var payment = await paymentRepository.GetByOrderIdAsync(orderId);
    
        if (payment == null)
        {
            return RedirectToAction("Failed", new { orderId });
        }

        if (payment.Status == PaymentStatus.Success)
        {
            return RedirectToAction("Success", new { orderId });
        }

        if (payment.Status == PaymentStatus.Pending)
        {
            var liqPayData = await liqPayService.GetStatusAsync(orderId.ToString());
        
            if (liqPayData.TryGetValue("status", out var statusObj))
            {
                var status = statusObj.ToString();

                if (status is "success" or "sandbox" or "wait_accept" or "processing")
                {
                    await paymentRepository.UpdatePaymentStatusAsync(orderId, PaymentStatus.Success, 
                        System.Text.Json.JsonSerializer.Serialize(liqPayData));
                
                    return RedirectToAction("Success", new { orderId });
                }
            }
        }

        return RedirectToAction("Failed", new { orderId });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Success(int orderId)
    {
        var order = orderRepository.GetById(orderId);
        return View(order);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Failed(int orderId)
    {
        var order = orderRepository.GetById(orderId);
        return View(order);
    }

    private Task<Order> CreateOrderAsync(CheckoutViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cart = cartRepository.GetCartByUserId(userId);

        var deliveryCost = CalculateDeliveryCost(model.DeliveryMethod, model.City);

        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.Now,
            Status = OrderStatus.New,
            StatusId = (int)OrderStatus.New,
            TotalAmount = cart.Items.Sum(i => i.Quantity * i.Product.Price),
            OrderItems = cart.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Product.Price
            }).ToList()
        };

        orderRepository.Add(order);

        foreach (var item in cart.Items)
        {
            var product = productRepository.GetById(item.ProductId);
            product.StockQuantity -= item.Quantity;
            productRepository.Update(product);
                
            logger.LogInformation($"Updated stock for product {product.Name}: reduced by {item.Quantity}, remaining: {product.StockQuantity}");
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            PaymentMethod = model.PaymentMethod,
            Status = PaymentStatus.Pending,
            Amount = order.TotalAmount + deliveryCost,
            CreatedAt = DateTime.Now
        };

        paymentRepository.Add(payment);

        var delivery = new Delivery
        {
            OrderId = order.Id,
            Method = model.DeliveryMethod,
            Status = DeliveryStatus.Pending,
            Cost = (decimal)deliveryCost,
            RecipientName = model.RecipientName,
            RecipientPhone = model.RecipientPhone,
            Address = model.Address,
            City = model.City,
            NovaPoshtaBranch = model.NovaPoshtaBranch,
            CarrierName = model.CarrierName,
            PreferredDeliveryDate = model.PreferredDeliveryDate,
            CreatedAt = DateTime.Now,
            Notes = model.DeliveryNotes
        };

        deliveryRepository.Add(delivery);

        cartRepository.ClearCart(userId);

        return Task.FromResult(order);
    }

    private static double CalculateDeliveryCost(DeliveryMethod method, string? city)
    {
        return method switch
        {
            DeliveryMethod.SelfPickup => 0,
            DeliveryMethod.NovaPoshta => string.IsNullOrEmpty(city) ? 80 : 
                city.Contains("київ", StringComparison.CurrentCultureIgnoreCase) ? 60 : 80,
            DeliveryMethod.Courier => string.IsNullOrEmpty(city) ? 150 : 
                city.Contains("київ", StringComparison.CurrentCultureIgnoreCase) ? 100 : 150,
            _ => 0
        };
    }
}