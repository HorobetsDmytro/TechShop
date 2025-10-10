using TechShop.Interfaces;
using TechShop.Models;

namespace TechShop.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context) { }

    public override void Add(Category category)
    {
        Console.WriteLine($"Adding category: {category.Name}");
        base.Add(category);
        Console.WriteLine("Category added successfully");
    }

    public IEnumerable<Category> GetCategories()
    {
        return _context.Categories.ToList();
    }
}
