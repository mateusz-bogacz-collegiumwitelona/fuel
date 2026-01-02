namespace DTO.Responses
{
    public class GetStationsResponse
    {
        public string BrandName { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; } 
        public string City { get; set; }
        public string PostalCode { get; set; } 
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
