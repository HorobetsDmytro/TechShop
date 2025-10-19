using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop.Interfaces;
using TechShop.Models;

namespace TechShop.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class ProductController : Controller
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }


    public IActionResult Index(string searchString, int? categoryId, string sortOrder, double? minPrice, double? maxPrice, int page = 1)
    {
        var products = _productRepository.GetAll();
        ViewBag.Categories = _categoryRepository.GetAll().ToList();

        if (!string.IsNullOrEmpty(searchString))
        {
            products = products.Where(p => p.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }

        if (categoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            products = products.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            products = products.Where(p => p.Price <= maxPrice.Value);
        }

        ViewBag.NameSortParam = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewBag.PriceSortParam = sortOrder == "price" ? "price_desc" : "price";

        products = sortOrder switch
        {
            "name_desc" => products.OrderByDescending(p => p.Name),
            "price" => products.OrderBy(p => p.Price),
            "price_desc" => products.OrderByDescending(p => p.Price),
            _ => products.OrderBy(p => p.Name)
        };

        const int pageSize = 4;
        var totalItems = products.Count();
        var pagedProducts = products.Skip((page - 1) * pageSize).Take(pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.SearchString = searchString;
        ViewBag.CategoryId = categoryId;
        ViewBag.CurrentSort = sortOrder;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;

        return View(pagedProducts);
    }

    public IActionResult Details(int id)
    {
        var product = _productRepository.GetById(id);
        return View(product);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = _categoryRepository.GetAll();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
    {
        try
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Будь ласка, виберіть зображення");
                ViewBag.Categories = _categoryRepository.GetAll();
                return View(product);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ImageFile", "Дозволені тільки зображення форматів: .jpg, .jpeg, .png, .gif");
                ViewBag.Categories = _categoryRepository.GetAll();
                return View(product);
            }

            if (ModelState.IsValid)
            {
                var fileName = Guid.NewGuid().ToString() + fileExtension;

                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                product.ImageUrl = "/images/products/" + fileName;

                _productRepository.Add(product);

                return RedirectToAction(nameof(Index));
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            foreach (var error in errors)
            {
                ModelState.AddModelError("", error);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Виникла помилка при створенні товару: {ex.Message}");
        }

        ViewBag.Categories = _categoryRepository.GetAll();
        return View(product);
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var product = _productRepository.GetById(id);

        ViewBag.Categories = _categoryRepository.GetAll();
        return View(product);
    }

    [HttpPost]
    public IActionResult Edit(Product product)
    {
        if (ModelState.IsValid)
        {
            _productRepository.Update(product);
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = _categoryRepository.GetAll();
        return View(product);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        var product = _productRepository.GetById(id);
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteConfirmed(int id)
    {
        _productRepository.Delete(id);
        return RedirectToAction(nameof(Index));
    }
}
