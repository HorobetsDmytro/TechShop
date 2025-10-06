using TechShop.Models;

namespace TechShop.Interfaces;

public interface IUserRepository : IRepository<User>
{
    User GetByUsername(string username);
}