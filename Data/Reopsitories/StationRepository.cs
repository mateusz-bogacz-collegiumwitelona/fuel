using Data.Context;
using Data.Interfaces;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Data.Reopsitories
{
    public class StationRepository : IStationRepository
    {
        private readonly ApplicationDbContext _context;

        public StationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<GetStationsResponse>> GetAllStationsForMapAsync()
         => await _context.Stations
            .Select(s => new GetStationsResponse
            {
                BrandName = s.Brand.Name,
                Address = s.Address,
                Latitude = s.Location.Y,
                Longitude = s.Location.X
            })
            .ToListAsync();


        public async Task<List<GetStationsResponse>> GetNearestStationAsync(
            double latitude,
            double longitude,
            int? count 
            )
        {
            var userLocation = new Point(longitude, latitude) { SRID = 4326 };

            if (count == null || count <= 0) count = 3;

            return await _context.Stations
                .OrderBy(s => s.Location.Distance(userLocation))
                .Take(count.Value)
                .Include(s => s.Brand)
                .Select(s => new GetStationsResponse
                {
                    BrandName = s.Brand.Name,
                    Address = s.Address,
                    Latitude = s.Location.Y,
                    Longitude = s.Location.X
                })
                .ToListAsync();
        }
    }
}
