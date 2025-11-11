using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Services.Helpers
{
    public static class PaginationExtensions
    {
        public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var totalCount = source.Count();
            var items = source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0
            };
        }

        public static PagedResult<T> ToPagedResult<T>(this List<T> source, int pageNumber, int pageSize)
            => ((IEnumerable<T>)source).ToPagedResult(pageNumber, pageSize);

        public static async Task<Result<PagedResult<T>>> ToPagedResultAsync<T>(
            this Task<List<T>> dataTask,
            GetPaggedRequest pagged,
            ILogger logger,
            string entityName = "item"
            )
        {
            try
            {
                var result = await dataTask;

                if (result == null || !result.Any())
                {
                    logger.LogWarning($"No {entityName} found in the database.");
                    var emptyPage = CreateEmptyPagedResult<T>(pagged);

                    return Result<PagedResult<T>>.Good(
                        $"No {entityName} found.",
                        StatusCodes.Status200OK,
                        emptyPage);
                }

                int pageNumber = pagged.PageNumber ?? 1;
                int pageSize = pagged.PageSize ?? 10;

                var pagedResult = result.ToPagedResult(pageNumber, pageSize);

                if (pagedResult.PageNumber > pagedResult.TotalPages && pagedResult.TotalPages > 0)
                    pagedResult = result.ToPagedResult(pagedResult.TotalPages, pageSize);

                return Result<PagedResult<T>>.Good(
                    $"{entityName} retrieved successfully",
                    StatusCodes.Status200OK,
                    pagedResult);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred while retrieving {entityName}: {{Message}} | {{InnerException}}",
                    ex.Message, ex.InnerException);

                return Result<PagedResult<T>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

        private static PagedResult<T> CreateEmptyPagedResult<T>(GetPaggedRequest pagged)
            => new PagedResult<T>
            {
                Items = new List<T>(),
                PageNumber = pagged.PageNumber ?? 1,
                PageSize = pagged.PageSize ?? 10,
                TotalCount = 0,
                TotalPages = 0
            };

        public static async Task<Result<PagedResult<T>>> ToCachedPagedResultAsync<T>(
            this Func<Task<List<T>>> dataFactory,
            string cacheKey,
            GetPaggedRequest pagged,
            CacheService cache,
            ILogger logger,
            string entityName = "item",
            TimeSpan? cacheExpiry = null)
        {
            try
            {
                var result = await cache.GetOrSetListAsync(
                    cacheKey,
                    dataFactory,
                    cacheExpiry ?? CacheService.CacheExpiry.Medium
                );

                if (result == null || !result.Any())
                {
                    logger.LogWarning($"No {entityName} found in the database.");
                    var emptyPage = CreateEmptyPagedResult<T>(pagged);
                    return Result<PagedResult<T>>.Good(
                        $"No {entityName} found.",
                        StatusCodes.Status200OK,
                        emptyPage);
                }

                int pageNumber = pagged.PageNumber ?? 1;
                int pageSize = pagged.PageSize ?? 10;

                var pagedResult = result.ToPagedResult(pageNumber, pageSize);

                if (pagedResult.PageNumber > pagedResult.TotalPages && pagedResult.TotalPages > 0)
                    pagedResult = result.ToPagedResult(pagedResult.TotalPages, pageSize);

                return Result<PagedResult<T>>.Good(
                    $"{entityName} retrieved successfully",
                    StatusCodes.Status200OK,
                    pagedResult);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred while retrieving {entityName}: {{Message}} | {{InnerException}}",
                    ex.Message, ex.InnerException);
                return Result<PagedResult<T>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

       public static async Task<Result<PagedResult<T>>> ToCachedPagedResultAsync<T>(
           this Func<Task<List<T>>> dataFactory,
           string baseCacheKey,
           GetPaggedRequest pagged,
           TableRequest tableRequest,
           CacheService cache,
           ILogger logger,
           string entityName = "item",
           TimeSpan? cacheExpiry = null
           )
        {
            var cacheKey = cache.GeneratePagedKey(
                baseCacheKey,
                pagged.PageNumber ?? 1,
                pagged.PageSize ?? 10,
                tableRequest.Search,
                tableRequest.SortBy,
                tableRequest.SortDirection
            );

            return await dataFactory.ToCachedPagedResultAsync(
                cacheKey,
                pagged,
                cache,
                logger,
                entityName,
                cacheExpiry
            );
        }

    }
}
