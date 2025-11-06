using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        public async Task<FuelType> FindFuelTypeByNameAsync(string fuelType)
            => await _context.FuelTypes.FirstOrDefaultAsync(ft => ft.Name == fuelType);

        public async Task<List<string>> GetAllFuelTypeCodesAsync()
            => await _context.FuelTypes.Select(ft => ft.Code).ToListAsync();

        public async Task<List<AddFuelTypeRequest>> GetStaionFuelTypes(Guid stationId)
            => await _context.FuelPrices
                .Where(fp => fp.StationId == stationId)
                .Include(fp => fp.FuelType)
                .Select(fp => new AddFuelTypeRequest
                {
                    Code = fp.FuelType.Code,
                    Price = fp.Price
                })
                .ToListAsync();



    }
}
