using Data.Context;
using Data.Helpers;
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
            => await new TableQueryBuilder<FuelType, GetFuelTypeResponses>(_context.FuelTypes, request)
                .ApplySearch((q, search) =>
                    q.Where(ft => ft.Name.ToLower().Contains(search) ||
                                  ft.Code.ToLower().Contains(search))
                )
                .ApplySort(
                    new Dictionary<string, Expression<Func<FuelType, object>>>
                    {
                        { "name", ft => ft.Name },
                        { "code", ft => ft.Code },
                        { "createdat", ft => ft.CreatedAt },
                        { "updatedat", ft => ft.UpdatedAt }
                    },
                    ft => ft.Name
                )
                .ProjectAndExecuteAsync(ft => new GetFuelTypeResponses
                {
                    Name = ft.Name,
                    Code = ft.Code,
                    CreatedAt = ft.CreatedAt,
                    UpdatedAt = ft.UpdatedAt
                });

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

        public async Task<bool> EditFuelTypeAsync(FuelType fuelType, string? name, string? code)
        {
            if (!string.IsNullOrEmpty(name))
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

        public async Task<bool> AssignFuelTypeToStationAsync(Guid fuelTypeId, Guid stationId, decimal price)
        {
            var station = await _context.Stations.FindAsync(stationId);

            var fuelType = await _context.FuelTypes.FindAsync(fuelTypeId);

            var existingFuelPrice = await _context.FuelPrices
                .FirstOrDefaultAsync(fp => fp.StationId == stationId && fp.FuelTypeId == fuelTypeId);

            if (existingFuelPrice != null)
            {
                _logger.LogInformation("FuelType with ID {FuelTypeId} is already assigned to Station with ID {StationId}.", fuelTypeId, stationId);
                return false;
            }

            var fuelPrice = new FuelPrice
            {
                StationId = stationId,
                FuelTypeId = fuelTypeId,
                Price = price,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.FuelPrices.AddAsync(fuelPrice);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<List<GetFuelPriceAndCodeResponse>> GetFuelPriceForStationAsync(FindStationRequest request)
            => await _context.FuelPrices.Include(fp => fp.FuelType)
                    .Where(fp =>
                        fp.Station.Brand.Name == request.BrandName &&
                        fp.Station.Address.Street == request.Street &&
                        fp.Station.Address.HouseNumber == request.HouseNumber &&
                        fp.Station.Address.City == request.City
                    )
                    .Select(fp => new GetFuelPriceAndCodeResponse
                    {
                        FuelCode = fp.FuelType.Code,
                        Price = fp.Price,
                        ValidFrom = fp.ValidFrom
                    }
                    ).ToListAsync();

        public async Task<bool> ChangeFuelPriceAsync(Guid stationId, Guid fuelTypeId, decimal price)
        {
            var fuelPrice = await _context.FuelPrices.FirstOrDefaultAsync(fp => fp.StationId == stationId && fp.FuelTypeId == fuelTypeId);
            
            if (fuelPrice == null)
            {
                _logger.LogWarning("Fuel price not found for Station ID {StationId} and FuelType ID {FuelTypeId}.", stationId, fuelTypeId);
                return false;
            }
            
            fuelPrice.Price = price;
            fuelPrice.UpdatedAt = DateTime.UtcNow;
            fuelPrice.ValidFrom = DateTime.UtcNow;
            
            var result = await _context.SaveChangesAsync();
            
            return result > 0;
        }

    }
}
