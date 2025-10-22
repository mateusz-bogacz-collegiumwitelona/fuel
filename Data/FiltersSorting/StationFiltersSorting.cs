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
    public class StationFiltersSorting
    {
        private double MetersToRadius(int distance)
          => (distance * 1000) / GeoConstants.METERS_PER_DEGREE;

        protected internal IQueryable<Station> FilterByDistance<T>(
            IQueryable<Station> query,
            int distance,
            float lat,
            float lon)
        {
            var userLocation = new Point(lon, lat) { SRID = GeoConstants.SRID_VALUE };

            query = query.Where(s =>
                s.Address.Location.Distance(userLocation) <= MetersToRadius(distance)
            );

            var sql = query.ToQueryString();

            return query;
        }

        protected internal IQueryable<Station> FilterByFuelType(IQueryable<Station> query, List<string> fuelTypes)
            => query.Where(s => s.FuelPrice.Any(fp => fuelTypes.Contains(fp.FuelType.Name)));

        protected internal IQueryable<Station> FilterByPrice(IQueryable<Station> query, decimal? minPrice, decimal? maxPrice)
            => query.Where(s => s.FuelPrice.Any(fp =>
                    (!minPrice.HasValue || fp.Price >= minPrice.Value) &&
                    (!maxPrice.HasValue || fp.Price <= maxPrice.Value)
                ));


        protected internal IQueryable<Station> FilterByBrand(IQueryable<Station> query, string brandName)
            => query.Where(s => s.Brand.Name.ToLower() == brandName.ToLower());

        protected internal IQueryable<Station> SortingByDistance(
            IQueryable<Station> query,
            float lon,
            float lat,
            string sortingDirection = "asc")
        {
            if (query == null) return query;

            var userLocation = new Point(lon, lat) { SRID = GeoConstants.SRID_VALUE };

            return sortingDirection?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Address.Location.Distance(userLocation))
                : query.OrderBy(s => s.Address.Location.Distance(userLocation));
        }

        protected internal IQueryable<Station> SortingByPrice( IQueryable<Station> query, string sortingDirection = "asc")
         => sortingDirection?.ToLower() == "desc"
                ? query.OrderByDescending(s => s.FuelPrice.Min(fp => fp.Price))
                : query.OrderBy(s => s.FuelPrice.Min(fp => fp.Price));
    }
}
