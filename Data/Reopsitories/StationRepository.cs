using CommunityToolkit.HighPerformance.Helpers;
using Data.Context;
using Data.Helpers;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using System.Linq.Expressions;

namespace Data.Reopsitories
{
    public class StationRepository : StationFiltersSorting, IStationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StationRepository> _logger;
        private readonly IBrandRepository _brandRepository;

        private StationFiltersSorting filters = new StationFiltersSorting();

        public StationRepository(
            ApplicationDbContext context,
            ILogger<StationRepository> logger,
            IBrandRepository brandRepository)
        {
            _context = context;
            _logger = logger;
            _brandRepository = brandRepository;
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
                            .Select(fp => new GetFuelPriceAndCodeResponse
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
        

        public async Task<GetStationListResponse> GetStationProfileAsync(GetStationProfileRequest request)
        {
            var station = await _context.Stations
                .Include(s => s.Brand)
                .Include(s => s.Address)
                .Include(s => s.FuelPrice)
                    .ThenInclude(fp => fp.FuelType)
                .FirstOrDefaultAsync(s =>
                    s.Address.Street == request.Street &&
                    s.Address.HouseNumber == request.HouseNumber &&
                    s.Address.City == request.City &&
                    s.Address.PostalCode == request.PostalCode
                );

            if (station == null) return null;

            return new GetStationListResponse
            {
                BrandName = station.Brand.Name,
                Street = station.Address.Street,
                HouseNumber = station.Address.HouseNumber,
                City = station.Address.City,
                PostalCode = station.Address.PostalCode,
                Latitude = station.Address.Location.Y,
                Longitude = station.Address.Location.X,
                FuelPrice = station.FuelPrice
                             .Select(fp => new GetFuelPriceAndCodeResponse
                             {
                                 FuelCode = fp.FuelType.Code,
                                 Price = fp.Price,
                                 ValidFrom = fp.ValidFrom
                             })
                             .ToList()
            };
        }

        public async Task<Station> FindStationByDataAsync(string brandName, string street, string houseNumber, string city)
            => await _context.Stations
                .Include(s => s.Brand)
                .Include(s => s.Address)
                .Include(s => s.FuelPrice)
                .FirstOrDefaultAsync(s =>
                    s.Brand.Name.ToLower() == brandName.ToLower() &&
                    s.Address.Street.ToLower() == street.ToLower() &&
                    s.Address.HouseNumber.ToLower() == houseNumber.ToLower() &&
                    s.Address.City.ToLower() == city.ToLower()
                );

        public async Task<List<GetStationListForAdminResponse>> GetStationsListForAdminAsync(TableRequest request)
        {
            var query = _context.Stations
                .Include(s => s.Address)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                string searchLower = request.Search.ToLower();
                query = query.Where(s => 
                        s.Brand.Name.ToLower().Contains(searchLower) ||
                        s.Address.Street.ToLower().Contains(searchLower) ||
                        s.Address.HouseNumber.ToLower().Contains(searchLower) ||
                        s.Address.City.ToLower().Contains(searchLower) ||
                        s.Address.PostalCode.ToLower().Contains(searchLower)
                    );
            }

            var sortMap = new Dictionary<string, Expression<Func<Station, object>>>
            {
                { "brnadname", s => s.Brand.Name },
                { "street", s => s.Address.Street },
                { "housenumber", s => s.Address.HouseNumber },
                { "city", s => s.Address.City },
                { "postalcode", s => s.Address.PostalCode },
                { "createdat", s => s.CreatedAt },
                { "updatedat", s => s.UpdatedAt }
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
                query = query.OrderBy(s => s.Brand.Name);
            }

           var result = await query
                .Select(s => new GetStationListForAdminResponse
                {
                    BrandName = s.Brand.Name,
                    Street = s.Address.Street,
                    HouseNumber = s.Address.HouseNumber,
                    City = s.Address.City,
                    PostalCode = s.Address.PostalCode,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();

            return result;
        }

        public async Task<bool> IsStationExistAsync(string brandName, string street, string houseNumber, string city)
            => await _context.Stations.AnyAsync( s => 
                s.Brand.Name.ToLower() == brandName &&
                s.Address.Street.ToLower() == street &&
                s.Address.HouseNumber.ToLower() == houseNumber &&
                s.Address.City.ToLower() == city
            );

        public async Task<bool> EditStationAsync(EditStationRequest request)
        {
            var station = await _context.Stations
                .Include(s => s.Address)
                .Include(s => s.Brand)
                .FirstOrDefaultAsync(s =>
                    s.Brand.Name.ToLower() == request.FindStation.BrandName.ToLower() &&
                    s.Address.Street.ToLower() == request.FindStation.Street.ToLower() &&
                    s.Address.HouseNumber.ToLower() == request.FindStation.HouseNumber.ToLower() &&
                    s.Address.City.ToLower() == request.FindStation.City.ToLower()
                );

            if (station == null)
            {
                _logger.LogWarning("Station not found for edit: {findStation}", request.FindStation);
                return false;
            }

            if (!string.IsNullOrEmpty(request.NewBrandName) && !string.IsNullOrWhiteSpace(request.NewBrandName))
            {
                if (!await _brandRepository.FindBrandAsync(request.NewBrandName))
                    await _brandRepository.AddBrandAsync(request.NewBrandName);

                var brand = await _brandRepository.GetBrandData(request.NewBrandName);

                station.Brand = brand;
            }

            if (!string.IsNullOrWhiteSpace(request.NewStreet) &&
                 !string.IsNullOrWhiteSpace(request.NewHouseNumber) &&
                 !string.IsNullOrWhiteSpace(request.NewCity) &&
                 request.NewLatitude.HasValue &&
                 request.NewLongitude.HasValue
            )
            {
                var newLocation = new Point(
                    request.NewLongitude.Value,
                    request.NewLatitude.Value)
                { SRID = GeoConstants.SRID_VALUE };

                station.Address.Location = newLocation;
                station.Address.Street = request.NewStreet;
                station.Address.HouseNumber = request.NewHouseNumber;
                station.Address.City = request.NewCity;
                station.Address.UpdatedAt = DateTime.UtcNow;
            }

            station.UpdatedAt = DateTime.UtcNow;

            _context.Update(station);

            var result = await _context.SaveChangesAsync();

            return result > 0;

        }
    }
}
