
namespace DTO.Requests
{
    public class GetStationListRequest
    {
        //location
        public double? LocationLatitude { get; set; }
        public double? LocationLongitude { get; set; }
        public double? Distance { get; set; }

        //fuelType
        public List<string>? FuelType { get; set; }

        //price 
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        //brand
        public string? BrandName { get; set; }

        //sorting by
        public bool? SortingByDisance { get; set; } = false;
        public bool? SortingByPrice { get; set; } = false;
        public string? SortingDirection { get; set; } = "asc";

        public GetPaggedRequest? Pagging { get; set; }
    }
}
