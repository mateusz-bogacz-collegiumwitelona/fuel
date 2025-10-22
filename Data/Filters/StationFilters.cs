using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Microsoft.EntityFrameworkCore;
using Data.Models;
using System.Security.Cryptography.X509Certificates;

namespace Data.Helpers
{
    public class StationFilters
    {
        private double MetersToRadius(int distance)
          => (distance * 1000) / GeoConstants.METERS_PER_DEGREE;

        protected internal IQueryable<Station> FilterByDistance<T>(
            IQueryable<Station> query, 
            int distance,
            float lat,
            float lon)
            where T : class
        {
            var userLocation = new Point(lon, lat) { SRID = GeoConstants.SRID_VALUE };

            query = query.Where(s =>
                s.Address.Location.Distance(userLocation) <= MetersToRadius(distance)
            );

            var sql = query.ToQueryString();

            return query;
        }

        protected internal IQueryable<Station> FilterByFuelType(
            IQueryable<Station> query,
            List<string> fuelTypes
        )
        {
            if (fuelTypes == null || !fuelTypes.Any()) return query;

            return query.Select(s => new Station
            {
                Id = s.Id,
                Brand = s.Brand,
                Address = s.Address,
                FuelPrice = s.FuelPrice
                    .Where(fp => fuelTypes.Contains(fp.FuelType.Name))
                    .ToList()
            }).AsQueryable();
        }

        protected internal IQueryable<Station> FilterByPrice(
            IQueryable<Station> query,
            decimal? minPrice,
            decimal? maxPrice
        )
        {
            if (!minPrice.HasValue && !maxPrice.HasValue) return query;

            return query.Select(s => new Station
            {
                Id = s.Id,
                Brand = s.Brand,
                Address = s.Address,
                FuelPrice = s.FuelPrice
                    .Where(fp =>
                        (!minPrice.HasValue || fp.Price >= minPrice.Value) &&
                        (!maxPrice.HasValue || fp.Price <= maxPrice.Value)
                    )
                    .ToList()
            }).AsQueryable();
        }


        protected internal IQueryable<Station> FilterByBrand(IQueryable<Station> query, string brandName)
        {
            if (string.IsNullOrEmpty(brandName)) return query;

            brandName = brandName.ToLower();

            return query.Where(s => s.Brand.Name.ToLower() == brandName).AsQueryable();
        }
    }
}
