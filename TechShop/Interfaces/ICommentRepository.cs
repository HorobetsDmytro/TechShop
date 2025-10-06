using TechShop.Models;

namespace TechShop.Interfaces;

public interface ICommentRepository :  IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetCommentsByProductIdAsync(int productId);
    Task AddCommentAsync(Comment comment);
    Task DeleteCommentAsync(int commentId);
}
