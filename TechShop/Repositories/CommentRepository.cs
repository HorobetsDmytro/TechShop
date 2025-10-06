using Microsoft.EntityFrameworkCore;
using TechShop.Models;
using TechShop.Interfaces;

namespace TechShop.Repositories;

public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    public CommentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Comment>> GetCommentsByProductIdAsync(int productId)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.ProductId == productId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task AddCommentAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(int commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }
}
