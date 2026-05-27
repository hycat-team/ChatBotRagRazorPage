using System.Linq.Expressions;

namespace RagChatbot.DataAccess.Interfaces
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query();
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task SaveChangesAsync();
    }
}
