using Data.Context;
using Data.Interfaces;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Reopsitories
{
    public class StationRepository : IStationRepository
    {
        private readonly ApplicationDbContext _context;

        public StationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<GetAllStationsForMap>> GetAllStationsForMapAsync()
         => await _context.Stations
            .Select(s => new GetAllStationsForMap
            {
                BrandName = s.Brand.Name,
                Address = s.Address,
                Latitude = s.Location.Y,
                Longitude = s.Location.X
            })
            .ToListAsync();
    }
}
