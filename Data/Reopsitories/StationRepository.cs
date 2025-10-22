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
using Data.Helpers;
using Data.Models;
namespace Data.Reopsitories
{
    public class StationRepository : StationFiltersSorting, IStationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StationRepository> _logger;

        private StationFiltersSorting filters = new StationFiltersSorting();

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

            if (request.LocationLatitude.HasValue && request.LocationLongitude.HasValue && request.Distance.HasValue)
                query = filters.FilterByDistance<Station>(
                    query,
                    (int)request.Distance.Value,
                    (float)request.LocationLatitude,
                    (float)request.LocationLongitude);

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
            var userLocation = new Point(longitude, latitude) { SRID = GeoConstants.SRID_VALUE };

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


        public async Task<List<GetStationListResponse>> GetStationListAsync(GetStationListRequest request)
        {
            var stations = _context.Stations
                .Include(s => s.Brand)
                .Include(s => s.Address)
                .Include(s => s.FuelPrice)
                    .ThenInclude(fp => fp.FuelType)
                .AsQueryable();

            if (request.LocationLongitude.HasValue &&
                request.LocationLatitude.HasValue &&
                request.Distance.HasValue)
            {
                stations = filters.FilterByDistance<Station>(
                    stations,
                    (int)request.Distance.Value,
                    (float)request.LocationLatitude.Value,
                    (float)request.LocationLongitude.Value
                );
            }

            if (request.FuelType != null && request.FuelType.Any())
                stations = filters.FilterByFuelType(stations, request.FuelType);

            if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
                stations = filters.FilterByPrice(stations, request.MinPrice, request.MaxPrice);

            if (!string.IsNullOrEmpty(request.BrandName))
                stations = filters.FilterByBrand(stations, request.BrandName);

            if (request.SortingByDisance.HasValue)
            {
                stations = filters.SortingByDistance(
                    stations,
                    (float)request.LocationLongitude.Value,
                    (float)request.LocationLatitude.Value,
                    request.SortingDirection
                );
            }
            else if (request.SortingByPrice.HasValue)
            {
                stations = stations.Where(s => s.FuelPrice.Any());
                stations = filters.SortingByPrice(
                    stations,
                    request.SortingDirection
                );
            }

            var result = stations.Select(s => new GetStationListResponse
            {
                BrandName = s.Brand.Name,
                Street = s.Address.Street,
                HouseNumber = s.Address.HouseNumber,
                City = s.Address.City,
                PostalCode = s.Address.PostalCode,
                Latitude = s.Address.Location.Y,
                Longitude = s.Address.Location.X,
                FuelPrice = s.FuelPrice
                            .Where(fp =>
                                (request.FuelType == null || !request.FuelType.Any() ||
                                 request.FuelType.Contains(fp.FuelType.Name)) &&
                                (!request.MinPrice.HasValue || fp.Price >= request.MinPrice.Value) &&
                                (!request.MaxPrice.HasValue || fp.Price <= request.MaxPrice.Value)
                            )
                            .Select(fp => new GetFuelPrivceAndCodeResponse
                            {
                                FuelCode = fp.FuelType.Code,
                                Price = fp.Price,
                                ValidFrom = fp.ValidFrom
                            })
                            .ToList()
            })
            .Where(s => s.FuelPrice.Any())
            .ToList();

            return result;
        }

        public async Task<bool> FindBrandAsync(string brandName)
            => await _context.Brand.AnyAsync(b => b.Name.ToLower() == brandName.ToLower());
    }
}
