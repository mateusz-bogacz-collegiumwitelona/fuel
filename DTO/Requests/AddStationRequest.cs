using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class AddStationRequest
    {
        [Required(ErrorMessage = "Brand name is required")]
        public string BrandName { get; set; }

        [Required(ErrorMessage = "Street is required")]
        public string Street { get; set; }

        [Required(ErrorMessage = "House number is required")]
        public string HouseNumber { get; set; }
        
        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [RegularExpression(@"^[A-Za-z0-9 -]{3,10}$", ErrorMessage = "Invalid postal code format.")]
        [Required(ErrorMessage = "City is required")]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        public List<AddFuelTypeAndPriceRequest> FuelTypes { get; set; } = new();
    }
}
