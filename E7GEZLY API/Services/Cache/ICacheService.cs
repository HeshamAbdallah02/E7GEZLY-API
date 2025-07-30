namespace E7GEZLY_API.Services.Caching
{
    /// <summary>
    /// Distributed caching service interface
    /// </summary>
    public interface ICacheService
    {
        // Basic operations
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        // Batch operations
        Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);
        Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        // Pattern-based operations
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        // Tag-based invalidation
        Task TagAsync(string key, params string[] tags);
        Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

        // Atomic operations
        Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    }
}