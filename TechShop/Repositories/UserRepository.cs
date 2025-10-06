using TechShop.Interfaces;
using TechShop.Models;

namespace TechShop.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public User GetByUsername(string username)
    {
        return _dbSet.FirstOrDefault(u => u.UserName == username);
    }
}
