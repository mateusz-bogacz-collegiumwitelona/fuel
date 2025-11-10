using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Data.Helpers
{
    public  class TableQueryBuilder<TEntity, TResponse> where TEntity : class
    {
        private IQueryable<TEntity> _query;
        private readonly TableRequest _request;

        public TableQueryBuilder(IQueryable<TEntity> query, TableRequest request)
        {
            _query = query.AsNoTracking();
            _request = request;
        }

        public  TableQueryBuilder<TEntity, TResponse> ApplySearch(
            Func<IQueryable<TEntity>, string, IQueryable<TEntity>> searchFunc
            )
        {
            if (!string.IsNullOrEmpty(_request.Search))
                _query = searchFunc(_query, _request.Search.ToLower());

            return this;
        }

        public TableQueryBuilder<TEntity, TResponse> ApplySort (
            Dictionary<string, Expression<Func<TEntity, object>>> sortMap,
            Expression<Func<TEntity, object>> defaultSort
            )
        {
            if (!string.IsNullOrEmpty(_request.SortBy) &&
                sortMap.TryGetValue(_request.SortBy.ToLower(), out var sortExpr))
            {
                _query = _request.SortDirection?.ToLower() == "desc"
                    ? _query.OrderByDescending(sortExpr)
                    : _query.OrderBy(sortExpr);
            }
            else
            {
                _query = _query.OrderBy(defaultSort);
            }

            return this;
        }

        public async Task<List<TResponse>> ProjectAndExecuteAsync(
            Expression<Func<TEntity, TResponse>> projection
            )
            => await _query.Select(projection).ToListAsync();
    }
}
