using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpers
{
    public static class PaginationExtensions
    {
        public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int pageNumber,int pageSize)
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
    }
}
