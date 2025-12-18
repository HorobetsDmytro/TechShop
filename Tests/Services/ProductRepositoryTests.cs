using Xunit;
using Moq;
using TechShop.Models;
using TechShop.Interfaces;

namespace Tests.Services;

public class ProductRepositoryTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;

    public ProductRepositoryTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
    }

    [Fact]
    public void GetProduct_ReturnsAllProducts()
    {
        var expectedProducts = new List<Product>
        {
            new Product { Id = 1, Name = "Ноутбук ASUS", Price = 25000, CategoryId = 1, StockQuantity = 5 },
            new Product { Id = 2, Name = "Смартфон Samsung", Price = 15000, CategoryId = 2, StockQuantity = 10 },
            new Product { Id = 3, Name = "Телевізор LG", Price = 35000, CategoryId = 3, StockQuantity = 3 }
        };

        _mockProductRepository
            .Setup(repo => repo.GetProduct())
            .Returns(expectedProducts);

        var result = _mockProductRepository.Object.GetProduct();

        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Equal(expectedProducts, result);
    }

    [Fact]
    public void GetProductsByCategory_ValidCategoryId_ReturnsFilteredProducts()
    {
        var categoryId = 1;
        var expectedProducts = new List<Product>
        {
            new Product { Id = 1, Name = "Ноутбук ASUS", Price = 25000, CategoryId = 1, StockQuantity = 5 },
            new Product { Id = 4, Name = "Ноутбук HP", Price = 22000, CategoryId = 1, StockQuantity = 7 }
        };

        _mockProductRepository
            .Setup(repo => repo.GetProductsByCategory(categoryId))
            .Returns(expectedProducts);

        var result = _mockProductRepository.Object.GetProductsByCategory(categoryId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(categoryId, p.CategoryId));
    }

    [Fact]
    public void GetProductsByCategory_EmptyCategory_ReturnsEmptyList()
    {
        var categoryId = 999;
        var emptyList = new List<Product>();

        _mockProductRepository
            .Setup(repo => repo.GetProductsByCategory(categoryId))
            .Returns(emptyList);

        var result = _mockProductRepository.Object.GetProductsByCategory(categoryId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void SearchProducts_ValidSearchTerm_ReturnsMatchingProducts()
    {
        var searchTerm = "ASUS";
        var allProducts = new List<Product>
        {
            new Product { Id = 1, Name = "Ноутбук ASUS", Price = 25000, CategoryId = 1 },
            new Product { Id = 2, Name = "Смартфон Samsung", Price = 15000, CategoryId = 2 },
            new Product { Id = 5, Name = "Монітор ASUS", Price = 8000, CategoryId = 4 }
        }.AsQueryable();

        _mockProductRepository
            .Setup(repo => repo.SearchProducts(searchTerm))
            .Returns(allProducts.Where(p => p.Name.Contains(searchTerm)));

        var result = _mockProductRepository.Object.SearchProducts(searchTerm);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Contains(searchTerm, p.Name));
    }

    [Fact]
    public void SearchProducts_NoMatches_ReturnsEmptyQueryable()
    {
        var searchTerm = "NonExistentProduct";
        var emptyQueryable = new List<Product>().AsQueryable();

        _mockProductRepository
            .Setup(repo => repo.SearchProducts(searchTerm))
            .Returns(emptyQueryable);

        var result = _mockProductRepository.Object.SearchProducts(searchTerm);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
}