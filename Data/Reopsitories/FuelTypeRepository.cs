using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Data.Reopsitories
{
    public class FuelTypeRepository : IFuelTypeRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StationRepository> _logger;

        public FuelTypeRepository(
            ApplicationDbContext context,
            ILogger<StationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FuelType> FindFuelTypeByCodeAsync(string fuelType)
            => await _context.FuelTypes.FirstOrDefaultAsync(ft => ft.Code == fuelType);

        public async Task<List<string>> GetAllFuelTypeCodesAsync()
            => await _context.FuelTypes.Select(ft => ft.Code).ToListAsync();

        public async Task<List<FindFuelRequest>> GetStaionFuelTypes(Guid stationId)
            => await _context.FuelPrices
                .Where(fp => fp.StationId == stationId)
                .Include(fp => fp.FuelType)
                .Select(fp => new FindFuelRequest
                {
                    Code = fp.FuelType.Code,
                    Price = fp.Price
                })
                .ToListAsync();

        public async Task<List<GetFuelTypeResponses>> GetFuelsTypeListAsync(TableRequest request)
        {
            var query = _context.FuelTypes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                string searchLower = request.Search.ToLower();
                query = query.Where(ft => ft.Name.ToLower().Contains(searchLower) ||
                                          ft.Code.ToLower().Contains(searchLower));
            }

            var sortMap = new Dictionary<string, Expression<Func<FuelType, object>>>
            {
                { "name", ft => ft.Name },
                { "code", ft => ft.Code },
                { "createdat", ft => ft.CreatedAt },
                { "updatedat", ft => ft.UpdatedAt }
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
                query = query.OrderBy(ft => ft.Name);
            }

            var result = await query
                .Select(ft => new GetFuelTypeResponses
                {
                    Name = ft.Name,
                    Code = ft.Code,
                    CreatedAt = ft.CreatedAt,
                    UpdatedAt = ft.UpdatedAt
                })
                .ToListAsync();

            return result;
        }

        public async Task<bool> AddFuelTypeAsync(string name, string code)
        {
            var fuelType = new FuelType
            {
                Name = name,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.FuelTypes.AddAsync(fuelType);
            
            var result = await _context.SaveChangesAsync();
            
            return result > 0;
        }

        public async Task<bool> EditFuelTypeAsync(FuelType fuelType, string? name, string? code )
        {
            if(!string.IsNullOrEmpty(name))
            {
                fuelType.Name = name;
                fuelType.UpdatedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(code))
            {
                fuelType.Code = code;
                fuelType.UpdatedAt = DateTime.UtcNow;
            }


            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<bool> DeleteFuelTypeAsync(FuelType fuelType)
        {
            _context.Remove(fuelType);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}
