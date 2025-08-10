using E7GEZLY_API.Application.Common.Interfaces;
using E7GEZLY_API.Data;

namespace E7GEZLY_API.Infrastructure.Persistence
{
    /// <summary>
    /// Unit of Work implementation for managing database transactions
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}