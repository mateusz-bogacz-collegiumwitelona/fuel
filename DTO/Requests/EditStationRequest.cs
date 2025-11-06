using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class EditStationRequest
    {
        public FindStationRequest FindStation { get; set; }
        public string? NewBrandName { get; set; }
        public string? NewStreet { get; set; }
        public string? NewHouseNumber { get; set; }
        public string? NewCity { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? NewLatitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? NewLongitude { get; set; }
        public List<AddFuelTypeRequest>? FuelType { get; set; } = new();
    }
}
