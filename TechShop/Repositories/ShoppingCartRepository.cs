using Microsoft.EntityFrameworkCore;
using TechShop.Interfaces;
using TechShop.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace TechShop.Repositories;

public class ShoppingCartRepository : GenericRepository<ShoppingCart>, IShoppingCartRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShoppingCartRepository> _logger;

    public ShoppingCartRepository(ApplicationDbContext context, ILogger<ShoppingCartRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public ShoppingCart GetCartByUserId(string userId)
    {
        return _context.ShoppingCarts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefault(c => c.UserId == userId) ?? CreateCart(userId);
    }

    public void AddItem(string userId, int productId)
    {
        var cart = GetCartByUserId(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item == null)
        {
            item = new ShoppingCartItem { ProductId = productId, Quantity = 1 };
            cart.Items.Add(item);
        }
        else
        {
            item.Quantity++;
        }

        _context.SaveChanges();
    }

    public void UpdateQuantity(string userId, int productId, int quantity)
    {
        var cart = GetCartByUserId(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item == null) return;
        item.Quantity = quantity;
        _context.SaveChanges();
    }

    public void RemoveItem(string userId, int productId)
    {
        var cart = GetCartByUserId(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item != null)
        {
            cart.Items.Remove(item);
            _context.SaveChanges();
        }
    }

    public void ClearCart(string userId)
    {
        try
        {
            var cart = _context.ShoppingCarts
                .Include(c => c.Items)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart != null)
            {
                foreach (var item in cart.Items.ToList())
                {
                    _context.ShoppingCartItems.Remove(item);
                }

                _context.SaveChanges();

                _logger.LogInformation($"Cart cleared for user {userId}");
            }
            else
            {
                _logger.LogWarning($"Cart not found for user {userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error clearing cart: {ex.Message}");
            throw;
        }
    }

    private ShoppingCart CreateCart(string userId)
    {
        var cart = new ShoppingCart { UserId = userId };
        _context.ShoppingCarts.Add(cart);
        _context.SaveChanges();
        return cart;
    }

    public IDbContextTransaction BeginTransaction()
    {
        return _context.Database.BeginTransaction();
    }
    
    public int GetProductQuantityInCart(string userId, int productId)
    {
        var cart = GetCartByUserId(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        return item?.Quantity ?? 0;
    }
}
