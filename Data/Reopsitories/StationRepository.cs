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
        private readonly IFuelTypeRepository _fuelTypeRepository;

        private StationFiltersSorting filters = new StationFiltersSorting();

        public StationRepository(
            ApplicationDbContext context,
            ILogger<StationRepository> logger,
            IBrandRepository brandRepository,
            IFuelTypeRepository fuelTypeRepository)
        {
            _context = context;
            _logger = logger;
            _brandRepository = brandRepository;
            _fuelTypeRepository = fuelTypeRepository;
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var station = await _context.Stations
                    .Include(s => s.Address)
                    .Include(s => s.Brand)
                    .FirstOrDefaultAsync(s =>
                        s.Brand != null &&  s.Brand != null && 
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

                if (!string.IsNullOrEmpty(request.NewBrandName))
                {
                    var brand = await _brandRepository.GetBrandData(request.NewBrandName);

                    if (brand != null)
                    {
                        station.Brand = brand;
                        station.BrandId = brand.Id;
                    }
                }

                bool addressUpdated = false;

                if (!string.IsNullOrWhiteSpace(request.NewStreet))
                {
                    station.Address.Street = request.NewStreet;
                    addressUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(request.NewHouseNumber))
                {
                    station.Address.HouseNumber = request.NewHouseNumber;
                    addressUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(request.NewCity))
                {
                    station.Address.City = request.NewCity;
                    addressUpdated = true;
                }

                if (request.NewLatitude.HasValue && request.NewLongitude.HasValue)
                {
                    var newLocation = new Point(
                        request.NewLongitude.Value,
                        request.NewLatitude.Value)
                    { SRID = GeoConstants.SRID_VALUE };

                    station.Address.Location = newLocation;
                    addressUpdated = true;
                }

                if (addressUpdated)
                {
                    station.Address.UpdatedAt = DateTime.UtcNow;
                }

                if (request.FuelType != null && request.FuelType.Any())
                {
                    var currentFuelTypes = await _fuelTypeRepository.GetStaionFuelTypes(station.Id);
                    var allFuelTypes = await _context.FuelTypes.ToListAsync();

                    var toDelete = currentFuelTypes
                        .Where(cf => !request.FuelType.Any(rf => rf.Code.Equals(cf.Code, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var toAdd = request.FuelType
                        .Where(rf => !currentFuelTypes.Any(cf => cf.Code.Equals(rf.Code, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var toUpdate = request.FuelType
                        .Where(rf => currentFuelTypes.Any(cf =>
                            cf.Code.Equals(rf.Code, StringComparison.OrdinalIgnoreCase) && cf.Price != rf.Price))
                        .ToList();

                    if (toDelete.Any())
                    {
                        var fuelsToRemove = await _context.FuelPrices
                            .Include(fp => fp.FuelType)
                            .Where(fp => fp.StationId == station.Id &&
                                         toDelete.Select(td => td.Code.ToLower()).Contains(fp.FuelType.Code.ToLower()))
                            .ToListAsync();

                        _context.FuelPrices.RemoveRange(fuelsToRemove);
                    }

                    foreach (var newFuel in toAdd)
                    {
                        var fuelType = allFuelTypes
                            .FirstOrDefault(f => f.Code.Equals(newFuel.Code, StringComparison.OrdinalIgnoreCase));

                        if (fuelType == null)
                            throw new InvalidOperationException($"Fuel type '{newFuel.Code}' not found in database.");

                        await _context.FuelPrices.AddAsync(new FuelPrice
                        {
                            Id = Guid.NewGuid(),
                            StationId = station.Id,
                            FuelTypeId = fuelType.Id,
                            Price = newFuel.Price,
                            ValidFrom = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }

                    foreach (var updatedFuel in toUpdate)
                    {
                        var fuelPrice = await _context.FuelPrices
                            .Include(fp => fp.FuelType)
                            .FirstOrDefaultAsync(fp =>
                                fp.StationId == station.Id &&
                                fp.FuelType.Code.Equals(updatedFuel.Code, StringComparison.OrdinalIgnoreCase));

                        if (fuelPrice != null)
                        {
                            fuelPrice.Price = updatedFuel.Price;
                            fuelPrice.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                station.UpdatedAt = DateTime.UtcNow;
                _context.Update(station);

                var result = await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return result > 0;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error editing station: {request}", request);
                return false;
            }
        }

        public async Task<GetStationInfoForEditResponse> GetStationInfoForEdit(FindStationRequest request)
            => await _context.Stations
                .Include(s => s.Brand)
                .Include(s => s.Address)
                .Include(s => s.FuelPrice)
                    .ThenInclude(fp => fp.FuelType)
                .Where(s =>
                    s.Brand.Name.ToLower() == request.BrandName.ToLower() &&
                    s.Address.Street.ToLower() == request.Street.ToLower() &&
                    s.Address.HouseNumber.ToLower() == request.HouseNumber.ToLower() &&
                    s.Address.City.ToLower() == request.City.ToLower()
                )
                .Select(s => new GetStationInfoForEditResponse
                {
                    NewBrandName = s.Brand.Name,
                    NewStreet = s.Address.Street,
                    NewHouseNumber = s.Address.HouseNumber,
                    NewCity = s.Address.City,
                    NewLatitude = s.Address.Location.Y,
                    NewLongitude = s.Address.Location.X,
                    FuelType = s.FuelPrice.Select(fp => new AddFuelTypeRequest
                    {
                        Code = fp.FuelType.Code,
                        Price = fp.Price
                    }).ToList()
                })
                .FirstOrDefaultAsync();

        public async Task<bool> AddNewStationAsync(AddStationRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var station = new Station
                {
                    Id = new Guid(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var brand = await _brandRepository.GetBrandData(request.BrandName);
                station.Brand = brand;

                station.Address = new StationAddress
                {
                    Id = Guid.NewGuid(),
                    Street = request.Street,
                    HouseNumber = request.HouseNumber,
                    City = request.City,
                    Location = new Point(
                        (float)request.Longitude,
                        (float)request.Latitude)
                    { SRID = GeoConstants.SRID_VALUE },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                foreach (var fuelTypeRequest in request.FuelTypes)
                {
                    var fuelType = await _context.FuelTypes
                        .FirstOrDefaultAsync(ft => ft.Code.ToLower() == fuelTypeRequest.Code.ToLower());

                    if (fuelType == null)
                        throw new InvalidOperationException($"Fuel type '{fuelTypeRequest.Code}' not found in database.");

                    station.FuelPrice.Add(new FuelPrice
                    {
                        Id = Guid.NewGuid(),
                        FuelType = fuelType,
                        Price = fuelTypeRequest.Price,
                        ValidFrom = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }


                await _context.Stations.AddAsync(station);
                var result = await _context.SaveChangesAsync();

                if (result <= 0)
                    throw new InvalidOperationException("Failed to add new station to the database.");

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding new station: {request}", request);
                return false;
            }
        }
    }
}
