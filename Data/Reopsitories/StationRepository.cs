using Data.Context;
using Data.Interfaces;
using DTO.Requests;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
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
        private readonly ILogger<StationRepository> _logger;

        private const int SRID_VALUE = 4326;
        private const double METERS_PER_DEGREE = 111320.0;

        private double MetersToRadius(int distance)
          => (distance * 1000) / 111320.0;

        public StationRepository(
            ApplicationDbContext context,
            ILogger<StationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<GetStationsResponse>> GetAllStationsForMapAsync(GetStationsRequest request)
        {
            var query = _context.Stations
                .Include(s => s.Brand)
                .Include(s => s.Address)
                .AsQueryable();

            if (request.BrandName != null && request.BrandName.Count > 0)
                query = query.Where(s => request.BrandName.Contains(s.Brand.Name));

            if (request.LocationLatitude.HasValue &&
                request.LocationLongitude.HasValue &&
                request.Distance.HasValue)
            {
                double lat = request.LocationLatitude.Value;
                double lon = request.LocationLongitude.Value;

                var userLocation = new Point(lon, lat) { SRID = SRID_VALUE };

                query = query.Where(s =>
                    s.Address.Location.Distance(userLocation) <= MetersToRadius(request.Distance.Value)
                );

                var sql = query.ToQueryString();
            }

            var result = await query
                .Select(s => new GetStationsResponse
                {
                    BrandName = s.Brand.Name,
                    Street = s.Address.Street,
                    HouseNumber = s.Address.HouseNumber,
                    City = s.Address.City,
                    PostalCode = s.Address.PostalCode,
                    Latitude = s.Address.Location.Y,
                    Longitude = s.Address.Location.X
                })
                .ToListAsync();

            return result;
        }


        public async Task<List<GetStationsResponse>> GetNearestStationAsync(
            double latitude,
            double longitude,
            int? count
            )
        {
            var userLocation = new Point(longitude, latitude) { SRID = SRID_VALUE };

            if (count == null || count <= 0) count = 3;

            return await _context.Stations
                .OrderBy(s => s.Address.Location.Distance(userLocation))
                .Take(count.Value)
                .Include(s => s.Brand)
                .Select(s => new GetStationsResponse
                {
                    BrandName = s.Brand.Name,
                    Street = s.Address.Street,
                    HouseNumber = s.Address.HouseNumber,
                    City = s.Address.City,
                    PostalCode = s.Address.PostalCode,
                    Latitude = s.Address.Location.Y,
                    Longitude = s.Address.Location.X
                })
                .ToListAsync();
        }
    }
}
