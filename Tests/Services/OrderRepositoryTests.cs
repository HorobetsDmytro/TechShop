using Moq;
using TechShop.Interfaces;
using TechShop.Models;
using Xunit;

namespace Tests.Services;

public class OrderRepositoryTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;

    public OrderRepositoryTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
    }

    [Fact]
    public void GetOrdersByUser_ValidUserId_ReturnsUserOrders()
    {
        var userId = "user123";
        var expectedOrders = new List<Order>
        {
            new Order { Id = 1, UserId = userId, TotalAmount = 5000, StatusId = 0 },
            new Order { Id = 2, UserId = userId, TotalAmount = 3000, StatusId = 1 }
        };

        _mockOrderRepository
            .Setup(repo => repo.GetOrdersByUser(userId))
            .Returns(expectedOrders);

        var result = _mockOrderRepository.Object.GetOrdersByUser(userId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, order => Assert.Equal(userId, order.UserId));
    }

    [Fact]
    public void GetOrdersByUser_NoOrders_ReturnsEmptyList()
    {
        var userId = "userWithoutOrders";
        var emptyList = new List<Order>();

        _mockOrderRepository
            .Setup(repo => repo.GetOrdersByUser(userId))
            .Returns(emptyList);

        var result = _mockOrderRepository.Object.GetOrdersByUser(userId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrders()
    {
        var expectedOrders = new List<Order>
        {
            new Order { Id = 1, UserId = "user1", TotalAmount = 5000 },
            new Order { Id = 2, UserId = "user2", TotalAmount = 3000 },
            new Order { Id = 3, UserId = "user1", TotalAmount = 7000 }
        };

        _mockOrderRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(expectedOrders);

        var result = await _mockOrderRepository.Object.GetAllAsync();

        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ExistingOrder_ReturnsOrder()
    {
        var orderId = 1;
        var expectedOrder = new Order
        {
            Id = orderId,
            UserId = "user123",
            TotalAmount = 5000,
            StatusId = 0,
            CreatedAt = System.DateTime.Now
        };

        _mockOrderRepository
            .Setup(repo => repo.GetByIdAsync(orderId))
            .ReturnsAsync(expectedOrder);

        var result = await _mockOrderRepository.Object.GetByIdAsync(orderId);

        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal("user123", result.UserId);
        Assert.Equal(5000, result.TotalAmount);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingOrder_ReturnsNull()
    {
        var orderId = 999;

        _mockOrderRepository
            .Setup(repo => repo.GetByIdAsync(orderId))
            .ReturnsAsync((Order)null);

        var result = await _mockOrderRepository.Object.GetByIdAsync(orderId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ValidOrder_UpdatesSuccessfully()
    {
        var order = new Order
        {
            Id = 1,
            UserId = "user123",
            TotalAmount = 6000,
            StatusId = 2
        };

        _mockOrderRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        await _mockOrderRepository.Object.UpdateAsync(order);

        _mockOrderRepository.Verify(
            repo => repo.UpdateAsync(It.Is<Order>(o => o.Id == 1 && o.StatusId == 2)),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesSuccessfully()
    {
        var orderId = 1;

        _mockOrderRepository
            .Setup(repo => repo.DeleteAsync(orderId))
            .Returns(Task.CompletedTask);

        await _mockOrderRepository.Object.DeleteAsync(orderId);

        _mockOrderRepository.Verify(
            repo => repo.DeleteAsync(orderId),
            Times.Once
        );
    }

    [Fact]
    public async Task GetProductAsync_ValidProductId_ReturnsProduct()
    {
        var productId = 1;
        var expectedProduct = new Product
        {
            Id = productId,
            Name = "Тестовий товар",
            Price = 1000,
            StockQuantity = 10
        };

        _mockOrderRepository
            .Setup(repo => repo.GetProductAsync(productId))
            .ReturnsAsync(expectedProduct);

        var result = await _mockOrderRepository.Object.GetProductAsync(productId);

        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Тестовий товар", result.Name);
    }
}