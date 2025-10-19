using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop.Interfaces;
using TechShop.Models;

namespace TechShop.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class CategoryController : Controller
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryController(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public IActionResult Index()
    {
        var categories = _categoryRepository.GetAll();
        return View(categories);
    }

    public IActionResult Details(int id)
    {
        var category = _categoryRepository.GetById(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Category category)
    {
        if (ModelState.IsValid)
        {
            _categoryRepository.Add(category);
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var category = _categoryRepository.GetById(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    [HttpPost]
    public IActionResult Edit(Category category)
    {
        if (ModelState.IsValid)
        {
            _categoryRepository.Update(category);
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    [HttpGet]
    public IActionResult Delete(int id)
    {
        var category = _categoryRepository.GetById(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteConfirmed(int id)
    {
        _categoryRepository.Delete(id);
        return RedirectToAction(nameof(Index));
    }
}
