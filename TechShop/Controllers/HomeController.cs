using Microsoft.AspNetCore.Mvc;
using TechShop.Interfaces;
using TechShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace TechShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly UserManager<User> _userManager;

        public HomeController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ICommentRepository commentRepository,
            UserManager<User> userManager)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _commentRepository = commentRepository;
            _userManager = userManager;
        }

        public IActionResult Index(int page = 1, string sortOrder = "", int? categoryId = null,
                                   double? minPrice = null, double? maxPrice = null, string searchTerm = "")
        {
            const int pageSize = 9;

            var query = string.IsNullOrWhiteSpace(searchTerm)
                ? _productRepository.GetProduct()
                : _productRepository.SearchProducts(searchTerm);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderBy(p => p.Name)
            };

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var products = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var allProducts = _productRepository.GetProduct();
            var minPriceInDb = allProducts.Any() ? allProducts.Min(p => p.Price) : 0;
            var maxPriceInDb = allProducts.Any() ? allProducts.Max(p => p.Price) : 0;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice ?? minPriceInDb;
            ViewBag.MaxPrice = maxPrice ?? maxPriceInDb;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Categories = _categoryRepository.GetAll();

            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = _productRepository.GetById(id);

            ViewBag.Categories = _categoryRepository.GetAll();
            ViewBag.Comments = await _commentRepository.GetCommentsByProductIdAsync(id);
            return View(product);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int productId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Json(new { success = false, message = "Помилка!" });
            }

            var comment = new Comment
            {
                Text = text,
                ProductId = productId,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.Now
            };

            await _commentRepository.AddCommentAsync(comment);

            var user = await _userManager.GetUserAsync(User);
            return Json(new
            {
                success = true,
                comment = new
                {
                    id = comment.Id,
                    text = comment.Text,
                    createdAt = comment.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    userName = $"{user?.FirstName} {user?.LastName}"
                }
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            await _commentRepository.DeleteCommentAsync(commentId);
            return Json(new { success = true });
        }

        public IActionResult Main()
        {
            var random = new Random();
            var allProducts = _productRepository.GetProduct().ToList();
            var carouselProducts = allProducts
                .OrderBy(x => random.Next())
                .Take(5)
                .ToList();

            var popularProducts = _productRepository.GetProduct()
                .Take(4)
                .ToList();

            ViewBag.CarouselProducts = carouselProducts;
            return View(popularProducts);
        }
    }
}