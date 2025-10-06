using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TechShop.Interfaces;
using System.Security.Claims;

namespace TechShop.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ShoppingCartController> _logger;

        public ShoppingCartController(
            IShoppingCartRepository cartRepository,
            IProductRepository productRepository,
            ILogger<ShoppingCartController> logger)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        [Authorize]
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = _cartRepository.GetCartByUserId(userId);
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            var product = _productRepository.GetById(productId);

            if (product.StockQuantity <= 0)
            {
                return Json(new { success = false, message = "Товар закінчився на складі." });
            }

            if (User.Identity is { IsAuthenticated: false })
            {
                TempData["PendingCartProductId"] = productId;
                
                return Json(new { 
                    success = false, 
                    requireAuth = true,
                    message = "Для додавання товарів в кошик потрібно увійти в систему",
                    loginUrl = "/Identity/Account/Login"
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var cart = _cartRepository.GetCartByUserId(userId);
                var existingItem = cart?.Items?.FirstOrDefault(i => i.ProductId == productId);
                
                var currentQuantityInCart = existingItem?.Quantity ?? 0;
                
                if (currentQuantityInCart >= product.StockQuantity)
                {
                    return Json(new { 
                        success = false, 
                        message = "Неможливо додати більше даного товару" 
                    });
                }

                _cartRepository.AddItem(userId, productId);
                
                var updatedCart = _cartRepository.GetCartByUserId(userId);
                var updatedItem = updatedCart?.Items?.FirstOrDefault(i => i.ProductId == productId);
                var newQuantityInCart = updatedItem?.Quantity ?? 1;
                
                var message = "Товар додано до кошика";
                if (newQuantityInCart >= product.StockQuantity)
                {
                    message += $" (досягнуто максимальну кількість: {product.StockQuantity} шт.)";
                }
                
                return Json(new { 
                    success = true,
                    message,
                    newQuantity = newQuantityInCart,
                    maxQuantity = product.StockQuantity
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding product to cart: {ex.Message}");
                return Json(new { success = false, message = "Помилка при додаванні товару до кошика. Спробуйте ще раз." });
            }
        }

        [HttpPost]
        [Authorize]
        public JsonResult UpdateQuantity(int productId, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = _productRepository.GetById(productId);

            if (quantity > product.StockQuantity)
            {
                ModelState.AddModelError("", "Запитана кількість перевищує наявний запас.");
                return Json(new { success = false, message = "Запитана кількість перевищує наявний запас." });
            }

            _cartRepository.UpdateQuantity(userId, productId, quantity);

            var cart = _cartRepository.GetCartByUserId(userId);
            var subtotal = cart.Items.FirstOrDefault(i => i.ProductId == productId)!.Quantity * product.Price;
            var total = cart.Items.Sum(i => i.Quantity * i.Product.Price);

            return Json(new { success = true, subtotal, total });
        }

        [HttpPost]
        [Authorize]
        public IActionResult RemoveFromCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _cartRepository.RemoveItem(userId, productId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        public IActionResult Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = _cartRepository.GetCartByUserId(userId);

            if (cart == null || cart.Items.Count == 0)
            {
                return Json(new { success = false, message = "Ваш кошик порожній." });
            }

            var stockIssues = new List<string>();

            foreach (var item in cart.Items)
            {
                var product = _productRepository.GetById(item.ProductId);
        
                if (product == null)
                {
                    stockIssues.Add($"Товар '{item.Product?.Name ?? "Невідомий товар"}' більше не доступний");
                    continue;
                }

                if (product.StockQuantity == 0)
                {
                    stockIssues.Add($"Товар '{product.Name}' закінчився на складі");
                }
                else if (product.StockQuantity < item.Quantity)
                {
                    stockIssues.Add($"Товар '{product.Name}': запитано {item.Quantity} шт., але в наявності лише {product.StockQuantity} шт.");
                }
            }

            if (stockIssues.Count != 0)
            {
                var errorMessage = "Виявлено проблеми з наступними товарами:<br/>" + 
                                   string.Join("<br/>", stockIssues.Select(issue => "• " + issue)) +
                                   "<br/><br/>Будь ласка, оновіть кількість товарів у кошику або видаліть недоступні товари.";
        
                return Json(new { 
                    success = false, 
                    message = errorMessage,
                    hasStockIssues = true
                });
            }

            return Json(new { 
                success = true, 
                redirect = Url.Action("Index", "Checkout") 
            });
        }

        [Authorize]
        public IActionResult ProcessPendingCart()
        {
            if (TempData["PendingCartProductId"] is int productId)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var product = _productRepository.GetById(productId);

                if (product != null)
                {
                    try
                    {
                        _cartRepository.AddItem(userId, productId);
                        TempData["CartMessage"] = $"Товар '{product.Name}' успішно додано до кошика!";
                        TempData["CartMessageType"] = "success";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding pending product to cart");
                        TempData["CartMessage"] = "Помилка при додаванні товару до кошика";
                        TempData["CartMessageType"] = "error";
                    }
                }

                TempData.Remove("PendingCartProductId");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}