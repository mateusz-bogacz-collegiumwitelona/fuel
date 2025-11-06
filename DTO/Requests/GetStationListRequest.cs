using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class GetStationListRequest
    {
        //location
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? LocationLatitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? LocationLongitude { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Distance must be a positive value")]
        public double? Distance { get; set; }

        //fuelType
        public List<string>? FuelType { get; set; }

        //price 
        [Range(0, double.MaxValue, ErrorMessage = "Minimum price cannot be negative")]
        public decimal? MinPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Maximum price cannot be negative")]
        public decimal? MaxPrice { get; set; }

        //brand
        public string? BrandName { get; set; }

        //sorting by
        public bool? SortingByDisance { get; set; } = false;
        
        public bool? SortingByPrice { get; set; } = false;

        [RegularExpression("^(asc|desc)$", ErrorMessage = "Sorting direction must be 'asc' or 'desc'")]
        public string? SortingDirection { get; set; } = "asc";

        public GetPaggedRequest? Pagging { get; set; }
    }
}
