// E7GEZLY API/DTOs/Common/PagedResponse.cs
using Microsoft.EntityFrameworkCore;

namespace E7GEZLY_API.DTOs.Common
{
    /// <summary>
    /// Generic paged result for API responses
    /// </summary>
    public record PagedResult<T>(
        IEnumerable<T> Items,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages
    )
    {
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// Extension methods for creating paged results
    /// </summary>
    public static class PagedResultExtensions
    {
        public static PagedResult<T> ToPagedResult<T>(
            this IEnumerable<T> source,
            int totalCount,
            int pageNumber,
            int pageSize)
        {
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResult<T>(
                source,
                totalCount,
                pageNumber,
                pageSize,
                totalPages
            );
        }

        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> source,
            int pageNumber,
            int pageSize)
        {
            var totalCount = await source.CountAsync();
            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return items.ToPagedResult(totalCount, pageNumber, pageSize);
        }
    }
}