using TechShop.Models;

namespace TechShop.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    IEnumerable<Product> GetProductsByCategory(int categoryId);

    IEnumerable<Product> GetProduct();

    IQueryable<Product> SearchProducts(string searchTerm);
};
