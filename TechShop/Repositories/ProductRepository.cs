using TechShop.Interfaces;
using TechShop.Models;

namespace TechShop.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }

    public IEnumerable<Product> GetProductsByCategory(int categoryId)
    {
        return _dbSet.Where(p => p.CategoryId == categoryId).ToList();
    }

    public override void Add(Product product)
    {
        try
        {
            Console.WriteLine("ProductRepository: Starting Add method");

            _context.Products.Add(product);
            Console.WriteLine("ProductRepository: Product added to context");

            _context.SaveChanges();
            Console.WriteLine("ProductRepository: Changes saved successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ProductRepository: Error in Add method: {ex.Message}");
            Console.WriteLine($"ProductRepository: Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public IEnumerable<Product> GetProduct()
    {
        return _context.Products.ToList();
    }
    
    public IQueryable<Product> GetProducts()
    {
        return _context.Products.AsQueryable();
    }

    public IQueryable<Product> SearchProducts(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return GetProducts();

        return _context.Products.Where(p => p.Name.Contains(searchTerm));
    }
    
    public override void Update(Product product)
    {
        try
        {
            _context.Products.Update(product);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ProductRepository: Error in Update method: {ex.Message}");
            throw;
        }
    }
}
