using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Data.Reopsitories
{
    public class BrandRepository : IBrandRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BrandRepository> _logger;

        public BrandRepository (
            ApplicationDbContext context,
            ILogger<BrandRepository> logger
            )
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<GetBrandDataResponse>> GetBrandToListAsync(TableRequest request)
        {
            var query = _context.Brand.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                string searchLower = request.Search.ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(searchLower));
            }

            var sortMap = new Dictionary<string, Expression<Func<Brand, object>>>
            {
                { "name", b => b.Name },
                { "createdat", b => b.CreatedAt },
                { "updatedat", b => b.UpdatedAt }
            };

            if (!string.IsNullOrEmpty(request.SortBy) && 
                sortMap.ContainsKey(request.SortBy.ToLower()))
            {
                var sortExpr = sortMap[request.SortBy.ToLower()];
                query = request.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(sortExpr)
                    : query.OrderBy(sortExpr);
            }
            else
            {
                query = query.OrderBy(b => b.Name);
            }

            var result = await query
                .Select(b => new GetBrandDataResponse
                {
                    Name = b.Name,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                }).ToListAsync();

            return result;
        }
    }
}
