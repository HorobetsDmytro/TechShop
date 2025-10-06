using TechShop.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace TechShop.Interfaces;

public interface IShoppingCartRepository :  IRepository<ShoppingCart>
{
    public ShoppingCart GetCartByUserId(string userId);
    public void AddItem(string userId, int productId);
    public void UpdateQuantity(string userId, int productId, int quantity);
    public void RemoveItem(string userId, int productId);
    void ClearCart(string userId);
    IDbContextTransaction BeginTransaction();
    int GetProductQuantityInCart(string userId, int productId);
}
