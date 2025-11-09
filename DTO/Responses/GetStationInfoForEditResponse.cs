using DTO.Requests;
using System.ComponentModel.DataAnnotations;

namespace DTO.Responses
{
    public class GetStationInfoForEditResponse
    {
        public string BrandName { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<FindFuelTypeRequest> FuelType { get; set; } = new();
    }
}
