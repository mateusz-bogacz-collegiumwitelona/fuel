namespace DTO.Responses
{
    public class GetStationListResponse
    {
        public string BrandName { get; set; }
        
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }


        public List<GetFuelPriceAndCodeResponse> FuelPrice { get; set; } = new List<GetFuelPriceAndCodeResponse>();
    }
}
