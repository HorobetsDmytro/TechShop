using TechShop.Models;

namespace TechShop.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    IEnumerable<Category> GetCategories();
}
