using DTO.Requests;
using System.ComponentModel.DataAnnotations;

namespace DTO.Responses
{
    public class GetStationInfoForEditResponse
    {
        public string NewBrandName { get; set; }
        public string NewStreet { get; set; }
        public string NewHouseNumber { get; set; }
        public string NewCity { get; set; }
        public double NewLatitude { get; set; }
        public double NewLongitude { get; set; }
        public List<AddFuelTypeRequest> FuelType { get; set; } = new();
    }
}
