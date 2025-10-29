using Data.Context;
using Data.Interfaces;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            => await _context.FuelTypes
                .FirstOrDefaultAsync(ft => ft.Name == fuelType);
    }
}
