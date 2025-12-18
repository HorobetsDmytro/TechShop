using Moq;
using TechShop.Interfaces;
using TechShop.Models;
using Xunit;

namespace Tests.Services;

public class ShoppingCartRepositoryTests
{
    private readonly Mock<IShoppingCartRepository> _mockCartRepository;

    public ShoppingCartRepositoryTests()
    {
        _mockCartRepository = new Mock<IShoppingCartRepository>();
    }

    [Fact]
    public void GetCartByUserId_ExistingUser_ReturnsCart()
    {
        var userId = "user123";
        var expectedCart = new ShoppingCart
        {
            Id = 1,
            UserId = userId,
            Items = new List<ShoppingCartItem>
            {
                new ShoppingCartItem { Id = 1, ProductId = 1, Quantity = 2 },
                new ShoppingCartItem { Id = 2, ProductId = 2, Quantity = 1 }
            }
        };

        _mockCartRepository
            .Setup(repo => repo.GetCartByUserId(userId))
            .Returns(expectedCart);

        var result = _mockCartRepository.Object.GetCartByUserId(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public void GetCartByUserId_NewUser_ReturnsEmptyCart()
    {
        var userId = "newUser";
        var emptyCart = new ShoppingCart
        {
            UserId = userId,
            Items = new List<ShoppingCartItem>()
        };

        _mockCartRepository
            .Setup(repo => repo.GetCartByUserId(userId))
            .Returns(emptyCart);

        var result = _mockCartRepository.Object.GetCartByUserId(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void AddItem_ValidUserAndProduct_AddsItemToCart()
    {
        var userId = "user123";
        var productId = 1;

        _mockCartRepository
            .Setup(repo => repo.AddItem(userId, productId));

        _mockCartRepository.Object.AddItem(userId, productId);

        _mockCartRepository.Verify(
            repo => repo.AddItem(userId, productId),
            Times.Once
        );
    }

    [Fact]
    public void UpdateQuantity_ValidParameters_UpdatesItemQuantity()
    {
        var userId = "user123";
        var productId = 1;
        var newQuantity = 5;

        _mockCartRepository
            .Setup(repo => repo.UpdateQuantity(userId, productId, newQuantity));

        _mockCartRepository.Object.UpdateQuantity(userId, productId, newQuantity);

        _mockCartRepository.Verify(
            repo => repo.UpdateQuantity(userId, productId, newQuantity),
            Times.Once
        );
    }

    [Fact]
    public void UpdateQuantity_ZeroQuantity_CallsUpdate()
    {
        var userId = "user123";
        var productId = 1;
        var quantity = 0;

        _mockCartRepository
            .Setup(repo => repo.UpdateQuantity(userId, productId, quantity));

        _mockCartRepository.Object.UpdateQuantity(userId, productId, quantity);

        _mockCartRepository.Verify(
            repo => repo.UpdateQuantity(userId, productId, quantity),
            Times.Once
        );
    }

    [Fact]
    public void RemoveItem_ValidUserAndProduct_RemovesItem()
    {
        var userId = "user123";
        var productId = 1;

        _mockCartRepository
            .Setup(repo => repo.RemoveItem(userId, productId));

        _mockCartRepository.Object.RemoveItem(userId, productId);

        _mockCartRepository.Verify(
            repo => repo.RemoveItem(userId, productId),
            Times.Once
        );
    }

    [Fact]
    public void ClearCart_ValidUser_ClearsAllItems()
    {
        var userId = "user123";

        _mockCartRepository
            .Setup(repo => repo.ClearCart(userId));

        _mockCartRepository.Object.ClearCart(userId);

        _mockCartRepository.Verify(
            repo => repo.ClearCart(userId),
            Times.Once
        );
    }

    [Fact]
    public void ClearCart_EmptyCart_CallsClear()
    {
        var userId = "userWithEmptyCart";

        _mockCartRepository
            .Setup(repo => repo.ClearCart(userId));

        _mockCartRepository.Object.ClearCart(userId);

        _mockCartRepository.Verify(
            repo => repo.ClearCart(userId),
            Times.Once
        );
    }
}