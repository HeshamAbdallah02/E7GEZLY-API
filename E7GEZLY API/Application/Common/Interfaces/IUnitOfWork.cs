namespace E7GEZLY_API.Application.Common.Interfaces
{
    /// <summary>
    /// Unit of Work interface for managing database transactions
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
    }
}